using System;
using System.Windows;
using System.Threading.Tasks;

namespace CutePet.Desktop;

internal static class CloudVerification
{
    internal static async Task RunAsync(MainWindow window, Action<bool, string> check, Action<string> render,
        Action<bool> quota, Func<bool> savedAutoCloud)
    {
        var flight = new CloudFlight();
        flight.Start();
        flight.Advance(TimeSpan.FromMilliseconds(700));
        check(flight.Active && flight.Travel == 0 && flight.Opacity == 0, "cloud waits for the spell before appearing");
        flight.Advance(TimeSpan.FromMilliseconds(500));
        check(flight.Opacity == 1 && flight.Lift == -6 && flight.Travel == 0, "cloud appears and lifts before horizontal travel");
        flight.Advance(TimeSpan.FromSeconds(4));
        check(Math.Abs(flight.Travel - 0.5) < 0.001, "cloud trip eases through its midpoint");
        flight.Advance(TimeSpan.FromSeconds(5));
        check(!flight.Active && flight.Opacity == 0 && flight.Lift == 0 && flight.Travel == 1, "cloud finishes at its destination and clears visual offsets");
        flight.Start(3000);
        flight.Advance(TimeSpan.FromSeconds(2));
        check(flight.Summoning && flight.Opacity == 0, "custom cloud spells keep their configured duration before lifting");
        check(CloudFlight.Target(0, 350, 0, 1920, 96, -1) == 96,
            "left boundary chooses an inward cloud trip");
        check(CloudFlight.Target(1570, 350, 0, 1920, 96, 1) == 1474,
            "right boundary turns inward and includes quota width");
        check(CloudFlight.Target(-1750, 350, -1920, 0, 192, -1) == -1558,
            "negative monitor coordinates and scaled travel remain in the work area");
        check(CloudFlight.Target(0, 2200, 0, 1920, 96, 1) == 0,
            "an oversized desktop host cannot drift outside a small work area");

        quota(false);
        window.SetCharacter(PetCharacter.Tianyi);
        window.SetDetailsMode(DetailsMode.Hidden);
        window.PointerChanged(false);
        if (window.Settings.PositionLocked) window.TogglePositionLock();
        window.SummonCloud();
        check(window.CloudActive && window.CharacterArt.Source == window.SelectedCharacter.Actions["summon-cloud"].Frames[0].Image,
            "manual cloud trip uses the character pack's spell");
        window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(1200));
        check(window.CloudActive && window.CloudArt.Opacity == 1 && window.CurrentCharacterFrame == CharacterFrame.Idle,
            "host keeps the original standing art above its separate cloud layer");
        render("cloud-riding");
        var origin = window.CloudRequestedX;
        window.AdvanceCharacterAnimation(TimeSpan.FromSeconds(4));
        var midway = window.CloudRequestedX;
        check(Math.Abs(midway - origin) > 1, "cloud controller produces real horizontal window destinations");
        window.PointerChanged(true);
        window.AdvanceCharacterAnimation(TimeSpan.FromSeconds(10));
        check(window.CloudActive && window.CloudRequestedX == midway && window.CloudArt.Opacity == 1,
            "pointer arrival freezes the trip without an elapsed-time jump");
        window.PointerChanged(false);
        window.BeginDetailsMenu();
        window.AdvanceCharacterAnimation(TimeSpan.FromSeconds(10));
        check(window.CloudRequestedX == midway, "context menus pause cloud travel");
        window.EndDetailsMenu();
        window.SetDetailsMode(DetailsMode.Always);
        window.AdvanceCharacterAnimation(TimeSpan.FromSeconds(10));
        check(window.CloudRequestedX == midway, "pinned quota details pause cloud travel");
        window.SetDetailsMode(DetailsMode.Hidden);
        window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(4500));
        check(!window.CloudActive && window.CloudArt.Opacity == 0 && window.CloudLift.Y == 0,
            "resume completes the trip and restores the standing layout");
        window.SummonCloud();
        window.TogglePositionLock();
        window.SummonCloud();
        check(!window.CloudActive && window.CloudArt.Opacity == 0, "locking cancels and prevents cloud motion");
        window.TogglePositionLock();
        window.SummonCloud();
        window.ToggleCharacterRest();
        check(!window.CloudActive && window.CharacterResting, "throne rest cancels the cloud before its own spell");
        window.WakeCharacterImmediately();
        window.SummonCloud();
        window.BeginQuotaDrag(new Point(16, 16));
        check(!window.CloudActive && window.CloudLift.Y == 0, "quota dragging immediately clears cloud motion");
        window.EndQuotaDrag(cancel: true);
        window.SummonCloud();
        window.SetCharacterScale(1.2);
        check(!window.CloudActive, "layout changes cancel an obsolete cloud destination");
        window.SetCharacterScale(1);
        window.SummonCloud();
        quota(true);
        check(!window.CloudActive && window.CurrentCharacterFrame == CharacterFrame.Low, "fresh low quota immediately clears the cloud");
        quota(false);
        window.SummonCloud();
        window.PlayCharacterInteraction();
        check(!window.CloudActive && window.CurrentCharacterFrame is CharacterFrame.Happy or CharacterFrame.Wave,
            "clicking interrupts cloud travel and plays the normal response");
        window.WakeCharacterImmediately();
        var autoRest = window.Settings.AutoRest;
        if (autoRest) window.ToggleAutoRest();
        if (!window.Settings.AutoCloud) window.ToggleAutoCloud();
        var quiet = window.NextActivityDelay;
        check(window.NextActivity == QuietActivity.Cloud && quiet >= 25000 && quiet <= 50000, "automatic cloud uses the unified randomized quiet plan");
        window.AdvanceAmbient(TimeSpan.FromMilliseconds(quiet-1));
        check(!window.CloudActive, "automatic cloud waits for its quiet interval");
        window.AdvanceCharacterAnimation(TimeSpan.FromSeconds(2));
        window.AdvanceAmbient(TimeSpan.FromMilliseconds(1));
        check(window.CloudActive && savedAutoCloud(), "automatic cloud starts and its switch persists");
        window.ToggleAutoCloud();
        window.AdvanceCloud(TimeSpan.FromMinutes(1));
        check(!window.CloudActive && !savedAutoCloud(), "disabling automatic cloud cancels and prevents new trips");
        if (autoRest) window.ToggleAutoRest();
        window.SummonCloud();
        check(window.CloudActive, "manual cloud remains available when automatic cloud is off");
        CloudMotionVerification.Run(window, check, render);
        window.SummonCloud();
        window.StartAnimationClock();
        await Task.Delay(1450);
        check(window.CloudActive && window.CloudArt.Opacity == 1 && window.CurrentCharacterFrame == CharacterFrame.Idle,
            "actual WPF clock completes the cloud spell and lift without native desktop movement");
        window.HidePet();
        check(!window.CloudActive && window.CloudArt.Opacity == 0, "hiding clears cloud clocks and visuals");
        window.SummonCloud();
        window.SetCharacter(PetCharacter.Cat);
        window.SummonCloud();
        check(!window.CloudActive && window.CloudArt.Source is null, "switching clears cloud art and the original cat retains its behavior");
    }
}
