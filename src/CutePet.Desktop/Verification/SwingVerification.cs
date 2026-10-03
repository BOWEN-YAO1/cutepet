using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media.Imaging;

namespace CutePet.Desktop;

internal static class SwingVerification
{
    internal static void Run(MainWindow window, string directory, Action<bool, string> check)
    {
        window.SetCharacter(PetCharacter.Tianyi);
        if (window.Settings.PositionLocked) window.TogglePositionLock();
        if (!window.Settings.EdgeInteraction) window.ToggleEdgeInteraction();
        window.PointerChanged(false); window.CharacterPointerChanged(false);
        var screen = new Rect(0, 0, 1920, 1040);
        var size = new Size(230, 178);
        var quota = window.QuotaHost.Position;
        void Attach() => window.CompletePetDrag(screen, size, new Point(800, 0), 1);
        Attach();
        check(window.SwingRopes.Visibility == Visibility.Visible && window.SwingRopes.Opacity == 0,
            "swing suspension appears with a gradual entry");
        window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(800));
        check(Math.Abs(window.EdgeSwing.Angle - 3) < 0.00001 && window.EdgeStretch.ScaleY == 1,
            "swing reaches a small positive extreme without stretching artwork");
        CheckRopes();
        var leftTop = window.SwingRopeLeft.X1;
        var rightTop = window.SwingRopeRight.X1;
        var angle = window.EdgeSwing.Angle;
        var endpoint = new Point(window.SwingRopeLeft.X2, window.SwingRopeLeft.Y2);
        window.BeginDetailsMenu();
        window.AdvanceCharacterAnimation(TimeSpan.FromMinutes(1));
        check(window.EdgeSwing.Angle == angle && new Point(window.SwingRopeLeft.X2, window.SwingRopeLeft.Y2) == endpoint,
            "menu freezes swing phase and both rope connections");
        window.EndDetailsMenu();
        window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(1600));
        check(Math.Abs(window.EdgeSwing.Angle + 3) < 0.00001 && window.SwingRopeLeft.X1 == leftTop
            && window.SwingRopeRight.X1 == rightTop && window.SwingRopeLeft.Y1 == 0 && window.SwingRopeRight.Y1 == 0,
            "swing reverses direction while both top attachments stay fixed");
        CheckRopes();
        Attach(); window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(300));
        window.PlayCharacterInteraction(); window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(500));
        check(Math.Abs(window.EdgeSwing.Angle - 4) < 0.00001,
            "click smoothly increases swing amplitude at the finite response midpoint");
        window.PlayCharacterInteraction(); window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(500));
        check(window.ScreenEdgePeekOffset == 0 && Math.Abs(window.EdgeSwing.Angle) <= 3,
            "repeated click does not accumulate swing pushes");
        angle = window.EdgeSwing.Angle;
        window.RefreshScreenEdgeBounds(new Rect(100, 40, 1600, 900), new Size(345, 267));
        check(window.EdgeSwing.Angle == angle && window.SwingRopeLeft.Y1 == 0 && window.QuotaHost.Position == quota,
            "swing reanchors after DPI and work-area changes without moving quota");
        CheckRopes();
        window.TogglePositionLock(); window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(400));
        check(window.ScreenEdgeActive && window.SwingRopes.Visibility == Visibility.Visible,
            "position lock permits the existing suspended swing to animate");
        window.TogglePositionLock(); window.RecallPet();
        check(window.EdgeSwing.Angle == 0 && window.SwingRopes.Visibility == Visibility.Collapsed
            && window.QuotaHost.Position == quota, "recall removes ropes and rotation without affecting quota");
        foreach (var point in new[] { new Point(0, 350), new Point(800, 862) })
        {
            window.CompletePetDrag(screen, size, point, 1); window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(800));
            check(window.EdgeSwing.Angle == 0 && window.SwingRopes.Visibility == Visibility.Collapsed,
                "side and bottom poses never inherit swing layers " + point);
        }
        window.SetCharacter(PetCharacter.Cat);
        check(window.EdgeSwing.Angle == 0 && window.SwingRopes.Visibility == Visibility.Collapsed,
            "switching character clears suspension graphics");
        var legacyDirectory = Path.Combine(window.Characters.Root, "legacy-top-swing-test");
        Directory.CreateDirectory(legacyDirectory);
        var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(window.SelectedCharacter.Idle.Frames[0].Image));
        using (var file = File.Create(Path.Combine(legacyDirectory, "idle.png"))) png.Save(file);
        var clip = new CharacterAction { Loop = true, Frames = new() { new("idle.png", 1000) } };
        var manifest = new CharacterManifest { Id = "legacy-top-swing-test", Name = "旧上侧测试", Actions = new() { ["idle"] = clip, ["edge-top-idle"] = clip } };
        File.WriteAllText(Path.Combine(legacyDirectory, "character.json"), JsonSerializer.Serialize(manifest, CharacterPackLoader.Json));
        window.Characters.Reload(); window.SetCharacterPackage(manifest.Id);
        Attach(); window.AdvanceCharacterAnimation(TimeSpan.FromSeconds(1));
        check(window.ScreenEdgeActive && window.EdgeSwing.Angle == 0 && window.SwingRopes.Visibility == Visibility.Collapsed,
            "legacy top-only pack without swing configuration keeps its original pose");
        var legacy = window.SelectedCharacter;
        window.SetCharacter(PetCharacter.Tianyi); window.Characters.Remove(legacy);
        var customDirectory = Path.Combine(window.Characters.Root, "short-swing-test");
        Directory.CreateDirectory(customDirectory);
        png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(window.Characters.Find("cat").Idle.Frames[0].Image));
        using (var file = File.Create(Path.Combine(customDirectory, "idle.png"))) png.Save(file);
        manifest = manifest with { Id = "short-swing-test", Name = "不同尺寸秋千测试", DisplayHeight = 100, TopSwing = new() };
        File.WriteAllText(Path.Combine(customDirectory, "character.json"), JsonSerializer.Serialize(manifest, CharacterPackLoader.Json));
        window.Characters.Reload(); window.SetCharacterPackage(manifest.Id);
        Attach(); window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(800));
        check(window.SwingRopes.Visibility == Visibility.Visible && window.CharacterArt.Height == 100,
            "custom swing supports a shorter manifest display size");
        CheckRopes();
        var custom = window.SelectedCharacter;
        window.SetCharacter(PetCharacter.Tianyi); window.Characters.Remove(custom);
        Attach(); window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(320));
        var encoder = new GifBitmapEncoder(); var delays = new List<int>();
        for (var i = 0; i < 64; i++)
        {
            var frame = VerticalEdgeVerification.Capture(window, ScreenEdge.Top);
            encoder.Frames.Add(BitmapFrame.Create(frame)); delays.Add(10);
            if (i == 8) window.PlayCharacterInteraction();
            window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(100));
        }
        using var bytes = new MemoryStream(); encoder.Save(bytes);
        File.WriteAllBytes(Path.Combine(directory, "top-swing.gif"), ThroneMotionVerification.WithAnimationMetadata(bytes.ToArray(), delays));
        check(window.QuotaHost.Position == quota && encoder.Frames.Count == 64,
            "actual WPF swing preview completes two cycles with stationary quota");
        window.WakeCharacterImmediately();

        void CheckRopes()
        {
            var image = window.SelectedCharacter.Actions["edge-top-idle"].Frames[0].Image;
            var art = window.CharacterArt;
            var height = Math.Min(art.Height, art.Width * image.PixelHeight / image.PixelWidth);
            var width = Math.Min(art.Width, art.Height * image.PixelWidth / image.PixelHeight);
            var swing = window.SelectedCharacter.Manifest.TopSwing!;
            foreach (var (rope, sign) in new[] { (window.SwingRopeLeft, -1), (window.SwingRopeRight, 1) })
            {
                // Compare against WPF's composed artwork transform, rather than duplicating the renderer's trigonometry.
                var local = new Point(sign * width * swing.SeatHalfWidth, (art.Height - height) / 2 + height * swing.SeatAnchorY - art.Height / 2);
                var actual = art.RenderTransform.Transform(local);
                actual.Offset(115, 16 + 148 - art.Height / 2);
                check(Math.Abs(actual.X - rope.X2) < 0.00001 && Math.Abs(actual.Y - rope.Y2) < 0.00001,
                    "rope lower endpoint follows the actual transformed board corner " + sign);
            }
        }
    }
}
