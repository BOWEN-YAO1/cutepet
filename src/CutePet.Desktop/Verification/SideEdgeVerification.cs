using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Media;

namespace CutePet.Desktop;

internal static class SideEdgeVerification
{
    internal static void Run(MainWindow window, string directory, Action<bool,string> check)
    {
        var area = new Rect(-1920,-240,1920,1040);
        var size = new Size(230,178);
        var quota = window.QuotaHost.Position;
        var encoder = new GifBitmapEncoder();
        var delays = new List<int>();
        var leftAngles = new Dictionary<string,double>();
        var samples = new List<BitmapSource>();
        var labels = new[] { "微笑探头", "缩回再探出", "探头轻摇", "探头点头" };
        var artwork = SideEdgeMotion.Responses.SelectMany(a => window.SelectedCharacter.Actions[a].Frames).DistinctBy(f => f.Image).ToArray();
        check(artwork.Length == 176 && artwork.All(f => f.Image.IsFrozen),
            "side gestures use 176 distinct drawings: 144 reveal stages and sixteen each for nod and tilt");
        check(window.SelectedCharacter.Actions["edge-peek"].Frames.Select(f=>f.Image).Distinct().Count()==144
            && new[]{"edge-nod","edge-sway"}.All(a=>window.SelectedCharacter.Actions[a].Frames
                .Select(f=>f.Image).Distinct().Count()==160),
            "nod and tilt each include the shared 144-stage reveal and sixteen independent gesture drawings");
        check(SideEdgeMotion.Responses.All(a=>ReferenceEquals(window.SelectedCharacter.Actions[a].Frames[0].Image,
                window.SelectedCharacter.Actions["edge-idle"].Frames[0].Image)
            &&ReferenceEquals(window.SelectedCharacter.Actions[a].Frames[^1].Image,window.SelectedCharacter.Actions["edge-idle"].Frames[0].Image)),
            "dense side gestures start and finish at the exact shared idle drawing");
        check(SideEdgeMotion.Responses.All(a=>window.SelectedCharacter.Actions[a].SmoothFrames
            && window.SelectedCharacter.Actions[a].Frames.All(f=>f.DurationMs is 10 or 40)),
            "dense reveal drawings advance at ten milliseconds while gestures retain forty-millisecond timing");
        check(window.SelectedCharacter.Actions["edge-peek"].Duration==2870
            && window.SelectedCharacter.Actions["edge-shy"].Duration==4310
            && new[]{"edge-nod","edge-sway"}.All(a=>window.SelectedCharacter.Actions[a].Duration==4110),
            "adding 112 transition drawings keeps reveal under three seconds and gestures under five seconds");
        check(artwork.All(f=>f.EdgeAnchorX==48.0/288 && f.EdgeAnchorY==260.0/384),
            "all side drawings share identical hand registration so frame changes never shift the anchor");
        var heads=artwork.Select(f=>HeadHeight(f.Image)).ToArray();
        for(var i=0;i<artwork.Length;i++)
        {var pose=new PngBitmapEncoder();pose.Frames.Add(BitmapFrame.Create(artwork[i].Image));
            using var file=File.Create(Path.Combine(directory,$"side-registered-{i:00}.png"));pose.Save(file);}
        check(heads.Max()-heads.Min()<=5 && heads.All(h=>h>=195&&h<=205),
            "actual rendered side head heights stay within five source pixels across reveal, nod and tilt: "+string.Join(",",heads));
        foreach(var frame in artwork)
        {
            var rgba=new FormatConvertedBitmap(frame.Image,PixelFormats.Bgra32,null,0);
            var stride=rgba.PixelWidth*4;var pixels=new byte[stride*rgba.PixelHeight];rgba.CopyPixels(pixels,stride,0);
            int Palm(int start,int end)
            {
                var count=0;
                for(var y=start;y<end;y++)for(var x=32;x<65;x++)
                {var p=y*stride+x*4;if(pixels[p+3]>=200&&pixels[p+2]>pixels[p+1]*1.1&&pixels[p+1]>pixels[p]*1.05)count++;}
                return count;
            }
            check(Palm(210,255)>=20&&Palm(265,310)>=20,
                "both painted gripping palms remain visible near the fixed side boundary");
        }
        CheckInterpolation(check);
        long LowerVisible(LoadedFrame frame)
        {
            var rgba = new FormatConvertedBitmap(frame.Image,PixelFormats.Bgra32,null,0);
            var stride = rgba.PixelWidth * 4;
            var pixels = new byte[stride * rgba.PixelHeight]; rgba.CopyPixels(pixels,stride,0);
            long count = 0;
            for (var y = (int)(rgba.PixelHeight * .65); y < rgba.PixelHeight; y++)
                for (var x = (int)(rgba.PixelWidth * frame.EdgeAnchorX!.Value); x < rgba.PixelWidth; x++)
                    if (pixels[y * stride + x * 4 + 3] >= 200) count++;
            return count;
        }
        var peekFrames = window.SelectedCharacter.Actions["edge-peek"].Frames;
        check(peekFrames.Max(LowerVisible) > LowerVisible(peekFrames[0]) * 1.5,
            "deeper leaning artwork progressively exposes more of the connected lower silhouette");
        SaveDrawings(window.SelectedCharacter,directory);
        foreach (var side in new[] { ScreenEdge.Left, ScreenEdge.Right })
        {
            window.WakeCharacterImmediately();
            window.CompletePetDrag(area,size,new Point(side == ScreenEdge.Left ? area.Left : area.Right - size.Width,100),1);
            window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(320));
            for (var index = 0; index < SideEdgeMotion.Responses.Length; index++)
            {
                var action = SideEdgeMotion.Responses[index];
                var duration = window.SelectedCharacter.Actions[action].Duration;
                var maximumOutward = 0.0;
                var seen = new HashSet<BitmapSource>();
                var aligned = true;
                window.PlayCharacterInteraction();
                check(window.ScreenEdgeResponse == action, "side responses rotate through supported package actions " + side + action);
                var steps=(int)Math.Ceiling(duration/20);
                for (var step = 0; step < steps; step++)
                {
                    var frame = ScreenEdgeVerification.Capture(window, side, (side == ScreenEdge.Left ? "左侧 · " : "右侧 · ") + labels[index]);
                    if(step%2==0){encoder.Frames.Add(BitmapFrame.Create(frame));delays.Add(4);}
                    seen.Add(window.CurrentSpriteFrame.Image);
                    if (step == steps/3) samples.Add(frame);
                    window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(Math.Min(20,duration-step*20)));
                    maximumOutward = Math.Max(maximumOutward,window.ScreenEdgePeekOffset);
                    var current = window.CurrentSpriteFrame;
                    var width = Math.Min(window.CharacterArt.Width,window.CharacterArt.Height * current.Image.PixelWidth / current.Image.PixelHeight);
                    var gripX = 115 + (side == ScreenEdge.Left ? 1 : -1) * width * (current.EdgeAnchorX!.Value - .5) + window.EdgeShift.X;
                    aligned &= Math.Abs(gripX - (side == ScreenEdge.Left ? 0 : 230)) < .001 && window.SideEdgeTilt.Angle == 0;
                    if (step == steps/2)
                    {
                        if (side == ScreenEdge.Left) leftAngles[action] = window.SideEdgeTilt.Angle;
                        else check(Math.Abs(leftAngles[action] + window.SideEdgeTilt.Angle) < .0001,
                            "right-side gesture mirrors its corresponding left tilt " + action);
                        window.BeginDetailsMenu();
                        var held = (window.EdgeShift.X,window.EdgeShift.Y,window.SideEdgeTilt.Angle,window.CharacterArt.Source);
                        window.AdvanceCharacterAnimation(TimeSpan.FromSeconds(10));
                        check(held == (window.EdgeShift.X,window.EdgeShift.Y,window.SideEdgeTilt.Angle,window.CharacterArt.Source),
                            "menu freezes gesture motion and expression together " + side + action);
                        window.EndDetailsMenu();
                        window.PlayCharacterInteraction();
                        check(window.ScreenEdgeResponse == action, "busy side response ignores repeated clicks without queuing " + side + action);
                    }
                }
                check(window.ScreenEdgeResponse is null && window.ScreenEdgePeekOffset == 0
                    && window.CurrentCharacterFrame == CharacterFrame.EdgeIdle && window.QuotaHost.Position == quota
                    && area.Contains(new Rect(window.ScreenEdgeAttachment!.Position,size)),
                    "finite side gesture returns to its base with fixed quota and safe HWND bounds " + side + action);
                check(maximumOutward == 0 && seen.Count >= 6 && aligned,
                    "drawn articulated stages keep their grips aligned and reveal gradually without whole-sprite translation " + side + action);
            }
            window.PlayCharacterInteraction();
            check(window.ScreenEdgeResponse == "edge-peek", "side gesture sequence wraps without accumulating input " + side);
            window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(300));
            window.RefreshScreenEdgeBounds(new Rect(-1920,-200,1920,1000),size);
            check(window.ScreenEdgeResponse == "edge-peek", "work-area reanchor preserves the running side response " + side);
            window.WakeCharacterImmediately();
            check(window.SideEdgeTilt.Angle == 0 && window.SideEdgeTilt.CenterX == 0 && window.EdgeSwing.Angle == 0 && window.EdgeShift.X == 0 && window.EdgeShift.Y == 0,
                "detaching removes side tilt, grip pivot and offsets " + side);
        }
        using var bytes = new MemoryStream(); encoder.Save(bytes);
        File.WriteAllBytes(Path.Combine(directory,"side-edge-actions.gif"),ThroneMotionVerification.WithAnimationMetadata(bytes.ToArray(),delays));
        check(encoder.Frames.Count == SideEdgeMotion.Responses.Sum(a=>(int)Math.Ceiling(Math.Ceiling(window.SelectedCharacter.Actions[a].Duration/20)/2))*2,
            "side GIF samples every forty milliseconds while window assertions advance every twenty milliseconds");
        var contact = new DrawingVisual();
        using (var draw = contact.RenderOpen())
            for (var row = 0; row < 4; row++)
                for (var column = 0; column < 2; column++)
                    draw.DrawImage(samples[column * 4 + row],new Rect(column * 680,row * 390,680,390));
        var bitmap = new RenderTargetBitmap(1360,1560,96,96,PixelFormats.Pbgra32); bitmap.Render(contact);
        var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(bitmap));
        using (var file = File.Create(Path.Combine(directory,"side-edge-contact-sheet.png"))) png.Save(file);
        var stages = new DrawingVisual();
        using (var draw = stages.RenderOpen())
        {
            var captions = new[] {"浅探头","露出肩部","侧身露腰","裙摆跟随","更深探出","微笑停留"};
            window.WakeCharacterImmediately();
            window.CompletePetDrag(area,size,new Point(area.Left,100),1);
            window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(320));
            window.PlayCharacterInteraction();
            for (var i = 0; i < 6; i++)
            {
                var capture = ScreenEdgeVerification.Capture(window,ScreenEdge.Left,captions[i]);
                draw.DrawImage(capture,new Rect((i % 3) * 680,(i / 3) * 390,680,390));
                if(i<5)window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(window.SelectedCharacter.Actions["edge-peek"].Duration/10));
            }
        }
        var stageBitmap = new RenderTargetBitmap(2040,780,96,96,PixelFormats.Pbgra32); stageBitmap.Render(stages);
        var stagePng = new PngBitmapEncoder(); stagePng.Frames.Add(BitmapFrame.Create(stageBitmap));
        using (var file = File.Create(Path.Combine(directory,"side-edge-stages.png"))) stagePng.Save(file);
        window.WakeCharacterImmediately();
        var player = new CharacterAnimation(); player.Configure(window.SelectedCharacter);
        foreach (var action in SideEdgeMotion.Responses.Skip(1))
        {
            player.Preview(action); player.Advance(TimeSpan.FromSeconds(30));
            check(player.Action == "edge-idle", "custom side preview returns to its package base " + action);
            player.Preview(action); player.Low = true;
            check(player.Action == "low", "fresh low quota clears custom side preview " + action);
        }
    }

    private static void CheckInterpolation(Action<bool,string> check)
    {
        BitmapSource Solid(byte b,byte g,byte r,byte a)
            => BitmapSource.Create(1,1,96,96,PixelFormats.Pbgra32,null,new[]{b,g,r,a},4);
        var red=Solid(0,0,255,255); var blue=Solid(255,0,0,255); var clear=Solid(0,0,0,0);
        var interpolator=new FrameInterpolator();
        var bytes=new byte[4]; var middle=interpolator.Sample(red,blue,.5);middle.CopyPixels(bytes,4,0);
        check(bytes[0]==128&&bytes[2]==127&&bytes[3]==255,"registered frame interpolation preserves opaque color without dimming");
        var later=interpolator.Sample(red,blue,.75);later.CopyPixels(bytes,4,0);
        check(ReferenceEquals(middle,later)&&bytes[0]>128&&bytes[2]<127,"subframe samples reuse one bounded output while pixels continue moving");
        interpolator.Sample(red,clear,.5).CopyPixels(bytes,4,0);
        check(bytes[2]==127&&bytes[3]==127,"transparent interpolation blends premultiplied color and alpha together");
        check(ReferenceEquals(interpolator.Sample(red,blue,0),red)&&ReferenceEquals(interpolator.Sample(red,blue,1),blue)
            &&ReferenceEquals(interpolator.Sample(red,red,.5),red),"frame boundaries and identical idle drawings remain their exact cached images");
        BitmapSource Marker(int headX)
        {
            var pixels=new byte[13*192*4];
            var eye=(5*13+headX)*4; pixels[eye+2]=pixels[eye+3]=255;
            foreach(var y in new[]{134,170})
            {var hand=(y*13+2)*4; pixels[hand+1]=pixels[hand+3]=255;}
            return BitmapSource.Create(13,192,96,96,PixelFormats.Pbgra32,null,pixels,13*4);
        }
        var a=new LoadedFrame(Marker(3),40,EdgeAnchorX:2.0/13,EdgeAnchorY:.75,HeadAnchorX:3.0/13,HeadAnchorY:5.0/192);
        var b=new LoadedFrame(Marker(9),40,EdgeAnchorX:2.0/13,EdgeAnchorY:.75,HeadAnchorX:9.0/13,HeadAnchorY:5.0/192);
        var morphed=interpolator.Sample(a,b,.5);var actual=new byte[13*192*4];morphed.CopyPixels(actual,13*4,0);
        check(actual[(5*13+6)*4+3]>=240&&actual[(5*13+3)*4+3]==0&&actual[(5*13+9)*4+3]==0,
            "head landmark warping produces one moving eye instead of two crossfaded eyes");
        check(new[]{134,170}.All(y=>actual[(y*13+2)*4+1]==255&&actual[(y*13+2)*4+3]==255),
            "head warping leaves both upper and lower palms fixed around the side anchor midpoint");
        interpolator.Reset();
    }

    private static void SaveDrawings(CharacterPack pack,string directory)
    {
        var encoder=new GifBitmapEncoder();var delays=new List<int>();
        foreach(var action in SideEdgeMotion.Responses)
        {
            var clip=pack.Actions[action];
            // GIF viewers often clamp a 10 ms delay to 100 ms. Capture the
            // source sequence at 50 fps while preserving its real duration.
            for(var elapsed=0.0;elapsed<clip.Duration;elapsed+=20)
            {
                var visual=new DrawingVisual();using(var draw=visual.RenderOpen())
                {draw.DrawRectangle(Brushes.WhiteSmoke,null,new Rect(0,0,288,384));draw.DrawImage(clip.At(elapsed),new Rect(0,0,288,384));}
                var bitmap=new RenderTargetBitmap(288,384,96,96,PixelFormats.Pbgra32);bitmap.Render(visual);
                encoder.Frames.Add(BitmapFrame.Create(bitmap));delays.Add((int)Math.Min(20,clip.Duration-elapsed)/10);
            }
        }
        using var bytes=new MemoryStream();encoder.Save(bytes);
        File.WriteAllBytes(Path.Combine(directory,"side-drawn-keyframes.gif"),ThroneMotionVerification.WithAnimationMetadata(bytes.ToArray(),delays));
        var drawings=pack.Actions["edge-peek"].Frames.Select(f=>f.Image).Distinct().ToArray();
        var grid=new DrawingVisual();using(var draw=grid.RenderOpen())
        {
            draw.DrawRectangle(Brushes.WhiteSmoke,null,new Rect(0,0,2304,1728));
            for(var i=0;i<drawings.Length;i++)
                draw.DrawImage(drawings[i],new Rect(i%16*144,i/16*192,144,192));
        }
        var sheet=new RenderTargetBitmap(2304,1728,96,96,PixelFormats.Pbgra32);sheet.Render(grid);
        var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(sheet));
        using var file=File.Create(Path.Combine(directory,"side-144-stages.png"));png.Save(file);
    }

    internal static int HeadHeight(BitmapSource image)
    {
        var rgba=new FormatConvertedBitmap(image,PixelFormats.Bgra32,null,0);
        var width=rgba.PixelWidth;var height=rgba.PixelHeight;
        var bytes=new byte[width*height*4];rgba.CopyPixels(bytes,width*4,0);
        var skin=new bool[width*height];var top=height;
        for(var y=0;y<height;y++)for(var x=0;x<width;x++)
        {
            var p=(y*width+x)*4;
            if(bytes[p+3]<200)continue;
            top=Math.Min(top,y);
            skin[y*width+x]=bytes[p+2]>180&&bytes[p+2]>bytes[p+1]*1.04&&bytes[p+1]>bytes[p]*1.02;
        }
        var largest=0;var chin=0;var queue=new Queue<int>();
        for(var seed=0;seed<skin.Length;seed++)
        {
            if(!skin[seed])continue;
            skin[seed]=false;queue.Enqueue(seed);var count=0;var bottom=0;
            while(queue.Count>0)
            {
                var point=queue.Dequeue();count++;bottom=Math.Max(bottom,point/width);
                // Include diagonal skin links after filtering, keeping the
                // lower cheek in the same face component as the upper cheek.
                for(var dy=-1;dy<=1;dy++)for(var dx=-1;dx<=1;dx++)
                    Add(point+dy*width+dx,point%width+dx>=0&&point%width+dx<width
                        &&point/width+dy>=0&&point/width+dy<height);
            }
            if(count>largest){largest=count;chin=bottom;}
        }
        return chin-top;
        void Add(int point,bool inside)
        {if(inside&&skin[point]){skin[point]=false;queue.Enqueue(point);}}
    }
}
