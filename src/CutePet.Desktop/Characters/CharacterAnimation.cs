using System;
using System.Windows.Media.Imaging;

namespace CutePet.Desktop;

internal enum CharacterFrame { Idle, Closed, Wave, Low, Look, Hover, Happy }

// Shared triggers; the selected package supplies clips and frame timings.
internal sealed class CharacterAnimation
{
    private CharacterPack pack = CharacterCatalog.BuiltIns[0];
    private string? transient;
    private double transientElapsed, baseElapsed;
    private bool low;
    public bool Low { get => low; set { if (low != value) { low = value; baseElapsed = 0;
        if (low && transient is "blink" or "look" or "hover") ResetTransient(); } } }
    public string Action => transient ?? (Low && pack.Actions.ContainsKey("low") ? "low" : "idle");
    public BitmapSource Image => pack.Actions[Action].At(transient is null ? baseElapsed : transientElapsed);
    public CharacterFrame Frame => Action switch
    { "low" => CharacterFrame.Low, "blink" => CharacterFrame.Closed,
        "greeting" => ReferenceEquals(Image, pack.Idle.Frames[0].Image) ? CharacterFrame.Idle : CharacterFrame.Wave,
        "look" => CharacterFrame.Look, "hover" => CharacterFrame.Hover, "happy" => CharacterFrame.Happy,
        _ => CharacterFrame.Idle };
    public void Configure(CharacterPack selected) { pack = selected; low = false; baseElapsed = 0; ResetTransient(); }
    public void Blink() { if (transient is null && !Low) Start("blink"); }
    public void Greet() => Start("greeting");
    public void ReactToClick(int choice)
    {
        var greeting = pack.Actions.ContainsKey("greeting");
        var happy = pack.Actions.ContainsKey("happy");
        if (greeting || happy) Start(happy && (!greeting || (choice & 1) == 1) ? "happy" : "greeting");
    }
    public bool TryAmbient(string action)
    {
        if (Low || action is not ("look" or "hover") || !pack.Actions.ContainsKey(action)) return false;
        if (transient is not null && !(action == "hover" && transient is "look" or "blink")) return false;
        Start(action);
        return true;
    }
    public void Preview(string action)
    {
        ResetTransient();
        Low = action == "low";
        if (action is not ("idle" or "low")) Start(action);
    }
    private void Start(string action)
    {
        if (!pack.Actions.ContainsKey(action)) return;
        transient = action;
        transientElapsed = 0;
    }
    public void Advance(TimeSpan elapsed)
    {
        var milliseconds = Math.Max(0, elapsed.TotalMilliseconds);
        if (transient is not null)
        {
            transientElapsed += milliseconds;
            if (transientElapsed >= pack.Actions[transient].Duration) ResetTransient();
        }
        else baseElapsed = (baseElapsed + milliseconds) % pack.Actions[Action].Duration;
    }
    public void ResetTransient() { transient = null; transientElapsed = 0; }
}
