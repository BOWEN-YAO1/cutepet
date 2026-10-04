using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        check(frames.Length==16&&frames.All(f=>f.Image is CroppedBitmap {IsFrozen:true}),
            "top gestures share sixteen distinct frozen seated poses");
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
            for(var step=0;step<16;step++)
            {
                seen.Add((BitmapSource)window.CharacterArt.Source);
                var capture=VerticalEdgeVerification.Capture(window,ScreenEdge.Top,labels[index]);
                encoder.Frames.Add(BitmapFrame.Create(capture));delays.Add((int)Math.Round(duration/160));
                if(step is 3 or 8 or 13)samples.Add(capture);
                window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(duration/16));
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
                if(step==7)
                {
                    window.BeginDetailsMenu();
                    var held=(window.CharacterArt.Source,window.EdgeSwing.Angle,window.EdgeShift.Y,window.SwingRopeLeft.X2,window.SwingRopeLeft.Y2);
                    window.AdvanceCharacterAnimation(TimeSpan.FromSeconds(10));
                    check(held==(window.CharacterArt.Source,window.EdgeSwing.Angle,window.EdgeShift.Y,window.SwingRopeLeft.X2,window.SwingRopeLeft.Y2),
                        "menu freezes top expression, swing phase and suspension together "+action);
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
}
