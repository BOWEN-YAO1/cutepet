using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CutePet.Desktop;

internal static class ScreenEdgeVerification
{
    internal static void Run(MainWindow window, string directory, Action<bool, string> check, Action<bool> low)
    {
        foreach (var dpi in new[] { 1.0, 1.5, 2.0 })
        {
            var area = new Rect(-1920, -240, 1920, 1040);
            var size = new Size(230 * dpi, 178 * dpi);
            foreach (var side in new[] { ScreenEdge.Left, ScreenEdge.Right })
            {
                var x = side == ScreenEdge.Left ? area.Left + 23 * dpi : area.Right - size.Width - 23 * dpi;
                var plan = ScreenEdgeLayout.Plan(area, size, new Point(x, area.Top - 80), dpi)!;
                check(plan.Side == side && area.Contains(new Rect(plan.Position, size)), "edge snap clamps negative-monitor bounds " + side + dpi);
                check(plan.Position.Y == area.Top, "edge snap excludes top taskbar area " + side + dpi);
                x = side == ScreenEdge.Left ? area.Left + 25 * dpi : area.Right - size.Width - 25 * dpi;
                check(ScreenEdgeLayout.Plan(area, size, new Point(x, 100), dpi) is null, "snap threshold follows DPI " + side + dpi);
            }
        }
        var screen = new Rect(0, 0, 1920, 1040);
        var pet = new Size(230, 178);
        check(ScreenEdgeLayout.Plan(screen, pet, new Point(-60, 1500), 1) is { Position.Y: 862 }, "partly offscreen release snaps safely above bottom taskbar");
        check(ScreenEdgeLayout.Plan(screen, pet, new Point(800, 0), 1, new[] { ScreenEdge.Left, ScreenEdge.Right }) is null,
            "legacy side-only packs do not attach to unsupported top edge");
        check(ScreenEdgeLayout.Plan(new Rect(0, 0, 200, 150), pet, new Point(), 1) is null, "oversized pets reject attachment");
        check(ScreenEdgeLayout.Plan(screen, pet, new Point(double.NaN, 0), 1) is null, "nonfinite release coordinates are rejected");

        low(false);
        window.SetCharacter(PetCharacter.Tianyi);
        if (window.Settings.PositionLocked) window.TogglePositionLock();
        if (!window.Settings.EdgeInteraction) window.ToggleEdgeInteraction();
        window.SetDetailsMode(DetailsMode.Hidden);
        window.PointerChanged(false);
        window.CharacterPointerChanged(false);
        window.WakeCharacterImmediately();
        var quota = window.QuotaHost.Position;
        bool Attach(ScreenEdge side = ScreenEdge.Left) => window.CompletePetDrag(screen, pet,
            new Point(side == ScreenEdge.Left ? 12 : 1680, 350), 1);
        check(Attach() && window.ScreenEdgeActive && window.CurrentCharacterFrame == CharacterFrame.EdgeIdle,
            "manual character release activates package edge base pose");
        check(window.Scene.ClipToBounds && window.GroundShadow.Visibility == Visibility.Hidden,
            "only pet artwork is clipped and its standing shadow is hidden");
        window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(320));
        check(window.EdgeShift.X < -65 && window.EdgeMirror.ScaleX == 1 && window.QuotaHost.Position == quota,
            "left edge aligns the package anchor while quota remains fixed");
        window.PlayCharacterInteraction();
        window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(window.SelectedCharacter.Actions["edge-peek"].Duration / 2));
        var smile = window.CharacterArt.Source;
        check(window.CurrentCharacterFrame == CharacterFrame.EdgePeek && window.ScreenEdgePeekOffset == 0
            && !ReferenceEquals(smile,window.SelectedCharacter.Actions["edge-idle"].Frames[0].Image),
            "click reaches a distinct drawn leaning pose without whole-body peeking translation");
        window.BeginDetailsMenu();
        window.AdvanceCharacterAnimation(TimeSpan.FromMinutes(1));
        check(ReferenceEquals(smile, window.CharacterArt.Source) && window.ScreenEdgePeekOffset == 0,
            "menu pauses both edge expression and movement clocks");
        window.EndDetailsMenu();
        window.PlayCharacterInteraction();
        window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(window.SelectedCharacter.Actions["edge-peek"].Duration / 2));
        check(window.CurrentCharacterFrame == CharacterFrame.EdgeIdle && window.ScreenEdgePeekOffset == 0,
            "repeat clicks do not accumulate responses and peek returns to edge base");
        window.StartCharacterBlink();
        window.AdvanceAmbient(TimeSpan.FromMinutes(1));
        window.AdvanceCloud(TimeSpan.FromMinutes(1));
        check(window.ScreenEdgeActive && !window.CloudActive && !window.CharacterResting,
            "edge attachment blocks standing blink, automatic cloud and rest");
        window.WakeCharacterImmediately(); // Same path used when a real character drag begins.
        check(!window.ScreenEdgeActive && !window.Scene.ClipToBounds && window.EdgeShift.X == 0 && window.EdgeMirror.ScaleX == 1,
            "drag away restores standing graphics and interaction state");
        Attach(ScreenEdge.Right);
        window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(320));
        check(window.EdgeMirror.ScaleX == -1 && window.EdgeShift.X > 65 && window.QuotaHost.Position == quota,
            "right edge mirrors only character art and keeps quota intact");
        window.CharacterPointerChanged(true);
        window.AdvanceAmbient(TimeSpan.FromMilliseconds(400));
        check(window.CurrentCharacterFrame == CharacterFrame.EdgePeek, "edge hover triggers one finite response");
        window.CharacterPointerChanged(false);
        window.RecallPet();
        check(!window.ScreenEdgeActive && window.LastRecallPosition is not null && window.QuotaHost.Position == quota,
            "recall leaves the border without moving quota");
        Attach(); window.TogglePositionLock();
        check(window.ScreenEdgeActive && !Attach(), "locking preserves the attached pose while preventing new drags");
        window.TogglePositionLock();
        window.ToggleEdgeInteraction();
        check(!window.ScreenEdgeActive && !Attach(), "disabling edge interaction detaches and suppresses snap");
        var saved = new PreferencesStore(Path.Combine(directory, "edge-prefs"));
        saved.Save(window.Settings);
        check(!saved.Load().EdgeInteraction && new Preferences().EdgeInteraction, "edge preference persists and old settings default to enabled");
        window.ToggleEdgeInteraction();
        Attach(); window.SetCharacterScale(1.2);
        check(!window.ScreenEdgeActive, "changing pet size clears its obsolete edge anchor");
        window.SetCharacterScale(1);
        Attach(); window.SetQuotaScale(1.2);
        check(window.ScreenEdgeActive && window.QuotaHost.Position == quota, "quota-only size changes keep the pet attached");
        window.SetQuotaScale(1);
        low(true);
        check(!window.ScreenEdgeActive && window.CurrentCharacterFrame == CharacterFrame.Low && !Attach(),
            "fresh low quota exits and prevents edge pose");
        low(false);
        Attach(); window.RoamDesktop();
        check(!window.ScreenEdgeActive && window.CloudActive, "manual cloud trip explicitly leaves the border");
        window.WakeCharacterImmediately();
        Attach(); window.ToggleCharacterRest();
        check(!window.ScreenEdgeActive && window.CharacterResting, "manual rest leaves border before conjuring throne");
        window.WakeCharacterImmediately();
        Attach(); window.HidePet(); window.RestorePet();
        check(!window.ScreenEdgeActive && window.CurrentCharacterFrame == CharacterFrame.Idle,
            "hide and restore clear edge state without restoring a clipped standing sprite");
        Attach(); window.SetCharacter(PetCharacter.Cat);
        check(!window.ScreenEdgeActive && !Attach(), "legacy cat has no edge artwork and keeps normal drag behavior");

        var player = new CharacterAnimation();
        player.Configure(window.Characters.Find("tianyi")); player.Preview("edge-peek");
        player.Advance(TimeSpan.FromMilliseconds(window.Characters.Find("tianyi").Actions["edge-peek"].Duration));
        check(player.Action == "edge-idle", "manager finite edge preview returns to its edge base");
        player.Low = true;
        check(player.Action == "low", "player low state clears standalone edge preview");
        window.SetCharacter(PetCharacter.Tianyi);
        SideEdgeVerification.Run(window, directory, check);
        Record(window, directory, check);
        window.WakeCharacterImmediately();
    }
    private static void Record(MainWindow window, string directory, Action<bool, string> check)
    {
        var encoder = new GifBitmapEncoder();
        var delays = new List<int>();
        foreach (var side in new[] { ScreenEdge.Left, ScreenEdge.Right })
        {
            window.WakeCharacterImmediately();
            window.CompletePetDrag(new Rect(0, 0, 1920, 1040), new Size(230, 178), new Point(side == ScreenEdge.Left ? 0 : 1690, 350), 1);
            window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(320));
            window.PlayCharacterInteraction();
            for (var i = 0; i < 15; i++)
            {
                var frame = Capture(window, side);
                encoder.Frames.Add(BitmapFrame.Create(frame)); delays.Add(10);
                window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(100));
            }
        }
        using var bytes = new MemoryStream(); encoder.Save(bytes);
        File.WriteAllBytes(Path.Combine(directory, "screen-edge.gif"), ThroneMotionVerification.WithAnimationMetadata(bytes.ToArray(), delays));
        check(encoder.Frames.Count == 30, "edge preview captures both actual WPF border poses and fixed quota card");
    }
    internal static RenderTargetBitmap Capture(MainWindow window, ScreenEdge side, string? label = null)
    {
        const double width = 680, height = 390;
        var visual = new DrawingVisual();
        using (var draw = visual.RenderOpen())
        {
            draw.DrawRectangle(new SolidColorBrush(Color.FromRgb(239, 245, 244)), null, new Rect(0, 0, width, height));
            draw.DrawRectangle(null, new Pen(new SolidColorBrush(Color.FromRgb(180, 201, 194)), 2), new Rect(20, 20, 640, 330));
            draw.DrawImage(WindowPreview.Surface((FrameworkElement)window.Content, window.Width, window.Height, 192),
                new Rect(side == ScreenEdge.Left ? 20 : 660 - window.Width * 1.8, 20, window.Width * 1.8, window.Height * 1.8));
            var card = window.QuotaHost;
            draw.DrawImage(WindowPreview.Surface((FrameworkElement)card.Content, card.Width, card.Height, 96), new Rect(290, 272, card.Width, card.Height));
            draw.DrawText(new FormattedText(label ?? (side == ScreenEdge.Left ? "屏幕左侧 · 探头" : "屏幕右侧 · 探头"),
                System.Globalization.CultureInfo.GetCultureInfo("zh-CN"), FlowDirection.LeftToRight, new Typeface("Microsoft YaHei"),
                14, new SolidColorBrush(Color.FromRgb(75, 102, 88)), 1), new Point(20, 363));
        }
        var bitmap = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
        bitmap.Render(visual); return bitmap;
    }
}
