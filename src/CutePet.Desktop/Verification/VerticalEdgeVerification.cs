using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CutePet.Desktop;

internal static class VerticalEdgeVerification
{
    internal static void Run(MainWindow window, string directory, Action<bool, string> check, Action<bool> low)
    {
        foreach (var dpi in new[] { 1.0, 1.5, 2.0 })
        {
            var full = new Rect(-1920, -240, 1920, 1080);
            var visibleTaskbar = new Rect(-1920, -240, 1920, 1080 - 40 * dpi);
            var area = ScreenEdgeLayout.SafeArea(visibleTaskbar, full, dpi);
            var size = new Size(230 * dpi, 178 * dpi);
            check(area == visibleTaskbar, "visible taskbar keeps its existing reserved height " + dpi);
            var hiddenArea = ScreenEdgeLayout.SafeArea(full, full, dpi);
            check(hiddenArea.Bottom == full.Bottom - 12 * dpi, "unreserved bottom leaves a DPI-scaled reveal strip " + dpi);
            foreach (var side in new[] { ScreenEdge.Top, ScreenEdge.Bottom })
            {
                var y = side == ScreenEdge.Top ? area.Top + 23 * dpi : area.Bottom - size.Height - 23 * dpi;
                var plan = ScreenEdgeLayout.Plan(area, size, new Point(-1000, y), dpi)!;
                check(plan.Side == side && area.Contains(new Rect(plan.Position, size)), "vertical snap stays in negative-coordinate work area " + side + dpi);
                y = side == ScreenEdge.Top ? area.Top + 25 * dpi : area.Bottom - size.Height - 25 * dpi;
                check(ScreenEdgeLayout.Plan(area, size, new Point(-1000, y), dpi) is null, "vertical snap obeys threshold " + side + dpi);
                var reanchored = ScreenEdgeLayout.Anchor(side, hiddenArea, size, plan.Position)!;
                check(hiddenArea.Contains(new Rect(reanchored.Position, size))
                    && (side != ScreenEdge.Bottom || reanchored.Position.Y + size.Height == hiddenArea.Bottom),
                    "taskbar change reanchors the same vertical side " + side + dpi);
            }
        }
        var screen = new Rect(0, 0, 1920, 1040);
        var pet = new Size(230, 178);
        check(ScreenEdgeLayout.Plan(screen, pet, new Point(20, 5), 1)!.Side == ScreenEdge.Top, "corner chooses closer top instead of side");
        check(ScreenEdgeLayout.Plan(screen, pet, new Point(5, 20), 1)!.Side == ScreenEdge.Left, "corner chooses closer side instead of top");
        check(ScreenEdgeLayout.Plan(screen, pet, new Point(0, 0), 1)!.Side == ScreenEdge.Left, "equal corner distances have stable priority");
        check(ScreenEdgeLayout.Plan(screen, pet, new Point(0, 0), 1, new[] { ScreenEdge.Bottom }) is null,
            "unsupported neighboring sides are never used as fallback poses");
        low(false);
        window.SetCharacter(PetCharacter.Tianyi);
        if (window.Settings.PositionLocked) window.TogglePositionLock();
        if (!window.Settings.EdgeInteraction) window.ToggleEdgeInteraction();
        window.SetDetailsMode(DetailsMode.Hidden);
        window.PointerChanged(false);
        var quota = window.QuotaHost.Position;
        var pack = window.SelectedCharacter;
        foreach (var side in new[] { ScreenEdge.Top, ScreenEdge.Bottom })
        {
            window.WakeCharacterImmediately();
            var released = new Point(800, side == ScreenEdge.Top ? 8 : 854);
            check(window.CompletePetDrag(screen, pet, released, 1) && window.ScreenEdgeAttachment!.Side == side,
                "host activates the intended vertical side " + side);
            check(ReferenceEquals(window.CharacterArt.Source, pack.Actions[EdgeActions.Base(side)].Frames[0].Image),
                "host selects dedicated vertical pose artwork " + side);
            window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(320));
            check(window.EdgeShift.X == 0 && window.EdgeMirror.ScaleX == 1
                && (side == ScreenEdge.Top ? window.EdgeShift.Y < 0 : window.EdgeShift.Y > 0),
                "vertical art aligns without horizontal mirroring or rotation " + side);
            window.PlayCharacterInteraction();
            var duration = pack.Actions[EdgeActions.Peek(EdgeActions.Base(side))].Duration;
            window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(duration / 2));
            var expression = window.CharacterArt.Source;
            check(ReferenceEquals(expression, pack.Actions[EdgeActions.Peek(EdgeActions.Base(side))].At(duration / 2))
                && window.ScreenEdgePeekOffset == (side == ScreenEdge.Top ? 8 : 0), "vertical response selects its package pose at midpoint " + side);
            var current=window.CurrentSpriteFrame;
            var height=Math.Min(window.CharacterArt.Height,window.CharacterArt.Width*current.Image.PixelHeight/current.Image.PixelWidth);
            var imageTop=16+148-window.CharacterArt.Height+(window.CharacterArt.Height-height)/2;
            var contact=imageTop+height*current.EdgeAnchorY!.Value+window.EdgeShift.Y;
            check(Math.Abs(contact-(side==ScreenEdge.Top ? 0 : 178))<.001 && window.EdgeStretch.ScaleY == 1,
                "vertical response keeps the hand or elbow contact line fixed " + side);
            var movedArea = new Rect(100, 40, 1600, 900);
            window.RefreshScreenEdgeBounds(movedArea, new Size(345, 267));
            check(window.ScreenEdgeAttachment!.Area == movedArea && window.ScreenEdgeAttachment.Side == side
                && movedArea.Contains(new Rect(window.ScreenEdgeAttachment.Position, window.ScreenEdgeAttachment.PetSize))
                && ReferenceEquals(expression, window.CharacterArt.Source) && window.ScreenEdgePeekOffset == (side == ScreenEdge.Top ? 8 : 0),
                "work-area and DPI changes preserve side and ongoing expression " + side);
            window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(duration / 2));
            check(window.CurrentCharacterFrame == CharacterFrame.EdgeIdle && window.ScreenEdgePeekOffset == 0
                && window.QuotaHost.Position == quota, "vertical response settles without affecting quota " + side);
            window.TogglePositionLock();
            window.PlayCharacterInteraction(); window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(500));
            check(window.CurrentCharacterFrame == CharacterFrame.EdgePeek, "locked vertical pose keeps click responses " + side);
            window.TogglePositionLock();
            window.RecallPet();
            check(!window.ScreenEdgeActive && window.EdgeShift.Y == 0 && window.QuotaHost.Position == quota,
                "recall clears vertical clipping and keeps quota fixed " + side);
            window.CompletePetDrag(screen, pet, released, 1);
            low(true);
            check(!window.ScreenEdgeActive && window.CurrentCharacterFrame == CharacterFrame.Low,
                "low quota clears vertical base and response " + side);
            low(false);
            window.CompletePetDrag(screen, pet, released, 1);
            window.RefreshScreenEdgeBounds(new Rect(0, 0, 100, 100), pet);
            check(!window.ScreenEdgeActive && window.EdgeShift.Y == 0,
                "too-small replacement screen safely exits vertical pose " + side);
        }
        var player = new CharacterAnimation();
        foreach (var side in new[] { ScreenEdge.Top, ScreenEdge.Bottom })
        {
            player.Configure(pack); player.Preview(EdgeActions.Peek(EdgeActions.Base(side)));
            player.Advance(TimeSpan.FromSeconds(2));
            check(player.Action == EdgeActions.Base(side), "manager response returns to matching vertical base " + side);
        }
        var legacy = pack with { Actions = pack.Actions.Where(pair => !pair.Key.StartsWith("edge-top-") && !pair.Key.StartsWith("edge-bottom-"))
            .ToDictionary(pair => pair.Key, pair => pair.Value) };
        check(EdgeActions.Supported(legacy).SequenceEqual(new[] { ScreenEdge.Left, ScreenEdge.Right }),
            "legacy side-only packages retain exactly their original supported sides");
        TopEdgeVerification.Run(window,directory,check);
        BottomEdgeVerification.Run(window,directory,check);
        Record(window, directory, check);
        window.WakeCharacterImmediately();
    }
    private static void Record(MainWindow window, string directory, Action<bool, string> check)
    {
        var encoder = new GifBitmapEncoder();
        var delays = new List<int>();
        foreach (var side in new[] { ScreenEdge.Top, ScreenEdge.Bottom })
        {
            window.WakeCharacterImmediately();
            window.CompletePetDrag(new Rect(0, 0, 1920, 1040), new Size(230, 178), new Point(800, side == ScreenEdge.Top ? 0 : 862), 1);
            window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(320)); window.PlayCharacterInteraction();
            for (var i = 0; i < 15; i++)
            {
                var frame = Capture(window, side);
                encoder.Frames.Add(BitmapFrame.Create(frame)); delays.Add(10);
                if (i == 5)
                {
                    var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(frame));
                    using var file = File.Create(Path.Combine(directory, "edge-" + side.ToString().ToLowerInvariant() + ".png")); png.Save(file);
                }
                window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(100));
            }
        }
        using var bytes = new MemoryStream(); encoder.Save(bytes);
        File.WriteAllBytes(Path.Combine(directory, "vertical-edge.gif"), ThroneMotionVerification.WithAnimationMetadata(bytes.ToArray(), delays));
        check(encoder.Frames.Count == 30, "vertical preview uses actual WPF top and bottom artwork with fixed quota and taskbar illustration");
    }
    internal static RenderTargetBitmap Capture(MainWindow window, ScreenEdge side, string? label = null)
    {
        var visual = new DrawingVisual();
        using (var draw = visual.RenderOpen())
        {
            draw.DrawRectangle(new SolidColorBrush(Color.FromRgb(239, 245, 244)), null, new Rect(0, 0, 680, 390));
            draw.DrawRectangle(null, new Pen(new SolidColorBrush(Color.FromRgb(180, 201, 194)), 2), new Rect(20, 20, 640, 330));
            draw.DrawRectangle(new SolidColorBrush(Color.FromRgb(201, 220, 212)), null, new Rect(20, 350, 640, 18));
            draw.DrawImage(WindowPreview.Surface((FrameworkElement)window.Content, window.Width, window.Height, 192),
                new Rect(340 - window.Width * 0.9, side == ScreenEdge.Top ? 20 : 350 - window.Height * 1.8, window.Width * 1.8, window.Height * 1.8));
            var card = window.QuotaHost;
            draw.DrawImage(WindowPreview.Surface((FrameworkElement)card.Content, card.Width, card.Height, 96), new Rect(525, 250, card.Width, card.Height));
            draw.DrawText(new FormattedText(label ?? (side == ScreenEdge.Top ? "上侧 · 悬挂秋千，轻轻摇摆" : "下侧 · 趴在任务栏上沿托腮"),
                System.Globalization.CultureInfo.GetCultureInfo("zh-CN"), FlowDirection.LeftToRight, new Typeface("Microsoft YaHei"),
                13, new SolidColorBrush(Color.FromRgb(75, 102, 88)), 1), new Point(20, 371));
        }
        var bitmap = new RenderTargetBitmap(680, 390, 96, 96, PixelFormats.Pbgra32); bitmap.Render(visual); return bitmap;
    }
}
