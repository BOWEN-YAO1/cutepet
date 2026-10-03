using System;

namespace CutePet.Desktop;

internal readonly record struct SideEdgePose(double Peek, double Lift, double Angle);

// Small, finite gestures around the border grip; artwork and durations remain package-owned.
internal static class SideEdgeMotion
{
    internal static readonly string[] Responses = { "edge-peek", "edge-shy", "edge-sway", "edge-nod" };
    internal static SideEdgePose Sample(string action, double progress)
    {
        var t = Math.Clamp(progress, 0, 1);
        var envelope = Math.Pow(Math.Sin(Math.PI * t), 2);
        return action switch
        {
            "edge-shy" => new(t < .4 ? -36 * Math.Pow(Math.Sin(Math.PI * t / .4), 2)
                : 2 * Math.Pow(Math.Sin(Math.PI * (t - .4) / .6), 2),
                -1.5 * envelope, 1.8 * Math.Sin(2 * Math.PI * t) * envelope),
            "edge-sway" => new(2 * envelope, 1.6 * Math.Sin(2 * Math.PI * t) * envelope,
                3.2 * Math.Sin(4 * Math.PI * t) * envelope),
            "edge-nod" => new(2 * envelope, 1.5 * Math.Sin(4 * Math.PI * t) * envelope,
                1.8 * Math.Sin(4 * Math.PI * t) * envelope),
            "edge-peek" => new(2 * envelope, 0, 0),
            _ => new(8 * envelope, 0, 0)
        };
    }
}
