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
            && ((TransformGroup)scenery[1].RenderTransform).Children[0] is ScaleTransform { ScaleX: -1 }, "side scenery reuses one cached image with right-side mirroring");
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
        var sceneryTilt = ((RotateTransform)((TransformGroup)scenery[0].RenderTransform).Children[1]).Angle;
        check(SceneryInside(), "tilted lotus clusters including their rotated corners remain inside the pet canvas");
        var star = window.SwingScenery.Children.OfType<System.Windows.Shapes.Path>().Last();
        var starOpacity = star.Opacity;
        var garden = window.SwingScenery.Children.OfType<Canvas>().Single();
        var leaves = garden.Children.OfType<System.Windows.Shapes.Path>().Where(p => p.Fill is not null).ToArray();
        var flowers = garden.Children.OfType<Image>().Where(p => p.Name.StartsWith("GardenFlower", StringComparison.Ordinal)).ToArray();
        var ornaments = garden.Children.OfType<Image>().Except(flowers).ToArray();
        var trailLayer = garden.Children.OfType<Canvas>().Single();
        var trails = trailLayer.Children.OfType<Canvas>().ToArray();
        var trailFlowers = trails.SelectMany(p => p.Children.OfType<Image>()).ToArray();
        check(trailLayer.Visibility == Visibility.Visible && trailFlowers.Length == 16
            && trailFlowers.All(p => flowers.Any(f => ReferenceEquals(f.Source, p.Source)))
            && trails.SelectMany(p => p.Children.Cast<UIElement>()).All(p => !p.IsHitTestVisible),
            "independent flowering vines reuse cached artwork without intercepting gestures");
        var trailRoots = trails.Select(TrailRoot).ToArray();
        check(trailRoots.All(p => Math.Abs(p.Y) < 1e-9) && TrailsInside(), "vine roots meet the desktop top edge while foliage stays within the scene");
        check(trails.All(canvas =>
        {
            var main = (PathGeometry)canvas.Children.OfType<System.Windows.Shapes.Path>().First().Data;
            var branches = canvas.Children.OfType<System.Windows.Shapes.Path>().Where(p => p.Name.StartsWith("GardenTrailBranch", StringComparison.Ordinal)).ToArray();
            return branches.Length == 3 && branches.All(p =>
            {
                var branch = (PathGeometry)p.Data;
                var join = branch.Figures[0].StartPoint;
                return main.StrokeContains(new Pen(Brushes.Black, 0.5), join) && branch.Figures[0].Segments.Count == 2;
            });
        }), "six curled side shoots connect to actual main-vine geometry");
        var trailBreeze = ((RotateTransform)((TransformGroup)trails[0].RenderTransform).Children[1]).Angle;
        check(garden.Visibility == Visibility.Visible && leaves.Length == 36 && flowers.Length == 6
            && garden.Children.Cast<UIElement>().All(p => !p.IsHitTestVisible),
            "garden adds floral vines without intercepting gestures");
        check(flowers.Select(p => p.Source).Distinct().Count() == 3 && flowers.All(p => p.Source is DrawingImage { IsFrozen: true }),
            "three native floral styles reuse frozen drawings without additional PNG decoding");
        check(ornaments.Length == 4 && ornaments.Select(p => p.Source).Distinct().Count() == 3
            && ornaments.All(p => p.Source is DrawingImage { IsFrozen: true } && !p.IsHitTestVisible),
            "moon, wind chime and butterflies add distinct cached motifs without a pointer surface");
        check(GardenInside(), "new charms and butterflies including their transformed corners fit the pet scene");
        var charm = ornaments.Single(p => p.Name == "GardenMoon");
        var butterfly = ornaments.Single(p => p.Name == "GardenButterfly0");
        var charmAngle = ((RotateTransform)charm.RenderTransform).Angle;
        var wingScale = ((ScaleTransform)((TransformGroup)butterfly.RenderTransform).Children[0]).ScaleX;
        check(scenery.All(p => p.Width < 42 && p.Opacity < 1), "garden lotus clusters remain smaller and softer than floating layout");
        var leafPoint = new Point(Canvas.GetLeft(leaves[0]), Canvas.GetTop(leaves[0]));
        var flowerAngle = ((RotateTransform)flowers[0].RenderTransform).Angle;
        // Include the WPF rotation origin when checking the outermost leaf tips.
        check(leaves.All(leaf =>
        {
            var bounds = leaf.RenderTransform.TransformBounds(new Rect(0, -leaf.Height / 2, leaf.Width, leaf.Height));
            bounds.Offset(Canvas.GetLeft(leaf), Canvas.GetTop(leaf) + leaf.Height / 2);
            return new Rect(0, 0, 230, 178).Contains(bounds);
        }), "garden foliage including rotated tips stays in the character canvas");
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
        check(Canvas.GetTop(scenery[0]) == sceneryTop && star.Opacity == starOpacity
            && ((RotateTransform)((TransformGroup)scenery[0].RenderTransform).Children[1]).Angle == sceneryTilt,
            "menu freezes side-cloud floating and star twinkle");
        check(new Point(Canvas.GetLeft(leaves[0]), Canvas.GetTop(leaves[0])) == leafPoint
            && ((RotateTransform)flowers[0].RenderTransform).Angle == flowerAngle, "menu freezes the connected garden as well");
        check(((RotateTransform)charm.RenderTransform).Angle == charmAngle
            && ((ScaleTransform)((TransformGroup)butterfly.RenderTransform).Children[0]).ScaleX == wingScale,
            "menu freezes hanging charm sway and butterfly flutter on the same clock");
        check(((RotateTransform)((TransformGroup)trails[0].RenderTransform).Children[1]).Angle == trailBreeze,
            "menu freezes independent flower trails on the existing clock");
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
        check(new Point(Canvas.GetLeft(leaves[0]), Canvas.GetTop(leaves[0])) != leafPoint, "garden resumes on the shared swing clock");
        // Half a swing cycle repeats the flutter phase; advance a quarter beat to verify resumption.
        window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(100));
        check(((RotateTransform)charm.RenderTransform).Angle != charmAngle
            && ((ScaleTransform)((TransformGroup)butterfly.RenderTransform).Children[0]).ScaleX != wingScale
            && GardenInside(), "new ornament motion resumes within the scene");
        check(((RotateTransform)((TransformGroup)trails[0].RenderTransform).Children[1]).Angle != trailBreeze && TrailsInside(),
            "independent vine breeze resumes without leaving the scene");
        check(trails.Select(TrailRoot).Zip(trailRoots).All(p => (p.First - p.Second).Length < 1e-9),
            "vine breeze moves foliage while both desktop roots stay fixed");
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
        check(garden.Visibility == Visibility.Collapsed && trailLayer.Visibility == Visibility.Collapsed,
            "recall clears native garden and independent vine decoration");
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
        check(window.SelectedCharacter.Manifest.TopSwing!.Scenery!.Layout == "floating" && garden.Visibility == Visibility.Collapsed,
            "old custom scenery keeps its floating layout without inherited flowers");
        check(trailLayer.Visibility == Visibility.Collapsed, "old floating layouts do not inherit free-standing vines");
        check(scenery.All(p => ((RotateTransform)((TransformGroup)p.RenderTransform).Children[1]).Angle == 0),
            "old floating layout does not inherit tilted lotus transforms");
        window.WakeCharacterImmediately();
        manifest = manifest with { TopSwing = manifest.TopSwing! with { Scenery = manifest.TopSwing.Scenery! with { Layout = "garden" } } };
        File.WriteAllText(Path.Combine(customDirectory, "character.json"), JsonSerializer.Serialize(manifest, CharacterPackLoader.Json));
        window.Characters.Reload(); window.SetCharacterPackage(manifest.Id);
        Attach(); window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(800));
        check(garden.Visibility == Visibility.Visible && Math.Abs(flowers[0].Width - 8 * 100.0 / 148) < 0.00001
            && window.QuotaHost.Position == quota, "opt-in garden scales with short custom artwork and keeps quota fixed");
        check(Math.Abs(charm.Height - 25 * 100.0 / 148) < 0.00001 && GardenInside(),
            "diverse garden ornaments scale with smaller custom characters");
        check(trailLayer.Visibility == Visibility.Visible && TrailsInside()
            && Math.Abs(((ScaleTransform)((TransformGroup)trails[0].RenderTransform).Children[0]).ScaleX - 100.0 / 148) < 0.00001,
            "independent trails scale with shorter artwork");
        check(trails.All(p => Math.Abs(TrailRoot(p).Y) < 1e-9), "shorter character scaling keeps vine roots on the desktop top edge");
        window.WakeCharacterImmediately();
        manifest = manifest with { TopSwing = manifest.TopSwing! with { Scenery = manifest.TopSwing.Scenery! with { DisplayWidth = 60, DisplayHeight = 60 } } };
        File.WriteAllText(Path.Combine(customDirectory, "character.json"), JsonSerializer.Serialize(manifest, CharacterPackLoader.Json));
        window.Characters.Reload(); window.SetCharacterPackage(manifest.Id);
        Attach(); window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(800));
        check(SceneryInside(), "maximum-sized tilted custom scenery stays within the shortened pet canvas");
        var custom = window.SelectedCharacter;
        window.SetCharacter(PetCharacter.Tianyi); window.Characters.Remove(custom);
        Attach(); window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(320));
        var rootedCycle = true;
        var cycleRoots = trails.Select(TrailRoot).ToArray();
        for (var i = 0; i < 32; i++)
        {
            window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(100));
            rootedCycle &= TrailsInside() && trails.Select(TrailRoot).Zip(cycleRoots).All(p => (p.First - p.Second).Length < 1e-9);
        }
        check(rootedCycle, "complete vine breeze cycle keeps roots fixed and branched foliage within the scene");
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

        bool SceneryInside() => scenery.All(art =>
        {
            var bounds = art.RenderTransform.TransformBounds(new Rect(-art.Width / 2, -art.Height / 2, art.Width, art.Height));
            bounds.Offset(Canvas.GetLeft(art) + art.Width / 2, Canvas.GetTop(art) + art.Height / 2);
            return new Rect(0, 0, 230, 178).Contains(bounds);
        });
        bool GardenInside() => ornaments.All(art =>
        {
            var origin = new Point(art.Width * art.RenderTransformOrigin.X, art.Height * art.RenderTransformOrigin.Y);
            var bounds = art.RenderTransform.TransformBounds(new Rect(-origin.X, -origin.Y, art.Width, art.Height));
            bounds.Offset(Canvas.GetLeft(art) + origin.X, Canvas.GetTop(art) + origin.Y);
            return new Rect(0, 0, 230, 178).Contains(bounds);
        });
        Point TrailRoot(Canvas canvas) => canvas.RenderTransform.Transform(
            ((PathGeometry)canvas.Children.OfType<System.Windows.Shapes.Path>().First().Data).Figures[0].StartPoint);
        bool TrailsInside() => trails.All(canvas => canvas.Children.Cast<FrameworkElement>().All(art =>
        {
            Rect bounds;
            if (art is System.Windows.Shapes.Path { Stroke: not null } stem)
            {
                var geometry = new GeometryGroup { Transform = canvas.RenderTransform };
                geometry.Children.Add(stem.Data);
                // The root's half stroke is intentionally clipped by the desktop edge.
                return new Rect(0, -0.5, 230, 178.5).Contains(geometry.GetRenderBounds(new Pen(stem.Stroke, stem.StrokeThickness)));
            }
            else
            {
                var origin = new Point(art.Width * art.RenderTransformOrigin.X, art.Height * art.RenderTransformOrigin.Y);
                bounds = art.RenderTransform.TransformBounds(new Rect(-origin.X, -origin.Y, art.Width, art.Height));
                bounds.Offset(Canvas.GetLeft(art) + origin.X, Canvas.GetTop(art) + origin.Y);
            }
            return new Rect(0, 0, 230, 178).Contains(canvas.RenderTransform.TransformBounds(bounds));
        }));

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
