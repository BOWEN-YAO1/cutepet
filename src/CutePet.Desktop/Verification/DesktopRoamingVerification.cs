using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media.Imaging;

namespace CutePet.Desktop;

internal static class DesktopRoamingVerification
{
    internal static void Run(MainWindow window, string directory, Action<bool, string> check)
    {
        var emptyQuota = new Rect(3000, 3000, 116, 68);
        foreach (var (area, size, origin) in new[] {
            (new Rect(0, 0, 1920, 1040), new Size(230, 178), new Point(1600, 800)),
            (new Rect(-1920, -240, 1920, 1040), new Size(460, 356), new Point(-500, 300)),
            (new Rect(1920, 0, 2560, 1400), new Size(345, 267), new Point(2400, 400)) })
        {
            var sampleIndex = 0;
            double Sample() => (++sampleIndex & 1) == 1 ? 0.1 : 0.2;
            var route = DesktopRoaming.Plan(area, size, origin, emptyQuota, 1.5, Sample)!;
            check(route is not null && area.Contains(new Rect(route.Start, size)) && area.Contains(new Rect(route.Target, size)),
                "roam endpoints include the full scaled pet and remain in the current work area " + area.Left);
            check(Math.Abs(route!.Target.X - route.Start.X) > 1 && Math.Abs(route.Target.Y - route.Start.Y) > 1,
                "desktop roam moves horizontally and vertically " + area.Left);
            var flight = new CloudFlight();
            flight.Start(700, route.TravelMs);
            for (var i = 0; i <= 20; i++)
            {
                var point = route.At(flight.Travel);
                check(area.Contains(new Rect(point, size)), "every roam sample stays within work-area bounds " + area.Left + ":" + i);
                flight.Advance(TimeSpan.FromMilliseconds((flight.DurationMs + 1) / 20));
            }
            check((route.At(0) - route.Start).Length < 0.001 && (route.At(1) - route.Target).Length < 0.001
                && (route.At(0.5) - (route.Start + (route.Target - route.Start) / 2)).Length > 1,
                "curved route keeps exact endpoints and a visible arc " + area.Left);
            check(area.Contains(new Rect(route.Control1, size)) && area.Contains(new Rect(route.Control2, size)),
                "curve controls keep the full scaled pet inside the work area " + area.Left);
        }
        var fallback = DesktopRoaming.Plan(new Rect(0, 0, 1920, 1040), new Size(230, 178), new Point(0, 0),
            new Rect(0, 0, 116, 68), 1, () => 0);
        check(fallback is not null && fallback.Target.X > 1000 && fallback.Target.Y > 500,
            "repeated unsuitable random samples fall back to a distant safe corner");
        check(DesktopRoaming.Plan(new Rect(0, 0, 200, 150), new Size(230, 178), new Point(), emptyQuota, 1, () => 0.5) is null,
            "oversized pets cannot begin an out-of-bounds roam");
        check(DesktopRoaming.Plan(new Rect(0, 0, 230, 178), new Size(230, 178), new Point(), emptyQuota, 1, () => 0.5) is null,
            "an exact-fit screen safely rejects a trip with no travel room");
        check(DesktopRoaming.Plan(new Rect(0, 0, 400, 300), new Size(230, 178), new Point(), new Rect(0, 0, 400, 300),
            1, () => 0.5) is null, "roaming does not pick an endpoint hidden under a screen-filling quota panel");
        foreach (var card in new[] { new Rect(1800, 900, 116, 68), new Rect(0, 0, 116, 68), new Rect(800, 400, 224, 40) })
        {
            var area = new Rect(0, 0, 1920, 1040);
            var size = new Size(230, 178);
            var recalled = DesktopRoaming.Recall(area, size, card);
            check(area.Contains(new Rect(recalled, size)) && !card.IntersectsWith(new Rect(recalled, size)),
                "recall remains visible beside quota at " + card.Left);
        }
        var shortFlight = new CloudFlight();
        shortFlight.Start(500, 16000);
        shortFlight.Advance(TimeSpan.FromMilliseconds(9000));
        check(shortFlight.Active && Math.Abs(shortFlight.Travel - 0.5) < 0.001,
            "variable-duration flight reaches its eased midpoint at the correct time");
        shortFlight.Advance(TimeSpan.FromSeconds(9));
        check(!shortFlight.Active && shortFlight.Travel == 1 && shortFlight.Opacity == 0,
            "variable-duration flight settles and clears its cloud");
        window.SetCharacter(PetCharacter.Tianyi);
        window.SetDetailsMode(DetailsMode.Hidden);
        window.PointerChanged(false);
        window.CharacterPointerChanged(false);
        if (window.Settings.PositionLocked) window.TogglePositionLock();
        window.WakeCharacterImmediately();
        var quotaPosition = window.QuotaHost.Position;
        window.RoamDesktop();
        var actual = window.CloudRoute;
        check(window.CloudActive && actual is not null, "host manual roam plans a current-monitor destination");
        var target = actual!.Target;
        check(actual.Area.Contains(new Rect(target, actual.PetSize)), "host destination excludes taskbar and includes full pet bounds");
        window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(1200 + actual.TravelMs / 2));
        var midpoint = new Point(window.CloudRequestedX, window.CloudRequestedY);
        check((midpoint - actual.Start).Length > 1 && window.QuotaHost.Position == quotaPosition,
            "host roam travels in desktop coordinates while quota remains fixed");
        check((midpoint - actual.At(0.5)).Length < 0.01,
            "host follows the same curved path used by geometry verification");
        window.PointerChanged(true);
        window.AdvanceCharacterAnimation(TimeSpan.FromMinutes(2));
        check(window.CloudActive && midpoint == new Point(window.CloudRequestedX, window.CloudRequestedY),
            "mouse arrival pauses long trips without accumulating a later jump");
        window.PointerChanged(false);
        window.BeginDetailsMenu();
        window.AdvanceCharacterAnimation(TimeSpan.FromMinutes(2));
        check(midpoint == new Point(window.CloudRequestedX, window.CloudRequestedY), "menus pause the same two-axis route");
        window.EndDetailsMenu();
        window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(window.CloudTripDuration));
        check(!window.CloudActive && (target - new Point(window.CloudRequestedX, window.CloudRequestedY)).Length < 0.01,
            "a resumed long trip ends exactly at its selected destination");
        window.RoamDesktop();
        window.QuotaHost.MoveTo(window.CloudRoute!.Target);
        window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(40));
        check(!window.CloudActive && window.CloudRoute is null, "quota moving over a selected destination cancels the obsolete route");
        window.QuotaHost.MoveTo(quotaPosition);
        window.RoamDesktop();
        window.RecallPet();
        check(!window.CloudActive && !window.CharacterRestPose && window.LastRecallPosition is not null
            && window.QuotaHost.Position == quotaPosition, "recall cancels roaming and leaves quota in place");
        if (!window.Settings.AutoCloud) window.ToggleAutoCloud();
        var autoRest = window.Settings.AutoRest;
        if (autoRest) window.ToggleAutoRest();
        var quiet = window.NextActivityDelay;
        window.AdvanceAmbient(TimeSpan.FromMilliseconds(quiet-1));
        check(!window.CloudActive, "recall waits before permitting another automatic departure");
        window.AdvanceCharacterAnimation(TimeSpan.FromSeconds(2));
        window.AdvanceAmbient(TimeSpan.FromMilliseconds(1));
        check(window.CloudActive && window.CloudRoute is not null, "free activity starts a full-desktop route after its quiet interval");
        window.ToggleAutoCloud();
        window.AdvanceCloud(TimeSpan.FromMinutes(3));
        check(!window.CloudActive && !window.Settings.AutoCloud, "free activity switch cancels and suppresses automatic trips");
        if (autoRest) window.ToggleAutoRest();
        window.RoamDesktop();
        check(window.CloudActive, "manual roaming remains available when free activity is disabled");
        window.TogglePositionLock();
        window.RoamDesktop();
        check(!window.CloudActive, "locked positions cancel and prevent desktop roaming");
        window.RecallPet();
        check(window.Settings.PositionLocked && window.LastRecallPosition is not null,
            "explicit recall works while keeping the position lock enabled");
        window.TogglePositionLock();
        window.SetCharacter(PetCharacter.Cat);
        window.RecallPet();
        window.RoamDesktop();
        check(!window.CloudActive && window.LastRecallPosition is not null, "cat supports recall without gaining unsupported cloud movement");
        window.SetCharacter(PetCharacter.Tianyi);
        Record(window, directory, check);
        RecordClose(window, directory, check);
    }
    private static void Record(MainWindow window, string directory, Action<bool, string> check)
    {
        window.WakeCharacterImmediately();
        var bounds = NativePlacement.RoamingBounds(window);
        var cardSize = NativePlacement.RoamingBounds(window.QuotaHost).Size;
        window.QuotaHost.MoveTo(new(bounds.Area.Left + 24, bounds.Area.Bottom - cardSize.Height - 24));
        window.RoamDesktop();
        var route = window.CloudRoute!;
        var encoder = new GifBitmapEncoder();
        var delays = new List<int>();
        window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(1200));
        var stride = route.TravelMs / 40;
        for (var i = 0; i <= 40; i++)
        {
            var position = new Point(window.CloudRequestedX, window.CloudRequestedY);
            var preview = WindowPreview.CaptureRoaming(window, position, route.Area);
            encoder.Frames.Add(BitmapFrame.Create(preview));
            delays.Add(15); // Compressed review of a long trip; runtime duration is recorded separately.
            window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(stride));
        }
        using var bytes = new MemoryStream();
        encoder.Save(bytes);
        File.WriteAllBytes(Path.Combine(directory, "desktop-roaming.gif"), ThroneMotionVerification.WithAnimationMetadata(bytes.ToArray(), delays));
        File.WriteAllText(Path.Combine(directory, "desktop-roaming-plan.txt"),
            $"Work area: {route.Area}\nPet physical size: {route.PetSize}\nFrom: {route.Start}\nTo: {route.Target}\nTravel: {route.TravelMs:F0} ms\nPreview speed is compressed.\n");
        check(encoder.Frames.Count == 41, "desktop preview captures the real pet and quota WPF surfaces over the chosen route");
        window.WakeCharacterImmediately();
    }
    private static void RecordClose(MainWindow window, string directory, Action<bool, string> check)
    {
        window.SetCharacterScale(1.5);
        window.WakeCharacterImmediately();
        window.SummonCloud();
        var origin = window.CloudRequestedX;
        var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(window).DpiScaleX;
        var quota = window.QuotaHost.Position;
        var encoder = new GifBitmapEncoder(); var delays = new List<int>();
        for (var i = 0; i < 99; i++)
        {
            var image = WindowPreview.Capture(window, 144, (window.CloudRequestedX - origin) / dpi, 820, window.Height + 12);
            encoder.Frames.Add(BitmapFrame.Create(image)); delays.Add(10);
            if (i == 32)
            {
                var png = new PngBitmapEncoder(); png.Frames.Add(BitmapFrame.Create(image));
                using var file = File.Create(Path.Combine(directory, "cloud-motion.png")); png.Save(file);
            }
            window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(100));
        }
        using var bytes = new MemoryStream(); encoder.Save(bytes);
        File.WriteAllBytes(Path.Combine(directory, "cloud-motion.gif"),
            ThroneMotionVerification.WithAnimationMetadata(bytes.ToArray(), delays));
        check(!window.CloudActive && window.CloudCharacterBob.Y == 0 && window.QuotaHost.Position == quota,
            "real-time WPF cloud preview includes spell, cruise, blink and settled arrival with fixed quota");
        window.WakeCharacterImmediately(); window.SetCharacterScale(1);
    }
}
