using System;

namespace CutePet.Desktop;

internal enum CharacterFrame { Idle, Closed, Wave, Low }

// One bounded interaction, not an animation queue. Elapsed time is supplied by the UI clock.
internal sealed class CharacterAnimation
{
    private double blinkRemaining;
    private double greetingRemaining;
    public bool Low { get; set; }
    public CharacterFrame Frame => greetingRemaining > 0
        ? (int)((1080 - greetingRemaining) / 180) % 2 == 0 ? CharacterFrame.Wave : CharacterFrame.Idle
        : Low ? CharacterFrame.Low : blinkRemaining > 0 ? CharacterFrame.Closed : CharacterFrame.Idle;
    public void Blink() { if (greetingRemaining == 0 && !Low) blinkRemaining = 160; }
    public void Greet() { greetingRemaining = 1080; blinkRemaining = 0; }
    public void Advance(TimeSpan elapsed)
    {
        var milliseconds = Math.Max(0, elapsed.TotalMilliseconds);
        blinkRemaining = Math.Max(0, blinkRemaining - milliseconds);
        greetingRemaining = Math.Max(0, greetingRemaining - milliseconds);
    }
    public void ResetTransient() { blinkRemaining = greetingRemaining = 0; }
}
