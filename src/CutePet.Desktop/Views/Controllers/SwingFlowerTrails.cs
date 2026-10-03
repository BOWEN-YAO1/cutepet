using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CutePet.Desktop;

// Floral vines grow from the desktop top edge, independently of the swing ropes.
internal sealed class SwingFlowerTrails
{
    private readonly Canvas layer = new() { Name = "GardenIndependentVines", IsHitTestVisible = false, Visibility = Visibility.Collapsed };
    private readonly Canvas[] sides = new Canvas[2];
    private readonly ScaleTransform[] scales = { new(1, 1, 115, 0), new(1, 1, 115, 0) };
    private readonly RotateTransform[] breezes = { new(), new() };

    internal SwingFlowerTrails(Canvas parent, Geometry leaf, Brush jade, DrawingImage[] blossoms)
    {
        parent.Children.Insert(0, layer);
        var stemBrush = new SolidColorBrush(Color.FromRgb(149, 184, 155)); stemBrush.Freeze();
        for (var side = 0; side < 2; side++)
        {
            var sign = side == 0 ? -1 : 1;
            Point At(double offset, double y) => new(115 + sign * offset, y);
            var figure = new PathFigure { StartPoint = At(92, 0), IsFilled = false };
            figure.Segments.Add(new BezierSegment(At(80 + side * 3, 15), At(111 - side * 2, 25), At(100, 48), true));
            figure.Segments.Add(new BezierSegment(At(108, 65), At(94, 79), At(94, 94), true));
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
                geometry.GetPointAtFractionLength(0.06 + level * 0.095, out var p, out var tangent);
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
                geometry.GetPointAtFractionLength(0.1 + level * 0.21, out var p, out _);
                var rotation = new RotateTransform(sign * (12 + level * 9)); rotation.Freeze();
                var size = level == 2 ? 14 : (level == 4 ? 12 : 10);
                var flower = new Image { Name = "GardenTrailFlower" + side + level, Source = blossoms[(level + side) % blossoms.Length],
                    Width = size, Height = size, Stretch = Stretch.Uniform, IsHitTestVisible = false,
                    Opacity = 0.9, RenderTransformOrigin = new Point(0.5, 0.5), RenderTransform = rotation };
                Canvas.SetLeft(flower, p.X - size / 2); Canvas.SetTop(flower, p.Y - size / 2);
                canvas.Children.Add(flower);
            }
            // Side shoots share a real point on the main stem, then curl in different directions.
            for (var branch = 0; branch < 3; branch++)
            {
                geometry.GetPointAtFractionLength(0.24 + branch * 0.24, out var join, out _);
                var tip = branch switch { 0 => At(72, join.Y + 14 + side * 3),
                    1 => At(102, join.Y + 18 - side * 2), _ => At(64, join.Y + 9) };
                var shoot = new PathFigure { StartPoint = join, IsFilled = false };
                var outward = branch == 1;
                shoot.Segments.Add(new BezierSegment(new Point(join.X + sign * (outward ? 8 : -5), join.Y + 5),
                    new Point(tip.X + sign * (outward ? 5 : 10), tip.Y - 13), tip, true));
                shoot.Segments.Add(new BezierSegment(new Point(tip.X - sign * 6, tip.Y + 7),
                    new Point(tip.X - sign * 12, tip.Y - 2), new Point(tip.X - sign * 5, tip.Y - 3), true));
                var branchGeometry = new PathGeometry(new[] { shoot }); branchGeometry.Freeze();
                canvas.Children.Add(new Path { Name = "GardenTrailBranch" + side + branch, Data = branchGeometry,
                    Stroke = stemBrush, StrokeThickness = 0.6, Opacity = 0.7, IsHitTestVisible = false,
                    StrokeStartLineCap = PenLineCap.Round, StrokeEndLineCap = PenLineCap.Round });
                for (var level = 0; level < 3; level++)
                {
                    branchGeometry.GetPointAtFractionLength(0.14 + level * 0.2, out var p, out var tangent);
                    var rotation = new RotateTransform(Math.Atan2(tangent.Y, tangent.X) * 180 / Math.PI + (level % 2 == 0 ? -60 : 60));
                    rotation.Freeze();
                    var art = new Path { Data = leaf, Fill = jade, Width = 4, Height = 2.5, Stretch = Stretch.Fill,
                        IsHitTestVisible = false, Opacity = 0.75, RenderTransformOrigin = new Point(0, 0.5), RenderTransform = rotation };
                    Canvas.SetLeft(art, p.X); Canvas.SetTop(art, p.Y - art.Height / 2); canvas.Children.Add(art);
                }
                var turn = new RotateTransform(sign * (24 + branch * 20)); turn.Freeze();
                var bloom = new Image { Name = "GardenTrailBranchFlower" + side + branch, Source = blossoms[(branch + side + 1) % blossoms.Length],
                    Width = branch == 1 ? 7 : 9, Height = branch == 1 ? 7 : 9, Stretch = Stretch.Uniform,
                    IsHitTestVisible = false, Opacity = 0.9, RenderTransformOrigin = new Point(0.5, 0.5), RenderTransform = turn };
                Canvas.SetLeft(bloom, tip.X - bloom.Width / 2); Canvas.SetTop(bloom, tip.Y - bloom.Height / 2); canvas.Children.Add(bloom);
            }
        }
    }

    internal void Hide() => layer.Visibility = Visibility.Collapsed;
    internal void RenderSide(int side, double scale, double phase)
    {
        scales[side].ScaleX = scales[side].ScaleY = scale;
        breezes[side].CenterX = 115 + (side == 0 ? -92 : 92) * scale;
        breezes[side].CenterY = 0;
        breezes[side].Angle = 0.75 * Math.Sin(phase + side * 1.1);
        layer.Visibility = Visibility.Visible;
    }
}
