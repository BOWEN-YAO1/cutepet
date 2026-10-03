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
        foreach (var image in new[] {window.SelectedCharacter.Actions["edge-idle"].Frames[0].Image,
            window.SelectedCharacter.Actions["edge-peek"].Frames[1].Image})
        {
            var rgba = new FormatConvertedBitmap(image,PixelFormats.Bgra32,null,0);
            var stride = rgba.PixelWidth * 4;
            var pixels = new byte[stride * rgba.PixelHeight]; rgba.CopyPixels(pixels,stride,0);
            check(Alpha(.28125,.94) > 200 && Alpha(.402,.94) > 200,
                "side artwork contains both complete boots below the connected skirt and legs");
            byte Alpha(double x,double y) => pixels[(int)(y * rgba.PixelHeight) * stride + (int)(x * rgba.PixelWidth) * 4 + 3];
        }
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
                window.PlayCharacterInteraction();
                check(window.ScreenEdgeResponse == action, "side responses rotate through supported package actions " + side + action);
                for (var step = 0; step < 20; step++)
                {
                    var frame = ScreenEdgeVerification.Capture(window, side, (side == ScreenEdge.Left ? "左侧 · " : "右侧 · ") + labels[index]);
                    encoder.Frames.Add(BitmapFrame.Create(frame)); delays.Add((int)Math.Round(duration / 200));
                    if (step == 7) samples.Add(frame);
                    window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(duration / 20));
                    maximumOutward = Math.Max(maximumOutward,window.ScreenEdgePeekOffset);
                    if (step == 4 && action == "edge-shy")
                        check(window.ScreenEdgePeekOffset < -20, "shy response withdraws behind the border before emerging " + side);
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
                check(maximumOutward <= 2.0001, "side gesture keeps the grip close to the border while revealing a complete lower body " + side + action);
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
