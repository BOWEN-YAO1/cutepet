using System;
using System.Windows;

namespace CutePet.Desktop;

internal static class CloudMotionVerification
{
    internal static void Run(MainWindow window, Action<bool, string> check, Action<string> render)
    {
        var flight = new CloudFlight(); flight.Start();
        flight.Advance(TimeSpan.FromMilliseconds(1200));
        check(flight.Bob == 0 && flight.Lean(1) == 0 && flight.CloudBreath == 0,
            "floating pose starts at rest after its smooth lift");
        flight.Advance(TimeSpan.FromMilliseconds(2000));
        check(Math.Abs(flight.Bob - flight.CloudBob) > 0.1 && Math.Abs(flight.Lean(1)) <= 1.8
            && Math.Abs(flight.CloudBreath) <= 0.025,
            "cloud lags behind the bounded figure bob without stretching the character");
        flight.Advance(TimeSpan.FromMilliseconds(6000));
        check(flight.Active && flight.Travel == 1 && flight.Bob == 0 && flight.Lean(1) == 0 && flight.CloudBreath == 0,
            "floating pose settles before the cloud fades at the destination");
        flight.Cancel();
        check(flight.Bob == 0 && flight.CloudBob == 0 && flight.Lean(-1) == 0 && flight.CloudTilt == 0,
            "cancel resets every floating channel");
        var nearStart = new CloudFlight(); nearStart.Start(); nearStart.Advance(TimeSpan.FromMilliseconds(1201));
        var nearEnd = new CloudFlight(); nearEnd.Start(); nearEnd.Advance(TimeSpan.FromMilliseconds(9199));
        check(nearStart.Travel < 1e-9 && 1 - nearEnd.Travel < 1e-9,
            "departure and arrival approach zero velocity and acceleration");

        window.WakeCharacterImmediately();
        window.SummonCloud();
        var quota = window.QuotaHost.Position;
        window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(3200));
        check(window.CloudLean.Angle != 0 && window.CloudCharacterBob.Y != window.CloudBob.Y
            && window.EdgeStretch.ScaleY == 1 && window.QuotaHost.Position == quota,
            "figure and cloud use separate motion layers while quota stays fixed");
        render("cloud-flowing");
        var lean = window.CloudLean.Angle; var bob = window.CloudCharacterBob.Y; var breath = window.CloudBreath.ScaleX;
        window.BeginDetailsMenu();
        window.AdvanceCharacterAnimation(TimeSpan.FromSeconds(10));
        check(window.CloudLean.Angle == lean && window.CloudCharacterBob.Y == bob && window.CloudBreath.ScaleX == breath,
            "menu pauses all floating channels without accumulated motion");
        window.EndDetailsMenu();
        window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(1000));
        check(window.CurrentCharacterFrame == CharacterFrame.Closed,
            "cloud cruising can blink using the character pack's existing frame");
        window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(160));
        check(window.CurrentCharacterFrame == CharacterFrame.Idle && window.CloudActive,
            "cruising blink returns to its floating pose without cancelling the route");
        window.TogglePositionLock();
        check(window.CloudLean.Angle == 0 && window.CloudCharacterBob.Y == 0 && window.CloudBob.Y == 0
            && window.CloudRoll.Angle == 0 && window.CloudBreath.ScaleX == 1 && window.CloudBreath.ScaleY == 1,
            "locking clears all cloud transforms before subsequent edge and throne poses");
        window.TogglePositionLock();
    }
}
