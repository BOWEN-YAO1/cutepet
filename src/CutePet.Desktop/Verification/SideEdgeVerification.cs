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
        check(artwork.Length == 8 && artwork.All(f => f.Image is CroppedBitmap {IsFrozen: true}),
            "side gestures use eight distinct cached articulated poses");
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
        check(LowerVisible(peekFrames[4]) > LowerVisible(peekFrames[0]) * 1.5,
            "deeper leaning artwork progressively exposes more of the connected lower silhouette");
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
                for (var step = 0; step < 20; step++)
                {
                    var frame = ScreenEdgeVerification.Capture(window, side, (side == ScreenEdge.Left ? "左侧 · " : "右侧 · ") + labels[index]);
                    encoder.Frames.Add(BitmapFrame.Create(frame)); delays.Add((int)Math.Round(duration / 200));
                    seen.Add((BitmapSource)window.CharacterArt.Source);
                    if (step == 7) samples.Add(frame);
                    window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(duration / 20));
                    maximumOutward = Math.Max(maximumOutward,window.ScreenEdgePeekOffset);
                    var current = window.CurrentSpriteFrame;
                    var width = Math.Min(window.CharacterArt.Width,window.CharacterArt.Height * current.Image.PixelWidth / current.Image.PixelHeight);
                    var gripX = 115 + (side == ScreenEdge.Left ? 1 : -1) * width * (current.EdgeAnchorX!.Value - .5) + window.EdgeShift.X;
                    aligned &= Math.Abs(gripX - (side == ScreenEdge.Left ? 0 : 230)) < .001 && window.SideEdgeTilt.Angle == 0;
                    if (step == 9)
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
        check(encoder.Frames.Count == 160, "side preview records four finite gestures on both borders from the actual WPF visual tree");
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
                window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(peekFrames[i].DurationMs));
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
}
