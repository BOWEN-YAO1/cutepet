using System;
using System.Linq;
using System.Windows;

namespace CutePet.Desktop;

internal sealed record RoamingRoute(Rect Area, Size PetSize, Point Start, Point Target, double TravelMs)
{
    internal Point Control1 { get; init; }
    internal Point Control2 { get; init; }
    internal Point At(double progress)
    {
        var t = Math.Clamp(progress, 0, 1); var u = 1 - t;
        return new(u * u * u * Start.X + 3 * u * u * t * Control1.X + 3 * u * t * t * Control2.X + t * t * t * Target.X,
            u * u * u * Start.Y + 3 * u * u * t * Control1.Y + 3 * u * t * t * Control2.Y + t * t * t * Target.Y);
    }
    internal Vector Heading(double progress)
    {
        var t = Math.Clamp(progress, 0, 1); var u = 1 - t;
        var tangent = 3 * u * u * (Control1 - Start) + 6 * u * t * (Control2 - Control1) + 3 * t * t * (Target - Control2);
        if (tangent.Length > 0) tangent.Normalize();
        return tangent;
    }
}

// Geometry uses physical desktop pixels, including negative monitor origins.
internal static class DesktopRoaming
{
    internal static RoamingRoute? Plan(Rect area, Size pet, Point origin, Rect quota, double dpi, Func<double> sample)
    {
        if (pet.Width > area.Width || pet.Height > area.Height || pet.Width <= 0 || pet.Height <= 0) return null;
        var start = Clamp(origin, area, pet);
        var maximumX = area.Right - pet.Width;
        var maximumY = area.Bottom - pet.Height;
        var exclusion = quota; exclusion.Inflate(12 * dpi, 12 * dpi);
        var minimum = Math.Min(240 * dpi, Math.Sqrt(Math.Pow(maximumX - area.Left, 2) + Math.Pow(maximumY - area.Top, 2)) * 0.3);
        Point? target = null;
        for (var i = 0; i < 32; i++)
        {
            var candidate = new Point(area.Left + Math.Clamp(sample(), 0, 1) * (maximumX - area.Left),
                area.Top + Math.Clamp(sample(), 0, 1) * (maximumY - area.Top));
            if ((candidate - start).Length >= Math.Max(1, minimum) && !exclusion.IntersectsWith(new Rect(candidate, pet)))
            { target = candidate; break; }
        }
        if (target is null)
        {
            var corners = new[] { new Point(area.Left, area.Top), new Point(maximumX, area.Top),
                new Point(area.Left, maximumY), new Point(maximumX, maximumY) };
            target = corners.Where(point => (point - start).Length >= 1 && !exclusion.IntersectsWith(new Rect(point, pet)))
                .OrderByDescending(point => (point - start).Length).Select(point => (Point?)point).FirstOrDefault();
        }
        if (target is not Point destination) return null;
        var delta = destination - start;
        var bend = new Vector(-delta.Y, delta.X); bend.Normalize();
        bend *= Math.Min(48 * dpi, delta.Length * 0.12);
        // The curve stays inside the work area when all four controls do.
        var first = Clamp(start + delta / 3 + bend, area, pet);
        var second = Clamp(start + delta * (2.0 / 3) + bend, area, pet);
        var peakDerivative = 3 * Math.Max((first - start).Length, Math.Max((second - first).Length, (destination - second).Length));
        // Quintic easing peaks at 1.875; account for the curved path's derivative.
        var duration = Math.Clamp(peakDerivative / Math.Max(0.1, dpi) * 1.875 / 40 * 1000, 8000, 120000);
        return new(area, pet, start, destination, duration) { Control1 = first, Control2 = second };
    }
    internal static Point Recall(Rect area, Size pet, Rect quota)
    {
        var candidates = new[] { new Point(quota.Right + 12, quota.Bottom - pet.Height),
            new Point(quota.Left - pet.Width - 12, quota.Bottom - pet.Height),
            new Point(quota.Left, quota.Top - pet.Height - 12), new Point(quota.Left, quota.Bottom + 12) };
        foreach (var candidate in candidates)
        {
            var bounded = Clamp(candidate, area, pet);
            if (!quota.IntersectsWith(new Rect(bounded, pet))) return bounded;
        }
        return Clamp(candidates[0], area, pet);
    }
    internal static Point Clamp(Point point, Rect area, Size pet) => new(
        Math.Clamp(point.X, area.Left, Math.Max(area.Left, area.Right - pet.Width)),
        Math.Clamp(point.Y, area.Top, Math.Max(area.Top, area.Bottom - pet.Height)));
}
