using System;
using System.Windows;
using System.Windows.Media;

namespace CutePet.Desktop;

// Six distinct flower silhouettes share frozen vector art throughout the garden.
internal static class SwingFlowerArt
{
    internal static DrawingImage[] Create() => new[] { Rose(), Star(), Bell(), GoldFlower(), Plum(), Lotus() };
    private static readonly Geometry Rounded = Geometry.Parse("M0,0 C-9,-3 -10,-13 -3,-16 C1,-19 9,-16 9,-9 Q7,-2 0,0 Z");
    private static readonly Geometry Pointed = Geometry.Parse("M0,0 C-4,-5 -4,-10 0,-18 C4,-10 4,-5 0,0 Z");
    private static readonly Geometry Slender = Geometry.Parse("M0,0 C-3,-5 -3,-14 0,-18 C3,-14 3,-5 0,0 Z");
    private static readonly Pen Edge = new(new SolidColorBrush(Color.FromRgb(191, 166, 114)), 0.35);

    private static Brush Tint(byte r, byte g, byte b) => new LinearGradientBrush(Color.FromRgb(r, g, b), Color.FromRgb(255, 247, 226), 90);
    private static void Ring(DrawingGroup drawing, Geometry petal, Brush tint, int count, double scale, double offset = 0)
    {
        for (var i = 0; i < count; i++)
        {
            var transform = new TransformGroup();
            transform.Children.Add(new ScaleTransform(scale, scale)); transform.Children.Add(new RotateTransform(offset + i * 360.0 / count));
            var group = new DrawingGroup { Transform = transform };
            group.Children.Add(new GeometryDrawing(tint, Edge, petal)); drawing.Children.Add(group);
        }
    }
    private static void Heart(DrawingGroup drawing, double radius, int stamens = 0)
    {
        var gold = new SolidColorBrush(Color.FromRgb(224, 176, 64));
        drawing.Children.Add(new GeometryDrawing(new RadialGradientBrush(Colors.Ivory, Color.FromRgb(224, 176, 64)), Edge,
            new EllipseGeometry(new Point(), radius, radius)));
        for (var i = 0; i < stamens; i++)
        {
            var angle = i * 2 * Math.PI / stamens;
            var p = new Point((radius + 1.5) * Math.Cos(angle), (radius + 1.5) * Math.Sin(angle));
            drawing.Children.Add(new GeometryDrawing(null, new Pen(gold, 0.4), new LineGeometry(new Point(), p)));
            drawing.Children.Add(new GeometryDrawing(gold, null, new EllipseGeometry(p, 0.5, 0.5)));
        }
    }
    private static DrawingImage Finish(DrawingGroup drawing)
    {
        var image = new DrawingImage(drawing); image.Freeze(); return image;
    }
    private static DrawingImage Rose()
    {
        var drawing = new DrawingGroup();
        Ring(drawing, Rounded, Tint(210, 85, 152), 10, 1);
        Ring(drawing, Rounded, Tint(235, 130, 174), 8, 0.72, 18);
        Ring(drawing, Rounded, Tint(249, 179, 201), 6, 0.42, 36);
        Heart(drawing, 1.8); return Finish(drawing);
    }
    private static DrawingImage Star()
    {
        var drawing = new DrawingGroup();
        Ring(drawing, Pointed, Tint(143, 97, 202), 5, 1);
        Ring(drawing, Pointed, Tint(191, 175, 245), 5, 0.6, 36);
        Heart(drawing, 2.3, 5); return Finish(drawing);
    }
    private static DrawingImage Bell()
    {
        var drawing = new DrawingGroup();
        drawing.Children.Add(new GeometryDrawing(null, new Pen(new SolidColorBrush(Color.FromRgb(137, 176, 142)), 0.8),
            Geometry.Parse("M0,-16 Q4,-12 0,-9")));
        drawing.Children.Add(new GeometryDrawing(Tint(89, 141, 220), Edge,
            Geometry.Parse("M-3,-9 C-8,-5 -6,1 -10,5 Q-5,3 -5,8 Q0,5 4,8 Q5,2 9,5 C5,-1 6,-5 3,-9 Q0,-11 -3,-9 Z")));
        drawing.Children.Add(new GeometryDrawing(Tint(172, 145, 232), Edge,
            Geometry.Parse("M-1,-9 Q-3,-2 -2,6 Q0,4 2,6 Q3,-2 1,-9 Z")));
        drawing.Children.Add(new GeometryDrawing(null, new Pen(new SolidColorBrush(Color.FromRgb(231, 190, 101)), 0.6),
            Geometry.Parse("M0,5 L0,10 M-2,5 L-3,9 M2,5 L3,9")));
        foreach (var p in new[] { new Point(0, 10), new Point(-3, 9), new Point(3, 9) })
            drawing.Children.Add(new GeometryDrawing(new SolidColorBrush(Color.FromRgb(231, 190, 101)), null, new EllipseGeometry(p, 0.8, 0.8)));
        return Finish(drawing);
    }
    private static DrawingImage GoldFlower()
    {
        var drawing = new DrawingGroup();
        Ring(drawing, Slender, Tint(225, 163, 48), 16, 1);
        Ring(drawing, Slender, Tint(247, 202, 94), 12, 0.73, 12);
        Ring(drawing, Rounded, Tint(255, 223, 135), 8, 0.4, 24);
        Heart(drawing, 2.5, 8); return Finish(drawing);
    }
    private static DrawingImage Plum()
    {
        var drawing = new DrawingGroup();
        var petal = Geometry.Parse("M0,0 C-10,-3 -11,-14 -3,-16 Q0,-19 3,-16 C11,-14 10,-3 0,0 Z");
        Ring(drawing, petal, Tint(245, 147, 145), 5, 1, 18);
        Ring(drawing, petal, Tint(255, 224, 207), 5, 0.48, 18);
        Heart(drawing, 2, 10); return Finish(drawing);
    }
    private static DrawingImage Lotus()
    {
        var drawing = new DrawingGroup();
        Ring(drawing, Pointed, Tint(89, 183, 180), 8, 1);
        Ring(drawing, Rounded, Tint(166, 213, 219), 6, 0.7, 24);
        Ring(drawing, Pointed, Tint(216, 213, 247), 5, 0.4, 12);
        Heart(drawing, 2, 6); return Finish(drawing);
    }
}
