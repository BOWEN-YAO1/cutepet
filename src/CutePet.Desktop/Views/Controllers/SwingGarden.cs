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
        var blossom = Blossom();
        for (var i = 0; i < flowers.Length; i++)
        {
            flowers[i] = new Image { Source = blossom, IsHitTestVisible = false, Stretch = Stretch.Uniform,
                RenderTransformOrigin = new Point(0.5, 0.5), RenderTransform = new RotateTransform() };
            layer.Children.Add(flowers[i]);
        }
        var pearl = new RadialGradientBrush(Colors.Ivory, Color.FromRgb(195, 213, 183)); pearl.Freeze();
        for (var i = 0; i < pearls.Length; i++)
        {
            pearls[i] = new Ellipse { Fill = pearl, Stroke = gold, IsHitTestVisible = false }; layer.Children.Add(pearls[i]);
        }
    }

    internal void Hide() => layer.Visibility = Visibility.Collapsed;

    internal void RenderSide(int side, Point start, Point a, Point b, Point end, Line rope, double scale, double phase)
    {
        layer.Visibility = Visibility.Visible;
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
    }

    private static Point Curve(Point p, Point a, Point b, Point end, double t)
    {
        var s = 1 - t;
        return new Point(s * s * s * p.X + 3 * s * s * t * a.X + 3 * s * t * t * b.X + t * t * t * end.X,
            s * s * s * p.Y + 3 * s * s * t * a.Y + 3 * s * t * t * b.Y + t * t * t * end.Y);
    }

    private static DrawingImage Blossom()
    {
        var drawing = new DrawingGroup();
        var ivory = new LinearGradientBrush(Color.FromRgb(255, 253, 239), Color.FromRgb(212, 225, 201), 90);
        var petal = Geometry.Parse("M0,0 C-6,-3 -6,-11 0,-15 C6,-11 6,-3 0,0 Z");
        for (var i = 0; i < 6; i++)
        {
            var group = new DrawingGroup { Transform = new RotateTransform(i * 60) };
            group.Children.Add(new GeometryDrawing(ivory, new Pen(new SolidColorBrush(Color.FromRgb(206, 201, 167)), 0.45), petal));
            drawing.Children.Add(group);
        }
        drawing.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromRgb(219, 184, 98)), null, new EllipseGeometry(new Point(), 2.3, 2.3)));
        var image = new DrawingImage(drawing); image.Freeze(); return image;
    }
}
