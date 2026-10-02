using System;
using System.Windows;

namespace CutePet.Desktop;

internal enum ScreenEdge { Left, Right }
internal sealed record EdgeAttachment(ScreenEdge Side, Rect Area, Size PetSize, Point Position);

internal static class ScreenEdgeLayout
{
    // All inputs and results are physical pixels; the snap distance remains 24 DIPs.
    internal static EdgeAttachment? Plan(Rect area, Size pet, Point released, double dpi)
    {
        if (area.IsEmpty || pet.Width <= 0 || pet.Height <= 0 || pet.Width > area.Width || pet.Height > area.Height
            || !double.IsFinite(released.X) || !double.IsFinite(released.Y) || !double.IsFinite(dpi) || dpi <= 0) return null;
        var left = released.X - area.Left;
        var right = area.Right - released.X - pet.Width;
        bool Near(double gap) => gap >= -pet.Width * 0.75 && gap <= 24 * dpi;
        if (!Near(left) && !Near(right)) return null;
        var side = Near(left) && (!Near(right) || Math.Abs(left) <= Math.Abs(right)) ? ScreenEdge.Left : ScreenEdge.Right;
        return new(side, area, pet, new(side == ScreenEdge.Left ? area.Left : area.Right - pet.Width,
            Math.Clamp(released.Y, area.Top, area.Bottom - pet.Height)));
    }
}
