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
    private double CruiseMs => Math.Max(0, elapsed - spellDuration - 500);
    internal double Travel => Smooth(Math.Clamp(CruiseMs / travelDuration, 0, 1));
    internal double Opacity => !Active ? 0 : Smooth(Math.Clamp((elapsed - spellDuration) / 500, 0, 1))
        * Smooth(Math.Clamp((DurationMs - elapsed) / 500, 0, 1));
    internal double Lift => !Active ? 0 : -6 * Opacity;
    // Ease the floating pose in and out independently of the cloud's visibility.
    private double Envelope => !Active ? 0 : Smooth(Math.Clamp(CruiseMs / 900, 0, 1))
        * Smooth(Math.Clamp((travelDuration - CruiseMs) / 1200, 0, 1));
    internal double Bob => (2.1 * Math.Sin(CruiseMs / 850) + 0.45 * Math.Sin(CruiseMs / 1430)) * Envelope;
    internal double CloudBob => (1.7 * Math.Sin((CruiseMs - 160) / 850) + 0.35 * Math.Sin(CruiseMs / 1430)) * Envelope;
    internal double Lean(double heading) => (Math.Clamp(heading, -1, 1) * 1.15 + 0.65 * Math.Sin(CruiseMs / 1200)) * Envelope;
    internal double CloudTilt => 0.65 * Math.Sin((CruiseMs - 160) / 1200) * Envelope;
    internal double CloudBreath => 0.025 * Math.Sin((CruiseMs - 160) / 1000) * Envelope;
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
    private static double Smooth(double x) => x * x * x * (10 + x * (-15 + 6 * x));
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
