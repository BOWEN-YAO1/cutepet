using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CutePet.Desktop;

internal static class BottomEdgeVerification
{
    internal static void Run(MainWindow window,string directory,Action<bool,string> check)
    {
        var area=new Rect(-1920,-240,1920,1040);
        var size=new Size(230,178);
        var quota=window.QuotaHost.Position;
        window.WakeCharacterImmediately();
        window.CompletePetDrag(area,size,new Point(-1000,area.Bottom-size.Height),1);
        window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(320));
        var frames=EdgeActions.BottomResponses.SelectMany(a=>window.SelectedCharacter.Actions[a].Frames).Select(f=>f.Image).Distinct().ToArray();
        check(frames.Length==16 && frames.All(f=>f is CroppedBitmap {IsFrozen:true}),
            "bottom gestures load sixteen distinct cached anatomical poses");
        foreach(var sprite in frames)
        {
            var frame=EdgeActions.BottomResponses.SelectMany(a=>window.SelectedCharacter.Actions[a].Frames).First(f=>f.Image==sprite);
            var rgba=new FormatConvertedBitmap(sprite,PixelFormats.Bgra32,null,0);
            var stride=rgba.PixelWidth*4;var pixels=new byte[stride*rgba.PixelHeight];rgba.CopyPixels(pixels,stride,0);
            var y=(int)Math.Round(frame.EdgeAnchorY!.Value*rgba.PixelHeight);
            bool Contact(double from,double to)=>Enumerable.Range((int)(from*rgba.PixelWidth),(int)((to-from)*rgba.PixelWidth))
                .Any(x=>pixels[y*stride+x*4+3]>=200);
            check(Contact(.18,.45)&&Contact(.55,.82),"both visible sleeves actually meet their registered support line");
        }
        var encoder=new GifBitmapEncoder();
        var delays=new List<int>();
        var samples=new List<BitmapSource>();
        var captions=new[] {"抬头微笑","左右张望","歪头微笑"};
        for(var index=0;index<EdgeActions.BottomResponses.Length;index++)
        {
            var action=EdgeActions.BottomResponses[index];
            var duration=window.SelectedCharacter.Actions[action].Duration;
            window.PlayCharacterInteraction();
            check(window.ScreenEdgeResponse==action,"bottom supported responses rotate "+action);
            var seen=new HashSet<BitmapSource>();
            for(var step=0;step<16;step++)
            {
                seen.Add((BitmapSource)window.CharacterArt.Source);
                var capture=VerticalEdgeVerification.Capture(window,ScreenEdge.Bottom,captions[index]);
                encoder.Frames.Add(BitmapFrame.Create(capture));
                delays.Add((int)Math.Round(duration/160));
                if(step is 3 or 8 or 13)samples.Add(capture);
                window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(duration/16));
                var frame=window.CurrentSpriteFrame;
                var height=Math.Min(window.CharacterArt.Height,window.CharacterArt.Width*frame.Image.PixelHeight/frame.Image.PixelWidth);
                var top=16+148-window.CharacterArt.Height+(window.CharacterArt.Height-height)/2;
                check(Math.Abs(top+height*frame.EdgeAnchorY!.Value+window.EdgeShift.Y-178)<.001
                    && window.EdgeShift.X==0 && window.EdgeStretch.ScaleY==1 && window.SideEdgeTilt.Angle==0
                    && window.ScreenEdgePeekOffset==0,
                    "bottom pose keeps elbows at the work-area floor without body stretching "+action+step);
                if(step==7)
                {
                    window.BeginDetailsMenu();
                    var held=(window.EdgeShift.Y,window.CharacterArt.Source);
                    window.AdvanceCharacterAnimation(TimeSpan.FromSeconds(10));
                    check(held==(window.EdgeShift.Y,window.CharacterArt.Source),"menu freezes bottom support and pose "+action);
                    window.EndDetailsMenu();
                    window.PlayCharacterInteraction();
                    check(window.ScreenEdgeResponse==action,"busy bottom clicks do not queue or skip responses "+action);
                    window.RefreshScreenEdgeBounds(new Rect(-1920,-200,1920,1000),size);
                    check(window.ScreenEdgeResponse==action,"bottom reanchor preserves the running clip "+action);
                }
            }
            check(seen.Count>=4 && window.ScreenEdgeResponse is null && window.CurrentCharacterFrame==CharacterFrame.EdgeIdle
                && window.QuotaHost.Position==quota && area.Contains(new Rect(window.ScreenEdgeAttachment!.Position,size)),
                "bottom articulated clip returns to chin-rest with safe HWND and fixed quota "+action);
        }
        window.PlayCharacterInteraction();
        check(window.ScreenEdgeResponse=="edge-bottom-peek","bottom response order wraps without accumulating clicks");
        window.WakeCharacterImmediately();
        check(!window.ScreenEdgeActive && window.EdgeShift.Y==0 && window.EdgeStretch.ScaleY==1,
            "detaching clears bottom registration and clipping");
        using(var bytes=new MemoryStream())
        {
            encoder.Save(bytes);
            File.WriteAllBytes(Path.Combine(directory,"bottom-edge-actions.gif"),ThroneMotionVerification.WithAnimationMetadata(bytes.ToArray(),delays));
        }
        var contact=new DrawingVisual();
        using(var draw=contact.RenderOpen())
            for(var i=0;i<samples.Count;i++)draw.DrawImage(samples[i],new Rect((i%3)*680,(i/3)*390,680,390));
        var bitmap=new RenderTargetBitmap(2040,1170,96,96,PixelFormats.Pbgra32);bitmap.Render(contact);
        var png=new PngBitmapEncoder();png.Frames.Add(BitmapFrame.Create(bitmap));
        using(var file=File.Create(Path.Combine(directory,"bottom-edge-contact-sheet.png")))png.Save(file);
        var player=new CharacterAnimation();player.Configure(window.SelectedCharacter);
        foreach(var action in EdgeActions.BottomResponses)
        {
            player.Preview(action);player.Advance(TimeSpan.FromSeconds(30));
            check(player.Action=="edge-bottom-idle","standalone bottom preview returns to matching support pose "+action);
            player.Preview(action);player.Low=true;
            check(player.Action=="low","fresh low quota clears bottom preview "+action);
        }
    }
}
