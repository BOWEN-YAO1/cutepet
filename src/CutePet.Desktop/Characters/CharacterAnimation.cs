using System;
using System.Windows.Media.Imaging;

namespace CutePet.Desktop;

internal enum CharacterFrame { Idle, Closed, Wave, Low, Look, Hover, Happy, Conjure, Sit, Rise }

// Shared triggers; the selected package supplies clips and frame timings.
internal sealed class CharacterAnimation
{
    private CharacterPack pack = CharacterCatalog.BuiltIns[0];
    private string? transient;
    private double transientElapsed, baseElapsed;
    private bool low;
    private bool resting;
    private string? afterStanding;
    private bool standAfterConjure;
    public bool Resting => resting;
    public bool RestPose => resting || transient == "stand";
    public bool Low
    {
        get => low;
        set
        {
            if (low == value) return;
            low = value;
            baseElapsed = 0;
            if (!low) return;
            resting = false;
            afterStanding = null;
            standAfterConjure = false;
            if (transient is "blink" or "look" or "hover" or "conjure" or "stand" or "summon-cloud") ResetTransient();
        }
    }
    public string Action => transient ?? (Low && pack.Actions.ContainsKey("low") ? "low" : resting ? "sit" : "idle");
    public BitmapSource Image => pack.Actions[Action].At(transient is null ? baseElapsed : transientElapsed);
    public CharacterFrame Frame => Action switch
    { "low" => CharacterFrame.Low, "blink" => CharacterFrame.Closed,
        "greeting" => ReferenceEquals(Image, pack.Idle.Frames[0].Image) ? CharacterFrame.Idle : CharacterFrame.Wave,
        "look" => CharacterFrame.Look, "hover" => CharacterFrame.Hover, "happy" => CharacterFrame.Happy,
        "conjure" => CharacterFrame.Conjure, "sit" => CharacterFrame.Sit, "stand" => CharacterFrame.Rise,
        _ => CharacterFrame.Idle };
    public void Configure(CharacterPack selected) { pack = selected; low = false; baseElapsed = 0; Reset(); }
    public void Blink() { if (transient is null && !Low && !RestPose) Start("blink"); }
    public void Greet() => Respond("greeting");
    private void Respond(string action)
    {
        if (RestPose)
        {
            StandUp();
            if (transient == "stand" || standAfterConjure)
            { afterStanding = pack.Actions.ContainsKey(action) ? action : null; return; }
        }
        Start(action);
    }
    public bool SitDown()
    {
        if (Low || RestPose || !pack.Actions.ContainsKey("sit")) return false;
        Reset();
        resting = true;
        baseElapsed = 0;
        Start("conjure");
        return true;
    }
    public void StandUp()
    {
        if (transient == "stand" || standAfterConjure) return;
        if (!resting) return;
        double? reverseAt = null;
        if (transient == "conjure")
        {
            if (pack.Actions.TryGetValue("stand", out var rise))
            {
                var lowering = pack.Actions["conjure"];
                var position = lowering.PositionAt(transientElapsed);
                var current = lowering.Frames[position.Index].Image;
                double before = 0;
                foreach (var frame in rise.Frames)
                {
                    if (ReferenceEquals(frame.Image, current))
                    { reverseAt = before + frame.DurationMs * Math.Min(0.999999, 1 - position.Fraction); break; }
                    before += frame.DurationMs;
                }
            }
            // Old/custom clips with no matching pose finish lowering before they rise.
            if (reverseAt is null) { standAfterConjure = true; return; }
        }
        Reset();
        baseElapsed = 0;
        Start("stand");
        if (reverseAt is double offset) transientElapsed = offset;
    }
    public void ReactToClick(int choice)
    {
        var greeting = pack.Actions.ContainsKey("greeting");
        var happy = pack.Actions.ContainsKey("happy");
        if (greeting || happy) Respond(happy && (!greeting || (choice & 1) == 1) ? "happy" : "greeting");
        else if (RestPose) StandUp();
    }
    public bool TryAmbient(string action)
    {
        if (Low || RestPose || action is not ("look" or "hover") || !pack.Actions.ContainsKey(action)) return false;
        if (transient is not null && !(action == "hover" && transient is "look" or "blink")) return false;
        Start(action);
        return true;
    }
    public void Preview(string action)
    {
        Reset();
        Low = action == "low";
        resting = action is "sit" or "conjure" && pack.Actions.ContainsKey("sit");
        if (action is not ("idle" or "low" or "sit")) Start(action);
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
        while (transient is not null)
        {
            var remaining = pack.Actions[transient].Duration - transientElapsed;
            if (milliseconds < remaining) { transientElapsed += milliseconds; return; }
            milliseconds -= remaining;
            var finished = transient;
            ResetTransient();
            if (finished == "conjure" && standAfterConjure)
            {
                standAfterConjure = false;
                resting = false;
                Start("stand");
                if (transient is null) StartPendingResponse();
            }
            if (finished == "stand") StartPendingResponse();
        }
        baseElapsed = (baseElapsed + milliseconds) % pack.Actions[Action].Duration;
    }
    public void ResetTransient() { transient = null; transientElapsed = 0; }
    private void StartPendingResponse()
    { if (afterStanding is string response) { afterStanding = null; Start(response); } }
    public void Reset() { resting = false; standAfterConjure = false; afterStanding = null; ResetTransient(); }
}
