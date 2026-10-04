using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CutePet.Desktop;

internal static class TopEdgeVerification
{
    internal static void Run(MainWindow window,string directory,Action<bool,string> check)
    {
        var area=new Rect(-1920,-240,1920,1040);
        var size=new Size(230,178);
        var quota=window.QuotaHost.Position;
        window.WakeCharacterImmediately();
        window.CompletePetDrag(area,size,new Point(-1000,area.Top),1);
        window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(320));
        var pack=window.SelectedCharacter;
        var frames=EdgeActions.TopResponses.SelectMany(a=>pack.Actions[a].Frames).GroupBy(f=>f.Image).Select(g=>g.First()).ToArray();
        check(frames.Length==64&&frames.All(f=>f.Image.IsFrozen),
            "top responses use sixty-four drawn poses across four continuous sixteen-stage motions");
        check(EdgeActions.TopResponses.All(a=>pack.Actions[a].Frames.Select(f=>f.Image).Distinct().Count()>=16),
            "each top response contains at least sixteen actual source drawings, excluding repeats and interpolation");
        var blinkFrames=pack.Actions["edge-top-peek"].Frames;
        check(blinkFrames.Count==31&&blinkFrames.Take(16).Select(f=>f.Image).SequenceEqual(blinkFrames.TakeLast(16).Reverse().Select(f=>f.Image)),
            "dense blink closes through all sixteen drawings and reopens with the exact reverse sequence");
        var iris=blinkFrames.Take(16).Select(f=>VisibleIris(f.Image)).ToArray();
        check(iris[0]>=50&&iris[^1]<=iris[0]*.05&&iris.Select(n=>n/10).Distinct().Count()>=8
            &&iris.Zip(iris.Skip(1),(a,b)=>Math.Abs(a-b)).Max()<=iris[0]*.35,
            "drawn eyelids progressively cover the green irises through at least eight visible openness levels: "+string.Join(",",iris));
        check(iris.Zip(iris.Skip(1),(a,b)=>b-a).Max()<=iris[0]*.04,
            "blink drawings close in measured openness order without reopening between closing stages");
        check(EdgeActions.TopResponses.All(a=>pack.Actions[a].SmoothFrames&&pack.Actions[a].Frames.All(f=>f.DurationMs==40)),
            "all top response keyframes use forty-millisecond samples with continuous presentation");
        var heads=frames.Select(f=>SideEdgeVerification.HeadHeight(f.Image)).ToArray();
        for(var i=0;i<frames.Length;i++)
        {
            var pose=new PngBitmapEncoder();pose.Frames.Add(BitmapFrame.Create(frames[i].Image));
            using var file=File.Create(Path.Combine(directory,$"top-registered-{i:00}.png"));pose.Save(file);
        }
        check(heads.Max()-heads.Min()<=5,
            "actual rendered top head heights remain stable across blink, look and tilt: "+string.Join(",",heads));
        check(frames.All(f=>f.EdgeAnchorY==pack.Manifest.EdgeTopAnchorY&&f.SwingSeatAnchorY==pack.Manifest.TopSwing!.SeatAnchorY),
            "top poses share fixed suspension and seat registration");
        check(pack.Actions["edge-top-idle"].Frames.Count==1&&EdgeActions.TopResponses.All(a=>
            ReferenceEquals(pack.Actions[a].Frames[0].Image,pack.Actions["edge-top-idle"].Frames[0].Image)
            &&ReferenceEquals(pack.Actions[a].Frames[^1].Image,pack.Actions["edge-top-idle"].Frames[0].Image)),
            "all top responses start and finish at the exact same idle drawing");
        // Show only painted keyframes, without the interpolator or swing transforms.
        var drawn=new GifBitmapEncoder();var drawnDelays=new List<int>();
        foreach(var action in EdgeActions.TopResponses)
            foreach(var frame in pack.Actions[action].Frames)
            {
                var visualFrame=new DrawingVisual();
                using(var draw=visualFrame.RenderOpen())
                {
                    draw.DrawRectangle(Brushes.WhiteSmoke,null,new Rect(0,0,256,352));
                    draw.DrawImage(frame.Image,new Rect(0,0,256,352));
                }
                var capture=new RenderTargetBitmap(256,352,96,96,PixelFormats.Pbgra32);capture.Render(visualFrame);
                drawn.Frames.Add(BitmapFrame.Create(capture));drawnDelays.Add(frame.DurationMs/10);
            }
        using(var bytes=new MemoryStream())
        {drawn.Save(bytes);File.WriteAllBytes(Path.Combine(directory,"top-drawn-keyframes.gif"),ThroneMotionVerification.WithAnimationMetadata(bytes.ToArray(),drawnDelays));}
        CheckHeadInterpolation(check);
        foreach(var frame in frames)
        {
            var rgba=new FormatConvertedBitmap(frame.Image,PixelFormats.Bgra32,null,0);
            var stride=rgba.PixelWidth*4;var pixels=new byte[stride*rgba.PixelHeight];rgba.CopyPixels(pixels,stride,0);
            var y=(int)Math.Round(frame.SwingSeatAnchorY!.Value*rgba.PixelHeight);
            bool Contact(double center)=>Enumerable.Range(Math.Max(0,(int)(center*rgba.PixelWidth)-8),17)
                .Any(x=>pixels[y*stride+x*4+3]>=200);
            check(Contact(.5-pack.Manifest.TopSwing!.SeatHalfWidth)&&Contact(.5+pack.Manifest.TopSwing.SeatHalfWidth),
                "both top rope connections land on visible jade seat pixels");
            check(Math.Abs(frame.SwingSeatAnchorY.Value-frame.EdgeAnchorY!.Value
                -pack.Manifest.TopSwing.SeatAnchorY+pack.Manifest.EdgeTopAnchorY)<1e-9,
                "registered top poses keep a consistent suspension-to-seat distance");
        }
        var roots=(window.SwingRopeLeft.X1,window.SwingRopeLeft.Y1,window.SwingRopeRight.X1,window.SwingRopeRight.Y1);
        var encoder=new GifBitmapEncoder();var delays=new List<int>();var samples=new List<BitmapSource>();
        var labels=new[] {"闭眼微笑","左右张望","歪头微笑"};
        for(var index=0;index<EdgeActions.TopResponses.Length;index++)
        {
            var action=EdgeActions.TopResponses[index];var duration=pack.Actions[action].Duration;
            window.PlayCharacterInteraction();
            check(window.ScreenEdgeResponse==action,"top responses rotate "+action);
            var seen=new HashSet<BitmapSource>();
            var steps=(int)Math.Ceiling(duration/20);
            for(var step=0;step<steps;step++)
            {
                seen.Add(window.CurrentSpriteFrame.Image);
                var capture=VerticalEdgeVerification.Capture(window,ScreenEdge.Top,labels[index]);
                encoder.Frames.Add(BitmapFrame.Create(capture));delays.Add(2);
                if(step==steps/4||step==steps/2||step==steps*3/4)samples.Add(capture);
                window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(Math.Min(20,duration-step*20)));
                var frame=window.CurrentSpriteFrame;var art=window.CharacterArt;
                var height=Math.Min(art.Height,art.Width*frame.Image.PixelHeight/frame.Image.PixelWidth);
                var width=Math.Min(art.Width,art.Height*frame.Image.PixelWidth/frame.Image.PixelHeight);
                bool Attached(System.Windows.Shapes.Line rope,int sign)
                {
                    var actual=art.RenderTransform.Transform(new Point(sign*width*pack.Manifest.TopSwing!.SeatHalfWidth,
                        (art.Height-height)/2+height*frame.SwingSeatAnchorY!.Value-art.Height/2));
                    actual.Offset(115,16+148-art.Height/2);
                    return Math.Abs(actual.X-rope.X2)<.00001&&Math.Abs(actual.Y-rope.Y2)<.00001;
                }
                check(Attached(window.SwingRopeLeft,-1)&&Attached(window.SwingRopeRight,1)
                    && roots==(window.SwingRopeLeft.X1,window.SwingRopeLeft.Y1,window.SwingRopeRight.X1,window.SwingRopeRight.Y1)
                    && Math.Abs(window.EdgeSwing.Angle)<=4 && window.EdgeStretch.ScaleY==1 && window.EdgeShift.X==0,
                    "top pose keeps suspension roots fixed and transformed seat connected "+action+step);
                if(step==steps/2)
                {
                    window.BeginDetailsMenu();
                    var held=(window.CharacterArt.Source,window.EdgeSwing.Angle,window.EdgeShift.Y,window.SwingRopeLeft.X2,window.SwingRopeLeft.Y2);
                    var heldPixels=PixelHash((BitmapSource)window.CharacterArt.Source);
                    window.AdvanceCharacterAnimation(TimeSpan.FromSeconds(10));
                    check(held==(window.CharacterArt.Source,window.EdgeSwing.Angle,window.EdgeShift.Y,window.SwingRopeLeft.X2,window.SwingRopeLeft.Y2),
                        "menu freezes top expression, swing phase and suspension together "+action);
                    check(heldPixels==PixelHash((BitmapSource)window.CharacterArt.Source),
                        "menu pause also freezes pixels inside the reused interpolation bitmap "+action);
                    window.EndDetailsMenu();window.PlayCharacterInteraction();
                    check(window.ScreenEdgeResponse==action,"busy top clicks do not accumulate or skip poses "+action);
                    window.RefreshScreenEdgeBounds(new Rect(-1920,-200,1920,1000),size);
                    check(window.ScreenEdgeResponse==action,"top work-area change preserves the running gesture "+action);
                }
            }
            check(seen.Count>=4&&window.ScreenEdgeResponse is null&&window.CurrentCharacterFrame==CharacterFrame.EdgeIdle
                &&window.QuotaHost.Position==quota&&window.SwingRopes.Visibility==Visibility.Visible,
                "top gesture settles into the suspended base with fixed quota "+action);
        }
        window.PlayCharacterInteraction();check(window.ScreenEdgeResponse=="edge-top-peek","top response order wraps");
        window.WakeCharacterImmediately();
        check(window.EdgeSwing.Angle==0&&window.EdgeShift.Y==0&&window.SwingRopes.Visibility==Visibility.Collapsed,
            "detaching clears top registration and all suspension transforms");
        using(var bytes=new MemoryStream())
        {encoder.Save(bytes);File.WriteAllBytes(Path.Combine(directory,"top-edge-actions.gif"),ThroneMotionVerification.WithAnimationMetadata(bytes.ToArray(),delays));}
        var visual=new DrawingVisual();using(var draw=visual.RenderOpen())
            for(var i=0;i<samples.Count;i++)draw.DrawImage(samples[i],new Rect(i%3*680,i/3*390,680,390));
        var bitmap=new RenderTargetBitmap(2040,1170,96,96,PixelFormats.Pbgra32);bitmap.Render(visual);
        var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));
        using(var file=File.Create(Path.Combine(directory,"top-edge-contact-sheet.png")))png.Save(file);
        var player=new CharacterAnimation();player.Configure(pack);
        foreach(var action in EdgeActions.TopResponses)
        {
            player.Preview(action);player.Advance(TimeSpan.FromSeconds(30));
            check(player.Action=="edge-top-idle","top preview returns to its suspended base "+action);
            player.Preview(action);player.Low=true;
            check(player.Action=="low","low quota clears top preview "+action);
            player.Configure(pack);
        }
    }

    private static int VisibleIris(BitmapSource image)
    {
        var rgba=new FormatConvertedBitmap(image,PixelFormats.Bgra32,null,0);
        var width=rgba.PixelWidth;var bytes=new byte[width*rgba.PixelHeight*4];rgba.CopyPixels(bytes,width*4,0);
        var top=rgba.PixelHeight;var count=0;
        for(var y=0;y<rgba.PixelHeight;y++)for(var x=0;x<width;x++)
            if(bytes[(y*width+x)*4+3]>=200)top=Math.Min(top,y);
        // Fixed registered eye band excludes forehead jade, earrings and seat.
        for(var y=top+100;y<top+142;y++)for(var x=65;x<190;x++)
        {var p=(y*width+x)*4;if(bytes[p+3]>=200&&bytes[p+1]>bytes[p+2]*1.15&&bytes[p+1]>bytes[p]*1.05)count++;}
        return count;
    }

    private static void CheckHeadInterpolation(Action<bool,string> check)
    {
        BitmapSource Marker(int x)
        {
            var pixels=new byte[13*100*4];var eye=(5*13+x)*4;pixels[eye+2]=pixels[eye+3]=255;
            foreach(var seatX in new[]{2,10}){var seat=(80*13+seatX)*4;pixels[seat+1]=pixels[seat+3]=255;}
            return BitmapSource.Create(13,100,96,96,PixelFormats.Pbgra32,null,pixels,13*4);
        }
        var a=new LoadedFrame(Marker(3),40,EdgeAnchorY:0,SwingSeatAnchorY:.75,HeadAnchorX:3.0/13,HeadAnchorY:.05);
        var b=new LoadedFrame(Marker(9),40,EdgeAnchorY:0,SwingSeatAnchorY:.75,HeadAnchorX:9.0/13,HeadAnchorY:.05);
        var interpolator=new FrameInterpolator();var result=interpolator.Sample(a,b,.5);var bytes=new byte[13*100*4];result.CopyPixels(bytes,13*4,0);
        check(bytes[(5*13+6)*4+3]>=240&&bytes[(5*13+3)*4+3]==0&&bytes[(5*13+9)*4+3]==0,
            "swing interpolation aligns the moving head even when the suspension is above it");
        check(new[]{2,10}.All(x=>bytes[(80*13+x)*4+1]==255&&bytes[(80*13+x)*4+3]==255),
            "swing head interpolation leaves both lower rope attachment pixels fixed");
        var pack=CharacterCatalog.BuiltIns[0];
        var loop=new LoadedAction(true,new[]{a,b},SmoothFrames:true);
        var player=new CharacterAnimation();player.Configure(pack with {Actions=new Dictionary<string,LoadedAction>(pack.Actions){["edge-top-idle"]=loop}});
        player.Preview("edge-top-idle");player.Advance(TimeSpan.FromMilliseconds(60));var sample=player.Presentation;
        check(ReferenceEquals(sample.From.Image,b.Image)&&ReferenceEquals(sample.To.Image,a.Image)&&Math.Abs(sample.Fraction-.5)<1e-9,
            "an opted-in looping top clip interpolates from its final frame back to the first");
    }

    private static string PixelHash(BitmapSource image)
    {var bytes=new byte[image.PixelWidth*image.PixelHeight*4];image.CopyPixels(bytes,image.PixelWidth*4,0);return Convert.ToHexString(SHA256.HashData(bytes));}
}
