using System;
using System.Windows.Media;
using System.Windows;

namespace CutePet.Desktop;

internal sealed class CloudMotionController
{
    private readonly MainWindow window;
    private readonly bool verification;
    private readonly CloudFlight flight = new();
    private double waiting, startX, targetX, top, targetY;
    internal RoamingRoute? Route { get; private set; }
    internal double TripDuration => flight.DurationMs;
    private int direction = -1;
    internal bool Active => flight.Active;
    internal double RequestedX { get; private set; }
    internal double RequestedY { get; private set; }
    internal CloudMotionController(MainWindow window, bool verification)
    { this.window = window; this.verification = verification; }
    internal bool Start(bool roam = false)
    {
        if (Active || window.ScreenEdgeActive || window.SelectedCharacter.CloudImage is null || window.Settings.PositionLocked
            || window.CharacterRestPose || window.Model.IsLow && !window.Model.IsStale) return false;
        var dpi = VisualTreeHelper.GetDpi(window);
        var plan = NativePlacement.PlanDrift(window, 96 * window.Settings.EffectiveCharacterScale * dpi.DpiScaleX, direction);
        Route = null;
        startX = plan.Start;
        targetX = plan.Target;
        top = plan.Top;
        targetY = top;
        if (roam)
        {
            var bounds = NativePlacement.RoamingBounds(window);
            var quota = window.QuotaHost.Position;
            var quotaBounds = NativePlacement.RoamingBounds(window.QuotaHost);
            Route = DesktopRoaming.Plan(bounds.Area, bounds.Size, bounds.Origin, new Rect(quota, quotaBounds.Size), dpi.DpiScaleX, Random.Shared.NextDouble);
            if (Route is null) { waiting = 0; return false; }
            startX = Route.Start.X; top = Route.Start.Y; targetX = Route.Target.X; targetY = Route.Target.Y;
        }
        RequestedX = startX;
        RequestedY = top;
        flight.Start(window.SelectedCharacter.Actions.TryGetValue("summon-cloud", out var spell) ? spell.Duration : 0, Route?.TravelMs ?? 8000);
        waiting = 0;
        window.StartCloudSpell();
        Render();
        return true;
    }
    internal void Advance(TimeSpan elapsed)
    {
        if (window.ScreenEdgeActive || window.SelectedCharacter.CloudImage is null || window.Settings.PositionLocked
            || window.CharacterRestPose || window.Model.IsLow && !window.Model.IsStale)
        { Cancel(); return; }
        if (Active && Route is not null)
        {
            var current = NativePlacement.RoamingBounds(window);
            if (current.Area != Route.Area || current.Size != Route.PetSize)
            {
                Cancel();
                if (!verification) { NativePlacement.Apply(window, current.Origin.X, current.Origin.Y); window.SavePlacement(); }
                return;
            }
            var quota = window.QuotaHost.Position;
            var exclusion = new Rect(quota, NativePlacement.RoamingBounds(window.QuotaHost).Size);
            exclusion.Inflate(12 * VisualTreeHelper.GetDpi(window).DpiScaleX, 12 * VisualTreeHelper.GetDpi(window).DpiScaleY);
            if (exclusion.IntersectsWith(new Rect(Route.Target, Route.PetSize))) { Cancel(); return; }
        }
        if (!window.CanCloudMove) return;
        if (!Active)
        {
            if (!window.Settings.AutoCloud) { waiting = 0; return; }
            waiting += Math.Max(0, elapsed.TotalMilliseconds);
            if (waiting >= 18000 && window.CharacterIdle) Start(roam: true);
            return;
        }
        var finished = flight.Advance(elapsed);
        RequestedX = startX + (targetX - startX) * flight.Travel;
        RequestedY = top + (targetY - top) * flight.Travel;
        if (Route is not null)
        {
            var point = Route.At(flight.Travel);
            RequestedX = point.X; RequestedY = point.Y;
        }
        // Verification executes the same plan/clock while leaving the real desktop untouched.
        if (!verification) NativePlacement.Apply(window, RequestedX, RequestedY);
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
        Route = null;
        waiting = 0;
        window.CancelCloudSpell();
        Render();
    }
    private void Render()
    {
        window.CloudArt.Opacity = flight.Opacity;
        window.CloudLift.Y = flight.Lift;
        window.CloudCharacterBob.Y = flight.Bob;
        window.CloudBob.Y = flight.CloudBob;
        window.CloudLean.Angle = flight.Lean(Route?.Heading(flight.Travel).X ?? Math.Sign(targetX - startX));
        window.CloudRoll.Angle = flight.CloudTilt;
        window.CloudBreath.ScaleX = 1 + flight.CloudBreath;
        window.CloudBreath.ScaleY = 1 - flight.CloudBreath;
        window.GroundShadow.Opacity = 1 - flight.Opacity;
    }
}
