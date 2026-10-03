using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CutePet.Desktop;

// Native floral details share the existing scenery's curve and animation clock.
// No extra bitmap decoding, timer, window, or pointer surface is required.
internal sealed class SwingGarden
{
    private readonly Canvas layer = new() { IsHitTestVisible = false, Visibility = Visibility.Collapsed };
    private readonly Path[] leaves = new Path[36];
    private readonly Image[] flowers = new Image[6];
    private readonly Ellipse[] pearls = new Ellipse[12];
    private readonly Path[] ropeVines = new Path[2];
    private readonly Ellipse[] haze = new Ellipse[6];
    private readonly Image[] charms = new Image[2];
    private readonly Image[] butterflies = new Image[2];
    private readonly SwingFlowerTrails flowerTrails;

    internal SwingGarden(Canvas parent)
    {
        parent.Children.Add(layer);
        var leaf = Geometry.Parse("M0,4 C3,-1 9,-1 12,0 C10,6 4,8 0,4 Z"); leaf.Freeze();
        var jade = new LinearGradientBrush(Color.FromRgb(209, 228, 205), Color.FromRgb(110, 163, 145), 90); jade.Freeze();
        var mist = new RadialGradientBrush(Color.FromArgb(85, 216, 230, 220), Colors.Transparent); mist.Freeze();
        var gold = new SolidColorBrush(Color.FromRgb(196, 183, 133)); gold.Freeze();
        for (var i = 0; i < haze.Length; i++)
        {
            haze[i] = new Ellipse { Fill = mist, IsHitTestVisible = false }; layer.Children.Add(haze[i]);
        }
        for (var i = 0; i < ropeVines.Length; i++)
        {
            ropeVines[i] = new Path { Stroke = gold, Opacity = 0.65, IsHitTestVisible = false,
                StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round };
            layer.Children.Add(ropeVines[i]);
        }
        for (var i = 0; i < leaves.Length; i++)
        {
            leaves[i] = new Path { Data = leaf, Fill = jade, Stretch = Stretch.Fill, IsHitTestVisible = false,
                RenderTransformOrigin = new Point(0, 0.5), RenderTransform = new RotateTransform(), Opacity = 0.8 };
            layer.Children.Add(leaves[i]);
        }
        var blossoms = new[] { Blossom(6, Color.FromRgb(255, 253, 239)), Blossom(5, Color.FromRgb(248, 219, 228)),
            Blossom(8, Color.FromRgb(221, 235, 250)) };
        flowerTrails = new SwingFlowerTrails(layer, leaf, jade, blossoms);
        for (var i = 0; i < flowers.Length; i++)
        {
            flowers[i] = new Image { Name = "GardenFlower" + i, Source = blossoms[(i + i / 3) % 3], IsHitTestVisible = false, Stretch = Stretch.Uniform,
                RenderTransformOrigin = new Point(0.5, 0.5), RenderTransform = new RotateTransform() };
            layer.Children.Add(flowers[i]);
        }
        var pearl = new RadialGradientBrush(Colors.Ivory, Color.FromRgb(195, 213, 183)); pearl.Freeze();
        for (var i = 0; i < pearls.Length; i++)
        {
            pearls[i] = new Ellipse { Fill = pearl, Stroke = gold, IsHitTestVisible = false }; layer.Children.Add(pearls[i]);
        }
        var butterfly = Butterfly();
        for (var i = 0; i < 2; i++)
        {
            charms[i] = new Image { Name = i == 0 ? "GardenMoon" : "GardenWindChime", Source = i == 0 ? Moon() : WindChime(),
                Stretch = Stretch.Fill, IsHitTestVisible = false, RenderTransformOrigin = new Point(0.5, 0),
                RenderTransform = new RotateTransform(), Opacity = 0.9 };
            layer.Children.Add(charms[i]);
            var transform = new TransformGroup();
            transform.Children.Add(new ScaleTransform()); transform.Children.Add(new RotateTransform(i == 0 ? -18 : 22));
            butterflies[i] = new Image { Name = "GardenButterfly" + i, Source = butterfly, Stretch = Stretch.Uniform,
                IsHitTestVisible = false, RenderTransformOrigin = new Point(0.5, 0.5), RenderTransform = transform, Opacity = 0.85 };
            layer.Children.Add(butterflies[i]);
        }
    }

    internal void Hide() { layer.Visibility = Visibility.Collapsed; flowerTrails.Hide(); }

    internal void RenderSide(int side, Point start, Point a, Point b, Point end, Line rope, double scale, double phase)
    {
        layer.Visibility = Visibility.Visible;
        flowerTrails.RenderSide(side, scale, phase);
        var sign = side == 0 ? -1 : 1;
        // Small alternating leaves grow from the same curved stem that reaches the lotus.
        for (var level = 0; level < 9; level++)
        {
            var t = 0.1 + level * 0.095;
            var p = Curve(start, a, b, end, t);
            var next = Curve(start, a, b, end, t + 0.01);
            var heading = Math.Atan2(next.Y - p.Y, next.X - p.X) * 180 / Math.PI;
            for (var branch = 0; branch < 2; branch++)
            {
                var leaf = leaves[side * 18 + level * 2 + branch];
                leaf.Width = (5.2 + (level % 3) * 0.65) * scale;
                leaf.Height = leaf.Width * 0.5;
                Canvas.SetLeft(leaf, p.X); Canvas.SetTop(leaf, p.Y - leaf.Height / 2);
                ((RotateTransform)leaf.RenderTransform).Angle = heading + (branch == 0 ? -62 : 62);
            }
        }
        for (var level = 0; level < 3; level++)
        {
            var p = Curve(start, a, b, end, 0.2 + level * 0.29);
            var flower = flowers[side * 3 + level];
            flower.Width = flower.Height = (level == 1 ? 11 : 8) * scale;
            Canvas.SetLeft(flower, p.X - flower.Width / 2); Canvas.SetTop(flower, p.Y - flower.Height / 2);
            ((RotateTransform)flower.RenderTransform).Angle = sign * 12 + 1.5 * Math.Sin(phase + side);
            var cloud = haze[side * 3 + level];
            cloud.Width = (22 + level * 7) * scale; cloud.Height = (7 + level) * scale;
            Canvas.SetLeft(cloud, end.X - cloud.Width / 2 + sign * (level - 1) * 6 * scale);
            Canvas.SetTop(cloud, end.Y + (8 + level * 3) * scale);
        }
        var figure = new PathFigure { IsFilled = false, StartPoint = new Point(rope.X1, rope.Y1) };
        // A fine winding thread binds the foliage visually back to the suspension.
        for (var level = 1; level <= 12; level++)
        {
            var t = level / 12.0;
            figure.Segments.Add(new LineSegment(new Point(rope.X1 + (rope.X2 - rope.X1) * t + Math.Sin(t * 6 * Math.PI) * 2 * scale,
                rope.Y1 + (rope.Y2 - rope.Y1) * t), true));
        }
        var geometry = new PathGeometry(new[] { figure }); geometry.Freeze();
        ropeVines[side].Data = geometry; ropeVines[side].StrokeThickness = 0.5 * scale;
        for (var level = 0; level < 6; level++)
        {
            var p = Curve(start, a, b, end, 0.14 + level * 0.14);
            var bead = pearls[side * 6 + level];
            bead.Width = bead.Height = (level % 2 == 0 ? 1.8 : 1.3) * scale;
            bead.StrokeThickness = 0.25 * scale;
            Canvas.SetLeft(bead, p.X - bead.Width / 2); Canvas.SetTop(bead, p.Y - bead.Height / 2);
        }
        var contact = Curve(start, a, b, end, side == 0 ? 0.34 : 0.38);
        var charm = charms[side];
        charm.Width = (side == 0 ? 12 : 13) * scale;
        charm.Height = (side == 0 ? 25 : 27) * scale;
        Canvas.SetLeft(charm, contact.X - charm.Width / 2); Canvas.SetTop(charm, contact.Y);
        ((RotateTransform)charm.RenderTransform).Angle = 2.5 * Math.Sin(phase - 0.3 + side);
        var flutter = butterflies[side];
        var perch = Curve(start, a, b, end, side == 0 ? 0.67 : 0.82);
        flutter.Width = 12 * scale; flutter.Height = 10 * scale;
        Canvas.SetLeft(flutter, perch.X + sign * (12 + 0.8 * Math.Sin(phase + side)) * scale - flutter.Width / 2);
        Canvas.SetTop(flutter, perch.Y - (7 + 0.7 * Math.Sin(phase + side)) * scale - flutter.Height / 2);
        ((ScaleTransform)((TransformGroup)flutter.RenderTransform).Children[0]).ScaleX = 0.7 + 0.3 * Math.Pow(Math.Sin(4 * phase + side), 2);
    }

    private static Point Curve(Point p, Point a, Point b, Point end, double t)
    {
        var s = 1 - t;
        return new Point(s * s * s * p.X + 3 * s * s * t * a.X + 3 * s * t * t * b.X + t * t * t * end.X,
            s * s * s * p.Y + 3 * s * s * t * a.Y + 3 * s * t * t * b.Y + t * t * t * end.Y);
    }

    private static DrawingImage Blossom(int petals, Color tint)
    {
        var drawing = new DrawingGroup();
        var ivory = new LinearGradientBrush(tint, Color.FromRgb(212, 225, 201), 90);
        var petal = Geometry.Parse("M0,0 C-6,-3 -6,-11 0,-15 C6,-11 6,-3 0,0 Z");
        for (var i = 0; i < petals; i++)
        {
            var group = new DrawingGroup { Transform = new RotateTransform(i * 360.0 / petals) };
            group.Children.Add(new GeometryDrawing(ivory, new Pen(new SolidColorBrush(Color.FromRgb(206, 201, 167)), 0.45), petal));
            drawing.Children.Add(group);
        }
        drawing.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromRgb(219, 184, 98)), null, new EllipseGeometry(new Point(), 2.3, 2.3)));
        var image = new DrawingImage(drawing); image.Freeze(); return image;
    }

    private static DrawingImage Moon()
    {
        var drawing = new DrawingGroup();
        drawing.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(0, 0, 12, 28))));
        var gold = new Pen(new SolidColorBrush(Color.FromRgb(191, 170, 111)), 0.55);
        drawing.Children.Add(new GeometryDrawing(null, gold, Geometry.Parse("M6,0 L6,4")));
        drawing.Children.Add(new GeometryDrawing(new LinearGradientBrush(Color.FromRgb(237, 245, 212), Color.FromRgb(143, 194, 166), 50), gold,
            Geometry.Parse("M8,4 C-1,3 -2,17 8,18 C3,14 3,8 8,4 Z")));
        drawing.Children.Add(new GeometryDrawing(null, gold, Geometry.Parse("M5,18 L5,22 M8,18 L8,24")));
        drawing.Children.Add(new GeometryDrawing(Brushes.Ivory, gold, new EllipseGeometry(new Point(5, 22), 1, 1)));
        drawing.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromRgb(204, 185, 121)), null,
            Geometry.Parse("M8,21 L9,23 L11,24 L9,25 L8,27 L7,25 L5,24 L7,23 Z")));
        var image = new DrawingImage(drawing); image.Freeze(); return image;
    }

    private static DrawingImage WindChime()
    {
        var drawing = new DrawingGroup();
        drawing.Children.Add(new GeometryDrawing(Brushes.Transparent, null, new RectangleGeometry(new Rect(-1, 0, 16, 31))));
        var gold = new Pen(new SolidColorBrush(Color.FromRgb(184, 162, 106)), 0.5);
        drawing.Children.Add(new GeometryDrawing(null, gold, Geometry.Parse("M7,0 L7,4 M2,12 L2,22 M7,12 L7,27 M12,12 L12,23")));
        drawing.Children.Add(new GeometryDrawing(new LinearGradientBrush(Color.FromRgb(233, 248, 240), Color.FromRgb(117, 184, 170), 90), gold,
            Geometry.Parse("M1,11 C1,1 13,1 13,11 Q7,14 1,11 Z")));
        drawing.Children.Add(new GeometryDrawing(null, gold, Geometry.Parse("M1,11 Q7,9 13,11")));
        foreach (var p in new[] { new Point(2, 22), new Point(7, 27), new Point(12, 23) })
            drawing.Children.Add(new GeometryDrawing(new LinearGradientBrush(Colors.Ivory, Color.FromRgb(180, 209, 225), 90), gold,
                Geometry.Parse(FormattableString.Invariant($"M{p.X},{p.Y - 2} Q{p.X - 3},{p.Y + 1} {p.X},{p.Y + 3} Q{p.X + 3},{p.Y + 1} {p.X},{p.Y - 2} Z"))));
        var image = new DrawingImage(drawing); image.Freeze(); return image;
    }

    private static DrawingImage Butterfly()
    {
        var drawing = new DrawingGroup();
        var outline = new Pen(new SolidColorBrush(Color.FromRgb(182, 167, 131)), 0.35);
        var upper = new LinearGradientBrush(Color.FromRgb(226, 237, 254), Color.FromRgb(158, 195, 213), 90);
        var lower = new LinearGradientBrush(Color.FromRgb(243, 227, 242), Color.FromRgb(183, 178, 213), 90);
        foreach (var sign in new[] { -1, 1 })
        {
            var wing = new DrawingGroup { Transform = new ScaleTransform(sign, 1) };
            wing.Children.Add(new GeometryDrawing(upper, outline, Geometry.Parse("M0,0 C3,-9 12,-7 8,-1 Q5,3 0,0 Z")));
            wing.Children.Add(new GeometryDrawing(lower, outline, Geometry.Parse("M0,1 C8,-1 8,7 3,6 Q1,5 0,1 Z")));
            drawing.Children.Add(wing);
        }
        drawing.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromRgb(156, 160, 143)), null, new EllipseGeometry(new Point(0, 0), 0.6, 4)));
        drawing.Children.Add(new GeometryDrawing(null, outline, Geometry.Parse("M0,-3 Q-2,-7 -3,-6 M0,-3 Q2,-7 3,-6")));
        var image = new DrawingImage(drawing); image.Freeze(); return image;
    }
}
