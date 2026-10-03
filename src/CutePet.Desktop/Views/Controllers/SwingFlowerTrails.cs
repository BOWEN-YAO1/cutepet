using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CutePet.Desktop;

// Independent floral sprays occupy the outer scene; they have no rope contact point.
internal sealed class SwingFlowerTrails
{
    private readonly Canvas layer = new() { Name = "GardenIndependentVines", IsHitTestVisible = false, Visibility = Visibility.Collapsed };
    private readonly Canvas[] sides = new Canvas[2];
    private readonly ScaleTransform[] scales = { new(1, 1, 115, 0), new(1, 1, 115, 0) };
    private readonly TranslateTransform[] breezes = { new(), new() };

    internal SwingFlowerTrails(Canvas parent, Geometry leaf, Brush jade, DrawingImage[] blossoms)
    {
        parent.Children.Insert(0, layer);
        var stemBrush = new SolidColorBrush(Color.FromRgb(149, 184, 155)); stemBrush.Freeze();
        for (var side = 0; side < 2; side++)
        {
            var sign = side == 0 ? -1 : 1;
            Point At(double offset, double y) => new(115 + sign * offset, y);
            var figure = new PathFigure { StartPoint = At(92, 24 + side * 12), IsFilled = false };
            figure.Segments.Add(new BezierSegment(At(108, 44), At(105, 78), At(94, 92), true));
            figure.Segments.Add(new BezierSegment(At(84, 116), At(85, 141), At(58, 136), true));
            var geometry = new PathGeometry(new[] { figure }); geometry.Freeze();
            var transform = new TransformGroup(); transform.Children.Add(scales[side]); transform.Children.Add(breezes[side]);
            var canvas = sides[side] = new Canvas { Name = "GardenTrailSide" + side, IsHitTestVisible = false, RenderTransform = transform };
            layer.Children.Add(canvas);
            canvas.Children.Add(new Path { Name = "GardenTrailStem" + side, Data = geometry, Stroke = stemBrush,
                StrokeThickness = 0.9, Opacity = 0.75, IsHitTestVisible = false,
                StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round });
            for (var level = 0; level < 10; level++)
            {
                geometry.GetPointAtFractionLength(0.04 + level * 0.1, out var p, out var tangent);
                var heading = Math.Atan2(tangent.Y, tangent.X) * 180 / Math.PI;
                for (var branch = 0; branch < 2; branch++)
                {
                    var rotation = new RotateTransform(heading + (branch == 0 ? -64 : 64)); rotation.Freeze();
                    var art = new Path { Data = leaf, Fill = jade, Width = 5.5 + level % 3, Height = 3.2,
                        Stretch = Stretch.Fill, Opacity = 0.75, IsHitTestVisible = false,
                        RenderTransformOrigin = new Point(0, 0.5), RenderTransform = rotation };
                    Canvas.SetLeft(art, p.X); Canvas.SetTop(art, p.Y - art.Height / 2);
                    canvas.Children.Add(art);
                }
            }
            for (var level = 0; level < 5; level++)
            {
                geometry.GetPointAtFractionLength(0.02 + level * 0.23, out var p, out _);
                var rotation = new RotateTransform(sign * (12 + level * 9)); rotation.Freeze();
                var size = level == 2 ? 12 : (level == 4 ? 11 : 8);
                var flower = new Image { Name = "GardenTrailFlower" + side + level, Source = blossoms[(level + side) % blossoms.Length],
                    Width = size, Height = size, Stretch = Stretch.Uniform, IsHitTestVisible = false,
                    Opacity = 0.9, RenderTransformOrigin = new Point(0.5, 0.5), RenderTransform = rotation };
                Canvas.SetLeft(flower, p.X - size / 2); Canvas.SetTop(flower, p.Y - size / 2);
                canvas.Children.Add(flower);
            }
        }
    }

    internal void Hide() => layer.Visibility = Visibility.Collapsed;
    internal void RenderSide(int side, double scale, double phase)
    {
        scales[side].ScaleX = scales[side].ScaleY = scale;
        breezes[side].X = 1.4 * scale * Math.Sin(phase + side * 1.1);
        breezes[side].Y = 0.6 * scale * Math.Sin(phase + side * 1.1 + 0.4);
        layer.Visibility = Visibility.Visible;
    }
}
