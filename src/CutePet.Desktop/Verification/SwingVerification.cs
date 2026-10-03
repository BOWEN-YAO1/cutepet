using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
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
        var pendants = window.SwingDecorations.Children.OfType<Image>().ToArray();
        var scenery = window.SwingScenery.Children.OfType<Image>().ToArray();
        check(window.SwingScenery.Visibility == Visibility.Visible && scenery.Length == 2
            && scenery.All(p => ReferenceEquals(p.Source, window.SelectedCharacter.SwingSceneryImage) && !p.IsHitTestVisible)
            && ((ScaleTransform)scenery[1].RenderTransform).ScaleX == -1, "side scenery reuses one cached image with right-side mirroring");
        check(scenery.All(p => Canvas.GetLeft(p) >= 0 && Canvas.GetTop(p) >= 0
            && Canvas.GetLeft(p) + p.Width <= 230 && Canvas.GetTop(p) + p.Height <= 178)
            && window.QuotaHost.Position == quota, "side scenery stays inside the pet scene and leaves quota fixed");
        var ribbons = window.SwingScenery.Children.OfType<System.Windows.Shapes.Path>().Take(2).ToArray();
        var sceneryRopes = new[] { window.SwingRopeLeft, window.SwingRopeRight };
        for (var side = 0; side < 2; side++)
        {
            var start = ((PathGeometry)ribbons[side].Data).Figures[0].StartPoint;
            var rope = sceneryRopes[side];
            check(Math.Abs(start.X - rope.X1 - (rope.X2 - rope.X1) * 0.07) < 0.00001
                && Math.Abs(start.Y - rope.Y1 - (rope.Y2 - rope.Y1) * 0.07) < 0.00001,
                "floating scenery ribbon remains attached to its swing rope " + side);
        }
        var sceneryTop = Canvas.GetTop(scenery[0]);
        var star = window.SwingScenery.Children.OfType<System.Windows.Shapes.Path>().Last();
        var starOpacity = star.Opacity;
        check(window.SwingDecorations.Visibility == Visibility.Visible && pendants.Length == 4
            && pendants.All(p => ReferenceEquals(p.Source, window.SelectedCharacter.SwingOrnamentImage) && !p.IsHitTestVisible),
            "four cached ornaments decorate ropes without intercepting pet gestures");
        CheckOrnaments();
        var pendantPoint = new Point(Canvas.GetLeft(pendants[0]), Canvas.GetTop(pendants[0]));
        var pendantAngle = ((RotateTransform)pendants[0].RenderTransform).Angle;
        var leftTop = window.SwingRopeLeft.X1;
        var rightTop = window.SwingRopeRight.X1;
        var angle = window.EdgeSwing.Angle;
        var endpoint = new Point(window.SwingRopeLeft.X2, window.SwingRopeLeft.Y2);
        window.BeginDetailsMenu();
        window.AdvanceCharacterAnimation(TimeSpan.FromMinutes(1));
        check(window.EdgeSwing.Angle == angle && new Point(window.SwingRopeLeft.X2, window.SwingRopeLeft.Y2) == endpoint,
            "menu freezes swing phase and both rope connections");
        check(new Point(Canvas.GetLeft(pendants[0]), Canvas.GetTop(pendants[0])) == pendantPoint
            && ((RotateTransform)pendants[0].RenderTransform).Angle == pendantAngle,
            "menu also freezes pendant placement and delayed sway");
        check(Canvas.GetTop(scenery[0]) == sceneryTop && star.Opacity == starOpacity,
            "menu freezes side-cloud floating and star twinkle");
        window.EndDetailsMenu();
        window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(1600));
        check(Math.Abs(window.EdgeSwing.Angle + 3) < 0.00001 && window.SwingRopeLeft.X1 == leftTop
            && window.SwingRopeRight.X1 == rightTop && window.SwingRopeLeft.Y1 == 0 && window.SwingRopeRight.Y1 == 0,
            "swing reverses direction while both top attachments stay fixed");
        CheckRopes();
        CheckOrnaments();
        check(new Point(Canvas.GetLeft(pendants[0]), Canvas.GetTop(pendants[0])) != pendantPoint
            && Math.Abs(((RotateTransform)pendants[0].RenderTransform).Angle) <= 4,
            "pendants follow reversed ropes with a bounded separate sway");
        check(Canvas.GetTop(scenery[0]) != sceneryTop && star.Opacity >= 0.25 && star.Opacity <= 0.85,
            "side clouds resume floating with bounded star brightness");
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
        check(window.SwingDecorations.Visibility == Visibility.Collapsed && pendants.All(p => p.Source is null),
            "recall clears ornament references and their visual layer");
        check(window.SwingScenery.Visibility == Visibility.Collapsed && scenery.All(p => p.Source is null),
            "recall also removes side scenery and clears artwork references");
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
        check(window.SwingDecorations.Visibility == Visibility.Collapsed && pendants.All(p => p.Source is null),
            "legacy top-only pack retains no previous character's ornaments");
        check(window.SwingScenery.Visibility == Visibility.Collapsed && scenery.All(p => p.Source is null),
            "legacy top-only pack retains no previous side scenery");
        var legacy = window.SelectedCharacter;
        window.SetCharacter(PetCharacter.Tianyi); window.Characters.Remove(legacy);
        var customDirectory = Path.Combine(window.Characters.Root, "short-swing-test");
        Directory.CreateDirectory(customDirectory);
        png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(window.Characters.Find("cat").Idle.Frames[0].Image));
        using (var file = File.Create(Path.Combine(customDirectory, "idle.png"))) png.Save(file);
        manifest = manifest with { Id = "short-swing-test", Name = "不同尺寸秋千测试", DisplayHeight = 100,
            TopSwing = new() { Ornament = new() { Image = "idle.png" }, Scenery = new() { Image = "idle.png" } } };
        File.WriteAllText(Path.Combine(customDirectory, "character.json"), JsonSerializer.Serialize(manifest, CharacterPackLoader.Json));
        window.Characters.Reload(); window.SetCharacterPackage(manifest.Id);
        Attach(); window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(800));
        check(window.SwingRopes.Visibility == Visibility.Visible && window.CharacterArt.Height == 100,
            "custom swing supports a shorter manifest display size");
        CheckRopes();
        check(Math.Abs(pendants[0].Width - 18 * 100.0 / 148) < 0.00001 && window.SwingDecorations.Visibility == Visibility.Visible,
            "custom ornament dimensions scale with the shorter artwork display");
        CheckOrnaments();
        check(Math.Abs(scenery[0].Width - 42 * 100.0 / 148) < 0.00001 && window.SwingScenery.Visibility == Visibility.Visible
            && ReferenceEquals(window.SelectedCharacter.SwingSceneryImage, window.SelectedCharacter.SwingOrnamentImage),
            "custom side scenery scales with shorter artwork and shared PNGs decode once");
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
        void CheckOrnaments()
        {
            var ropes = new[] { window.SwingRopeLeft, window.SwingRopeRight };
            for (var side = 0; side < 2; side++)
                for (var level = 0; level < 2; level++)
                {
                    var pendant = pendants[side * 2 + level];
                    var fraction = level == 0 ? 0.13 : 0.58;
                    var x = Canvas.GetLeft(pendant) + pendant.Width / 2;
                    var y = Canvas.GetTop(pendant);
                    var rope = ropes[side];
                    check(Math.Abs(x - rope.X1 - (rope.X2 - rope.X1) * fraction) < 0.00001
                        && Math.Abs(y - rope.Y1 - (rope.Y2 - rope.Y1) * fraction) < 0.00001,
                        "ornament contact stays on its moving rope " + side + level);
                }
        }
    }
}
