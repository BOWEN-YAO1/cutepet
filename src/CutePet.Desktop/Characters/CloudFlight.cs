using System;

namespace CutePet.Desktop;

// A bounded trip, independent of WPF, screens and the character artwork.
internal sealed class CloudFlight
{
    private double spellDuration = 700;
    private double travelDuration = 8000;
    internal double DurationMs => spellDuration + travelDuration + 1000;
    private double elapsed;
    internal bool Active { get; private set; }
    internal bool Summoning => Active && elapsed < spellDuration;
    internal double Travel => Smooth(Math.Clamp((elapsed - spellDuration - 500) / travelDuration, 0, 1));
    internal double Opacity => !Active ? 0 : Math.Clamp((elapsed - spellDuration) / 500, 0, 1)
        * Math.Clamp((DurationMs - elapsed) / 500, 0, 1);
    internal double Lift => !Active ? 0 : -6 * Opacity;
    internal double Bob => Active ? Math.Sin(Math.Max(0, elapsed - spellDuration - 500) / 700) * 1.2 * Opacity : 0;
    internal void Start(double spellDurationMs = 700, double travelDurationMs = 8000)
    { elapsed = 0; spellDuration = Math.Clamp(spellDurationMs, 0, 30000); travelDuration = Math.Clamp(travelDurationMs, 2000, 120000); Active = true; }
    internal bool Advance(TimeSpan duration)
    {
        if (!Active) return false;
        elapsed = Math.Min(DurationMs, elapsed + Math.Max(0, duration.TotalMilliseconds));
        if (elapsed < DurationMs) return false;
        Active = false;
        return true;
    }
    internal void Cancel() { elapsed = 0; Active = false; }
    private static double Smooth(double x) => x * x * (3 - 2 * x);
    internal static double Target(double start, double width, double areaLeft, double areaRight,
        double distance, int direction)
    {
        var maximum = Math.Max(areaLeft, areaRight - width);
        start = Math.Clamp(start, areaLeft, maximum);
        var preferred = Math.Clamp(start + direction * distance, areaLeft, maximum);
        var opposite = Math.Clamp(start - direction * distance, areaLeft, maximum);
        return Math.Abs(preferred - start) >= Math.Abs(opposite - start) ? preferred : opposite;
    }
}
