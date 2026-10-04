using System;

namespace CutePet.Desktop;

internal static class MotionEase
{
    internal static double Smooth(double t) { t = Math.Clamp(t, 0, 1); return t*t*t*(10+t*(-15+6*t)); }
}

// Preserve the standing float phase while gently lowering its amplitude for another pose.
internal sealed class IdleFloat
{
    private bool enabled;
    private double phase, amplitude, from, elapsed = 320;
    internal double Value => -2 * (1-Math.Cos(phase)) * amplitude;
    internal void SetEnabled(bool value)
    {
        if (enabled == value) return;
        enabled = value; from = amplitude; elapsed = 0;
    }
    internal void Advance(TimeSpan delta)
    {
        var ms = Math.Max(0, delta.TotalMilliseconds);
        elapsed = Math.Min(320, elapsed+ms);
        amplitude = from + ((enabled ? 1 : 0)-from)*MotionEase.Smooth(elapsed/320);
        if (amplitude > 0 || enabled) phase = (phase + ms*2*Math.PI/4400) % (2*Math.PI);
    }
    internal void Reset() { enabled = false; phase = amplitude = from = 0; elapsed = 320; }
}

internal readonly record struct CloudPose(double Opacity, double Lift, double Bob, double CloudBob, double Lean, double Tilt, double Breath)
{
    internal CloudPose Scale(double weight) => new(Opacity*weight, Lift*weight, Bob*weight, CloudBob*weight, Lean*weight, Tilt*weight, Breath*weight);
}

// The route stops immediately; its visible cloud and pose settle over a short finite interval.
internal sealed class CloudLanding
{
    internal const double DurationMs = 320;
    private CloudPose from;
    private double elapsed;
    internal bool Active { get; private set; }
    internal CloudPose Pose => Active ? from.Scale(1-MotionEase.Smooth(elapsed/DurationMs)) : default;
    internal void Begin(CloudPose pose) { from = pose; elapsed = 0; Active = pose.Opacity > 0 || pose.Lift != 0; }
    internal void Advance(TimeSpan delta) { elapsed = Math.Min(DurationMs, elapsed+Math.Max(0, delta.TotalMilliseconds)); if (elapsed == DurationMs) Active = false; }
    internal void Cancel() { Active = false; elapsed = 0; from = default; }
}
