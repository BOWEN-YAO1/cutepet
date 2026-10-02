using System;
using System.Windows.Media;

namespace CutePet.Desktop;

internal sealed class CloudMotionController
{
    private readonly MainWindow window;
    private readonly bool verification;
    private readonly CloudFlight flight = new();
    private double waiting, startX, targetX, top;
    private int direction = -1;
    internal bool Active => flight.Active;
    internal double RequestedX { get; private set; }
    internal CloudMotionController(MainWindow window, bool verification)
    { this.window = window; this.verification = verification; }
    internal bool Start()
    {
        if (Active || window.SelectedCharacter.CloudImage is null || window.Settings.PositionLocked
            || window.CharacterRestPose || window.Model.IsLow && !window.Model.IsStale) return false;
        var dpi = VisualTreeHelper.GetDpi(window);
        var plan = NativePlacement.PlanDrift(window, 96 * window.Settings.EffectiveCharacterScale * dpi.DpiScaleX, direction);
        startX = plan.Start;
        targetX = plan.Target;
        top = plan.Top;
        RequestedX = startX;
        flight.Start(window.SelectedCharacter.Actions.TryGetValue("summon-cloud", out var spell) ? spell.Duration : 0);
        waiting = 0;
        window.StartCloudSpell();
        Render();
        return true;
    }
    internal void Advance(TimeSpan elapsed)
    {
        if (window.SelectedCharacter.CloudImage is null || window.Settings.PositionLocked
            || window.CharacterRestPose || window.Model.IsLow && !window.Model.IsStale)
        { Cancel(); return; }
        if (!window.CanCloudMove) return;
        if (!Active)
        {
            if (!window.Settings.AutoCloud) { waiting = 0; return; }
            waiting += Math.Max(0, elapsed.TotalMilliseconds);
            if (waiting >= 18000 && window.CharacterIdle) Start();
            return;
        }
        var finished = flight.Advance(elapsed);
        RequestedX = startX + (targetX - startX) * flight.Travel;
        // Verification executes the same plan/clock while leaving the real desktop untouched.
        if (!verification) NativePlacement.Apply(window, RequestedX, top);
        Render();
        if (finished)
        {
            direction = targetX < startX ? 1 : -1;
            waiting = 0;
            window.SavePlacement();
        }
    }
    internal void Cancel()
    {
        flight.Cancel();
        waiting = 0;
        window.CancelCloudSpell();
        Render();
    }
    private void Render()
    {
        window.CloudArt.Opacity = flight.Opacity;
        window.CloudLift.Y = flight.Lift;
        window.CloudBob.Y = flight.Bob;
        window.GroundShadow.Opacity = 1 - flight.Opacity;
    }
}
