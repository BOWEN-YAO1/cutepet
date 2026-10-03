using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace CutePet.Desktop;

internal enum ScreenEdge { Left, Right, Top, Bottom }
internal sealed record EdgeAttachment(ScreenEdge Side, Rect Area, Size PetSize, Point Position);

internal static class ScreenEdgeLayout
{
    // Physical pixels throughout. Reserve a bottom strip when no taskbar space is reserved.
    internal static Rect SafeArea(Rect workArea, Rect screen, double dpi)
    {
        if (workArea.IsEmpty || screen.IsEmpty || !double.IsFinite(dpi) || dpi <= 0) return workArea;
        var bottomGap = screen.Bottom - workArea.Bottom;
        return bottomGap <= 2 * dpi && bottomGap >= 0
            ? new Rect(workArea.Left, workArea.Top, workArea.Width, Math.Max(0, workArea.Height - 12 * dpi)) : workArea;
    }
    internal static EdgeAttachment? Plan(Rect area, Size pet, Point released, double dpi, IEnumerable<ScreenEdge>? supported = null)
    {
        if (!Fits(area, pet) || !double.IsFinite(released.X) || !double.IsFinite(released.Y) || !double.IsFinite(dpi) || dpi <= 0) return null;
        var gaps = new[] { released.X - area.Left, area.Right - released.X - pet.Width,
            released.Y - area.Top, area.Bottom - released.Y - pet.Height };
        var candidates = (supported ?? Enum.GetValues<ScreenEdge>()).Distinct().Where(side => Enum.IsDefined(side))
            .Where(side => gaps[(int)side] >= -(side is ScreenEdge.Left or ScreenEdge.Right ? pet.Width : pet.Height) * 0.75
                && gaps[(int)side] <= 24 * dpi).OrderBy(side => Math.Abs(gaps[(int)side])).ThenBy(side => side).ToArray();
        return candidates.Length == 0 ? null : Anchor(candidates[0], area, pet, released);
    }
    internal static EdgeAttachment? Anchor(ScreenEdge side, Rect area, Size pet, Point origin)
    {
        if (!Fits(area, pet) || !Enum.IsDefined(side) || !double.IsFinite(origin.X) || !double.IsFinite(origin.Y)) return null;
        var x = side switch { ScreenEdge.Left => area.Left, ScreenEdge.Right => area.Right - pet.Width,
            _ => Math.Clamp(origin.X, area.Left, area.Right - pet.Width) };
        var y = side switch { ScreenEdge.Top => area.Top, ScreenEdge.Bottom => area.Bottom - pet.Height,
            _ => Math.Clamp(origin.Y, area.Top, area.Bottom - pet.Height) };
        return new(side, area, pet, new Point(x, y));
    }
    private static bool Fits(Rect area, Size pet) => !area.IsEmpty && double.IsFinite(area.Width) && double.IsFinite(area.Height)
        && double.IsFinite(area.X) && double.IsFinite(area.Y) && double.IsFinite(pet.Width) && double.IsFinite(pet.Height)
        && pet.Width > 0 && pet.Height > 0 && pet.Width <= area.Width && pet.Height <= area.Height;
}
