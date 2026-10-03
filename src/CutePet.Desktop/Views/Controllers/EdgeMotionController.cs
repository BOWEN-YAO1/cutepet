using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CutePet.Desktop;

// Keep the whole HWND inside its monitor. Clip only the character artwork at the edge.
internal sealed class EdgeMotionController
{
    private readonly MainWindow window;
    private readonly bool verification;
    private double peekElapsed = -1;
    private double entering;
    private double swingElapsed;
    private string? ropeColor;
    private SolidColorBrush? ropeBrush;
    private readonly Image[] pendants = new Image[4];
    private readonly Ellipse[] beads = new Ellipse[4];
    private readonly Image[] sceneryImages = new Image[2];
    private readonly RotateTransform[] sceneryTilts = { new(), new() };
    private readonly Path[] ribbons = new Path[2];
    private readonly Path[] stars = new Path[4];
    private readonly SwingGarden garden;
    private readonly SolidColorBrush vineBrush = new(Color.FromRgb(177, 171, 126));
    private readonly SolidColorBrush ribbonBrush = new(Color.FromRgb(143, 199, 184));
    internal EdgeAttachment? Attachment { get; private set; }
    internal bool Active => Attachment is not null;
    private string BaseAction => EdgeActions.Base(Attachment!.Side);
    private string PeekAction => EdgeActions.Peek(BaseAction);
    internal double PeekOffset { get; private set; }
    internal EdgeMotionController(MainWindow window, bool verification)
    {
        this.window = window; this.verification = verification;
        for (var i = 0; i < pendants.Length; i++)
        {
            pendants[i] = new Image { IsHitTestVisible = false, Stretch = Stretch.Uniform,
                RenderTransformOrigin = new Point(0.5, 0), RenderTransform = new RotateTransform() };
            RenderOptions.SetBitmapScalingMode(pendants[i], BitmapScalingMode.HighQuality);
            beads[i] = new Ellipse { IsHitTestVisible = false, Fill = new SolidColorBrush(Color.FromRgb(191, 229, 215)),
                Stroke = new SolidColorBrush(Color.FromRgb(187, 154, 92)), StrokeThickness = 0.55 };
            window.SwingDecorations.Children.Add(pendants[i]);
            window.SwingDecorations.Children.Add(beads[i]);
        }
        for (var i = 0; i < sceneryImages.Length; i++)
        {
            ribbons[i] = new Path { IsHitTestVisible = false, Stroke = new SolidColorBrush(Color.FromRgb(143, 199, 184)),
                StrokeThickness = 1.7, Opacity = 0.65, StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };
            var transform = new TransformGroup();
            transform.Children.Add(new ScaleTransform(i == 0 ? 1 : -1, 1));
            transform.Children.Add(sceneryTilts[i]);
            sceneryImages[i] = new Image { IsHitTestVisible = false, Stretch = Stretch.Uniform, RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = transform };
            RenderOptions.SetBitmapScalingMode(sceneryImages[i], BitmapScalingMode.HighQuality);
            window.SwingScenery.Children.Add(ribbons[i]);
            window.SwingScenery.Children.Add(sceneryImages[i]);
        }
        garden = new SwingGarden(window.SwingScenery);
        vineBrush.Freeze(); ribbonBrush.Freeze();
        var starGeometry = Geometry.Parse("M3,0 L4,2 L6,3 L4,4 L3,6 L2,4 L0,3 L2,2 Z");
        starGeometry.Freeze();
        for (var i = 0; i < stars.Length; i++)
        {
            stars[i] = new Path { IsHitTestVisible = false, Data = starGeometry, Stretch = Stretch.Fill,
                Fill = new SolidColorBrush(Color.FromRgb(218, 184, 110)) };
            window.SwingScenery.Children.Add(stars[i]);
        }
    }

    internal bool Attach(Rect area, Size size, Point released, double dpi)
    {
        if (!window.CanDrag || !window.Settings.EdgeInteraction || !EdgeActions.Available(window.SelectedCharacter)
            || window.Model.IsLow && !window.Model.IsStale) return false;
        var plan = ScreenEdgeLayout.Plan(area, size, released, dpi, EdgeActions.Supported(window.SelectedCharacter));
        if (plan is null) return false;
        window.WakeCharacterImmediately();
        Attachment = plan;
        entering = 0;
        swingElapsed = 0;
        window.StartEdgePose(BaseAction);
        Render();
        if (!verification) NativePlacement.Apply(window, plan.Position.X, plan.Position.Y);
        window.SavePlacement();
        return true;
    }
    internal void Peek()
    {
        if (!Active || peekElapsed >= 0 || !window.SelectedCharacter.Actions.ContainsKey(PeekAction)) return;
        peekElapsed = 0;
        window.StartEdgePeek();
        Render();
    }
    internal void Advance(TimeSpan elapsed)
    {
        if (!Active) return;
        if (!verification)
        {
            var bounds = NativePlacement.EdgeBounds(window);
            if (bounds.Area != Attachment!.Area || bounds.Size != Attachment.PetSize)
            { Reanchor(bounds.Area, bounds.Size); if (!Active) return; }
        }
        if (!window.CanPlayAmbient) return;
        var delta = Math.Max(0, elapsed.TotalMilliseconds);
        entering = Math.Min(320, entering + delta);
        swingElapsed = (swingElapsed + delta) % 3200;
        if (peekElapsed < 0) { Render(); return; }
        var duration = window.SelectedCharacter.Actions[PeekAction].Duration;
        peekElapsed += Math.Max(0, elapsed.TotalMilliseconds);
        if (peekElapsed >= duration) peekElapsed = -1;
        Render();
    }
    internal void Cancel()
    {
        Attachment = null;
        peekElapsed = -1;
        PeekOffset = 0;
        swingElapsed = 0;
        window.EdgeSwing.Angle = 0;
        window.EdgeSwing.CenterY = 0;
        window.SwingRopes.Visibility = Visibility.Collapsed;
        HideDecorations();
        HideScenery();
        window.EdgeMirror.ScaleX = 1;
        window.EdgeStretch.ScaleY = 1;
        window.EdgeStretch.CenterY = 0;
        window.EdgeShift.X = 0;
        window.EdgeShift.Y = 0;
        window.Scene.ClipToBounds = false;
        window.GroundShadow.Visibility = Visibility.Visible;
    }
    internal void Reanchor(Rect area, Size size)
    {
        if (Attachment is null) return;
        var plan = ScreenEdgeLayout.Anchor(Attachment.Side, area, size, Attachment.Position);
        if (plan is null)
        {
            window.WakeCharacterImmediately();
            if (!verification) { var current = NativePlacement.Get(window); NativePlacement.Apply(window, current.Left, current.Top); }
            window.SavePlacement(); return;
        }
        Attachment = plan;
        Render();
        if (!verification) NativePlacement.Apply(window, plan.Position.X, plan.Position.Y);
        window.SavePlacement();
    }
    private void Render()
    {
        if (Attachment is null) return;
        var right = Attachment.Side == ScreenEdge.Right;
        var width = window.CharacterArt.Width;
        var image = window.SelectedCharacter.Actions[BaseAction].Frames[0].Image;
        var renderedWidth = Math.Min(width, window.CharacterArt.Height * image.PixelWidth / image.PixelHeight);
        var renderedHeight = Math.Min(window.CharacterArt.Height, width * image.PixelHeight / image.PixelWidth);
        // The package artwork's left canvas edge is the virtual border. Hands may overlap it.
        var anchor = (230 - renderedWidth) / 2 + renderedWidth * window.SelectedCharacter.Manifest.EdgeAnchorX;
        var t = peekElapsed < 0 ? 0 : peekElapsed / window.SelectedCharacter.Actions[PeekAction].Duration;
        PeekOffset = 8 * Math.Pow(Math.Sin(Math.PI * t), 2);
        window.EdgeMirror.ScaleX = right ? -1 : 1;
        var entry = entering / 320;
        entry = entry * entry * (3 - 2 * entry);
        window.EdgeShift.X = Attachment.Side is ScreenEdge.Left or ScreenEdge.Right
            ? (right ? 1 : -1) * (anchor * entry - PeekOffset) : 0;
        var imageTop = 16 + 148 - window.CharacterArt.Height + (window.CharacterArt.Height - renderedHeight) / 2;
        var verticalAnchor = Attachment.Side == ScreenEdge.Top ? window.SelectedCharacter.Manifest.EdgeTopAnchorY
            : window.SelectedCharacter.Manifest.EdgeBottomAnchorY;
        // Top may swing independently; bottom keeps a small elbow-anchored lift.
        var vertical = Attachment.Side is ScreenEdge.Top or ScreenEdge.Bottom;
        window.EdgeStretch.CenterY = vertical ? renderedHeight * (verticalAnchor - 0.5) : 0;
        window.EdgeStretch.ScaleY = Attachment.Side == ScreenEdge.Bottom ? 1 + 0.04 * PeekOffset / 8 : 1;
        window.EdgeShift.Y = Attachment.Side switch
        {
            ScreenEdge.Top => -(imageTop + renderedHeight * window.SelectedCharacter.Manifest.EdgeTopAnchorY) * entry,
            ScreenEdge.Bottom => (178 - imageTop - renderedHeight * window.SelectedCharacter.Manifest.EdgeBottomAnchorY) * entry,
            _ => 0
        };
        RenderSwing(imageTop, renderedWidth, renderedHeight, entry);
        window.Scene.ClipToBounds = true;
        window.GroundShadow.Visibility = Visibility.Hidden;
    }
    private void RenderSwing(double imageTop, double width, double height, double entry)
    {
        if (Attachment!.Side != ScreenEdge.Top || window.SelectedCharacter.Manifest.TopSwing is not { } swing)
        {
            window.EdgeSwing.Angle = 0;
            window.SwingRopes.Visibility = Visibility.Collapsed;
            HideDecorations();
            HideScenery();
            return;
        }
        var anchor = window.SelectedCharacter.Manifest.EdgeTopAnchorY;
        var angle = (3 + PeekOffset / 8) * Math.Sin(2 * Math.PI * swingElapsed / 3200) * entry;
        window.EdgeSwing.CenterY = height * (anchor - 0.5);
        window.EdgeSwing.Angle = angle;
        var radians = angle * Math.PI / 180;
        var span = width * swing.SeatHalfWidth;
        var drop = height * (swing.SeatAnchorY - anchor);
        var pivotY = imageTop + height * anchor + window.EdgeShift.Y;
        if (ropeColor != swing.RopeColor)
        {
            ropeColor = swing.RopeColor;
            ropeBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(ropeColor));
            ropeBrush.Freeze();
        }
        foreach (var (rope, x) in new[] { (window.SwingRopeLeft, -span), (window.SwingRopeRight, span) })
        {
            rope.X1 = 115 + x;
            rope.Y1 = 0;
            rope.X2 = 115 + x * Math.Cos(radians) - drop * Math.Sin(radians);
            rope.Y2 = pivotY + x * Math.Sin(radians) + drop * Math.Cos(radians);
            rope.Stroke = ropeBrush;
        }
        window.SwingRopes.Opacity = entry;
        window.SwingRopes.Visibility = Visibility.Visible;
        RenderDecorations(swing, height, entry);
        RenderScenery(swing, height, pivotY, entry);
    }
    private void HideDecorations()
    {
        window.SwingDecorations.Visibility = Visibility.Collapsed;
        foreach (var pendant in pendants) pendant.Source = null;
    }
    private void RenderDecorations(CharacterTopSwing swing, double height, double entry)
    {
        if (swing.Ornament is not { } ornament || window.SelectedCharacter.SwingOrnamentImage is not { } image)
        { HideDecorations(); return; }
        var scale = height / 148;
        var sway = -4 * Math.Sin(2 * Math.PI * (swingElapsed - 160) / 3200) * entry;
        var ropes = new[] { window.SwingRopeLeft, window.SwingRopeRight };
        for (var side = 0; side < ropes.Length; side++)
        {
            var rope = ropes[side];
            for (var level = 0; level < 2; level++)
            {
                var index = side * 2 + level;
                var pendant = pendants[index];
                var fraction = level == 0 ? 0.13 : 0.58;
                pendant.Source = image;
                pendant.Width = ornament.DisplayWidth * scale;
                pendant.Height = ornament.DisplayHeight * scale;
                Canvas.SetLeft(pendant, rope.X1 + (rope.X2 - rope.X1) * fraction - pendant.Width / 2);
                Canvas.SetTop(pendant, rope.Y1 + (rope.Y2 - rope.Y1) * fraction);
                ((RotateTransform)pendant.RenderTransform).Angle = sway;
                var bead = beads[index];
                fraction = level == 0 ? 0.43 : 0.88;
                bead.Width = bead.Height = 3.2 * scale;
                Canvas.SetLeft(bead, rope.X1 + (rope.X2 - rope.X1) * fraction - bead.Width / 2);
                Canvas.SetTop(bead, rope.Y1 + (rope.Y2 - rope.Y1) * fraction - bead.Height / 2);
            }
        }
        window.SwingDecorations.Visibility = Visibility.Visible;
    }
    private void HideScenery()
    {
        window.SwingScenery.Visibility = Visibility.Collapsed;
        garden.Hide();
        foreach (var tilt in sceneryTilts) tilt.Angle = 0;
        foreach (var scenery in sceneryImages) scenery.Source = null;
    }
    private void RenderScenery(CharacterTopSwing swing, double height, double pivotY, double entry)
    {
        if (swing.Scenery is not { } scenery || window.SelectedCharacter.SwingSceneryImage is not { } image)
        { HideScenery(); return; }
        var scale = height / 148;
        var phase = 2 * Math.PI * swingElapsed / 3200;
        var natural = scenery.Layout == "garden";
        if (!natural) garden.Hide();
        for (var side = 0; side < sceneryImages.Length; side++)
        {
            var sign = side == 0 ? -1 : 1;
            var x = 115 + sign * (natural ? 77 : 74) * scale;
            var y = pivotY + height * (natural ? (side == 0 ? 0.74 : 0.66) : (side == 0 ? 0.62 : 0.48))
                + (natural ? 0.7 : 1.8) * scale * Math.Sin(phase + side * Math.PI);
            var art = sceneryImages[side];
            art.Source = image;
            var size = natural ? (side == 0 ? 0.73 : 0.63) : 1;
            art.Width = scenery.DisplayWidth * scale * size;
            art.Height = scenery.DisplayHeight * scale * size;
            sceneryTilts[side].Angle = natural ? (side == 0 ? -40 : 35) + 0.9 * Math.Sin(phase + side) : 0;
            art.Opacity = natural ? 0.9 : 1;
            Canvas.SetLeft(art, x - art.Width / 2);
            Canvas.SetTop(art, y - art.Height / 2);
            var rope = side == 0 ? window.SwingRopeLeft : window.SwingRopeRight;
            var figure = new PathFigure { IsFilled = false, StartPoint = new Point(
                rope.X1 + (rope.X2 - rope.X1) * 0.07, rope.Y1 + (rope.Y2 - rope.Y1) * 0.07) };
            var control1 = new Point(115 + sign * (natural ? 58 : 104) * scale, pivotY + 30 * scale);
            var control2 = new Point(115 + sign * (natural ? 90 : 35) * scale, y - 24 * scale);
            figure.Segments.Add(new BezierSegment(control1, control2, new Point(x, y), true));
            var geometry = new PathGeometry(new[] { figure });
            geometry.Freeze();
            ribbons[side].Data = geometry;
            ribbons[side].Stroke = natural ? vineBrush : ribbonBrush;
            ribbons[side].StrokeThickness = (natural ? 0.75 : 1.7) * scale;
            ribbons[side].Opacity = natural ? 0.7 : 0.65;
            if (natural) garden.RenderSide(side, figure.StartPoint, control1, control2, new Point(x, y), rope, scale, phase);
            for (var level = 0; level < 2; level++)
            {
                var star = stars[side * 2 + level];
                star.Width = star.Height = (level == 0 ? 4.5 : 3.5) * scale;
                star.Opacity = 0.25 + 0.6 * Math.Pow(Math.Sin(phase + side + level), 2);
                Canvas.SetLeft(star, 115 + sign * (level == 0 ? 90 : 64) * scale - star.Width / 2);
                Canvas.SetTop(star, pivotY + height * (level == 0 ? 0.29 : 0.85) - star.Height / 2);
            }
        }
        window.SwingScenery.Opacity = entry;
        window.SwingScenery.Visibility = Visibility.Visible;
    }
}
