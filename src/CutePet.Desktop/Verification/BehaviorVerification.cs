using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace CutePet.Desktop;

internal static class BehaviorVerification
{
    internal static async Task RunAsync(string directory, Action<bool, string> check)
    {
        var random = new Random(3741);
        var rhythm = new BehaviorRhythm(random.NextDouble);
        var delays = new HashSet<double>();
        var history = new List<QuietActivity>();
        var bounded = true;
        for (var i = 0; i < 40; i++)
        {
            rhythm.Plan(true,true,30000); var next = rhythm.Next; var delay = rhythm.RemainingMs;
            bounded &= next == QuietActivity.Rest ? delay >= 25500 && delay <= 37500 : delay >= 25000 && delay <= 50000;
            history.Add(next); delays.Add(delay);
            rhythm.Plan(true,true,30000);
            if (rhythm.Next != next || rhythm.RemainingMs != delay) throw new InvalidOperationException("Replanning rerolled an existing quiet deadline.");
            rhythm.Started(next);
        }
        check(bounded, "automatic activity quiet durations remain bounded across forty varied plans");
        check(delays.Count == 40 && history.Zip(history.Skip(1)).All(pair => pair.First != pair.Second),
            "substantial activities vary their deadlines and alternate while both options are enabled");
        rhythm.Plan(false,true,30000);
        check(rhythm.Next == QuietActivity.Cloud, "one enabled activity remains usable even when it was last used");
        rhythm.Plan(true,false,30000);
        check(rhythm.Next == QuietActivity.Rest && rhythm.RemainingMs >= 25500, "disabling a pending activity schedules the other with a fresh quiet delay");
        rhythm.Plan(false,false,30000);
        check(rhythm.Next == QuietActivity.None && rhythm.Advance(TimeSpan.FromHours(1)) == QuietActivity.None,
            "both automatic switches off leaves the activity planner idle");
        var names = new[] {"a","b","c"};
        var sequence = Enumerable.Range(0,30).Select(_ => rhythm.Choose("edge",names)).ToArray();
        check(sequence.Zip(sequence.Skip(1)).All(pair => pair.First != pair.Second)
            && Enumerable.Range(0,10).All(i => sequence.Skip(i*3).Take(3).Distinct().Count() == 3),
            "shuffled gestures cover every available response and avoid adjacent repetitions across rounds");
        check(sequence.Take(3).SequenceEqual(sequence.Skip(3).Take(3)) == false,
            "seeded production shuffle can change its ordering between rounds");
        check(rhythm.Choose("single",new[] {"only"}) == "only" && rhythm.Choose("single",new[] {"only"}) == "only",
            "custom characters with only one optional gesture keep that gesture");
        check(rhythm.Choose("edge",new[] {"replacement"}) == "replacement", "changing available clips removes obsolete queued gestures");
        var floating = new IdleFloat(); floating.SetEnabled(true); floating.Advance(TimeSpan.FromSeconds(1));
        var before = floating.Value; floating.SetEnabled(false);
        check(before == floating.Value && before < 0, "disabling standing float retains its current pose at the boundary");
        floating.Advance(TimeSpan.FromMilliseconds(1));
        check(Math.Abs(floating.Value-before) < .02, "standing float begins its transition without a position jump");
        floating.Advance(TimeSpan.FromMilliseconds(319));
        check(floating.Value == 0, "standing float settles fully before another pose takes over");
        floating.SetEnabled(true);
        check(floating.Value == 0, "resuming standing float starts with zero amplitude rather than an old offset");
        var floatingSamples = Enumerable.Range(0,200).Select(_ => { floating.Advance(TimeSpan.FromMilliseconds(40)); return floating.Value; }).ToArray();
        check(floatingSamples.All(value => value >= -4 && value <= 0), "continuous standing float keeps the original bounded height");
        floating.Reset(); check(floating.Value == 0, "lifetime reset clears the standing float phase and amplitude");
        var landing = new CloudLanding(); var pose = new CloudPose(1,-6,1.2,.8,1,.5,.02);
        landing.Begin(pose); check(landing.Pose == pose, "landing preserves every displayed channel at its start");
        landing.Advance(TimeSpan.FromMilliseconds(160));
        check(landing.Active && Math.Abs(landing.Pose.Opacity-.5) < 1e-9 && Math.Abs(landing.Pose.Lift+3)<1e-9,
            "cloud and figure settle together through the midpoint");
        landing.Advance(TimeSpan.FromSeconds(10));
        check(!landing.Active && landing.Pose == default, "a delayed frame completes landing without leaving stale transforms");

        var window = new MainWindow(new PreferencesStore(Path.Combine(directory,"behavior-tests")), verification:true);
        try
        {
            window.Show(); await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            window.SetCharacter(PetCharacter.Tianyi); window.SetDetailsMode(DetailsMode.Hidden);
            window.SetProactiveSpeech(false);
            var planned = window.NextActivity; var wait = window.NextActivityDelay;
            check(planned == QuietActivity.Rest, "deterministic verification exercises the same first activity planning path");
            window.AdvanceAmbient(TimeSpan.FromMilliseconds(wait-1)); window.AdvanceCharacterAnimation(TimeSpan.FromSeconds(2));
            var remaining = window.NextActivityDelay;
            window.BeginDetailsMenu(); window.AdvanceAmbient(TimeSpan.FromMinutes(1)); window.EndDetailsMenu();
            check(window.NextActivityDelay == remaining && !window.CloudActive && !window.CharacterResting,
                "open menus pause the unified quiet clock without queuing an activity");
            window.SetDetailsMode(DetailsMode.Always); window.AdvanceAmbient(TimeSpan.FromMinutes(1));
            check(window.NextActivityDelay == remaining, "pinned quota details also pause substantial activity");
            window.SetDetailsMode(DetailsMode.Hidden); window.AdvanceAmbient(TimeSpan.FromMilliseconds(remaining));
            check(window.CharacterResting && !window.CloudActive, "due automatic rest takes precedence without a competing cloud timer");
            window.AdvanceCharacterAnimation(TimeSpan.FromSeconds(2)); window.AdvanceAmbient(TimeSpan.FromSeconds(20));
            check(window.CurrentCharacterFrame == CharacterFrame.Rise, "automatic rest has a finite varied seated duration");
            window.AdvanceCharacterAnimation(TimeSpan.FromSeconds(2));
            check(window.NextActivity == QuietActivity.Cloud && window.NextActivityDelay >= 25000,
                "after automatic rest the next activity is cloud after a full quiet interval");
            window.AdvanceAmbient(TimeSpan.FromMilliseconds(window.NextActivityDelay));
            check(window.CloudActive && !window.CharacterResting, "automatic cloud starts once the shared plan becomes due");
            window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(3200));
            var x = window.CloudRequestedX; var y = window.CloudRequestedY; var quota = window.QuotaHost.Position;
            var lift = window.CloudLift.Y; var opacity = window.CloudArt.Opacity;
            window.PlayCharacterInteraction();
            check(!window.CloudActive && window.CloudLanding && window.CloudLift.Y == lift && window.CloudArt.Opacity == opacity,
                "click stops travel and responds immediately while preserving the starting landing pose");
            window.SummonCloud(); check(!window.CloudActive, "new departure cannot overwrite a still settling cloud");
            CaptureLanding(window,directory);
            check(!window.CloudLanding && window.CloudArt.Opacity == 0 && window.CloudLift.Y == 0
                && window.CloudLean.Angle == 0 && window.CloudCharacterBob.Y == 0,
                "actual WPF landing clears all visual channels after its finite transition");
            check(window.CloudRequestedX == x && window.CloudRequestedY == y && window.QuotaHost.Position == quota,
                "soft landing moves neither the stopped character route nor the independent quota");
            window.AdvanceCharacterAnimation(TimeSpan.FromSeconds(2));
            check(window.NextActivityDelay >= 25500, "landing and reply completion leave a fresh quiet interval before the next activity");
            window.WakeCharacterImmediately(); window.SummonCloud(); window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(1200));
            window.ToggleCharacterRest();
            check(window.CharacterResting && window.CloudLanding && !window.CloudActive, "manual throne begins while the previous cloud softly withdraws");
            window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(320));
            check(window.CharacterResting && !window.CloudLanding && window.CloudLift.Y == 0, "soft landing cannot cancel an already started throne sequence");
            window.AdvanceCharacterAnimation(TimeSpan.FromSeconds(2));
            var seated = new List<CharacterFrame>();
            for(var i=0;i<3;i++)
            { window.AdvanceAmbient(TimeSpan.FromSeconds(25)); seated.Add(window.CurrentCharacterFrame); window.AdvanceCharacterAnimation(TimeSpan.FromSeconds(2)); }
            check(seated.Distinct().Count()==3 && seated.Contains(CharacterFrame.SeatedBlink)
                && seated.Contains(CharacterFrame.SeatedWave) && seated.Contains(CharacterFrame.SeatedHappy),
                "settled sitting varies smiles, small waves and blinks without standing up");
            window.WakeCharacterImmediately();
            check(window.CompletePetDrag(new Rect(-1920,0,1920,1040),new Size(230,178),new Point(-900,2),1),
                "behavior verification attaches at the upper edge without native desktop movement");
            window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(320));
            var gestures = new List<string?>();
            for(var i=0;i<6;i++)
            {
                window.PeekScreenEdge(proactive:true); var action=window.ScreenEdgeResponse!; gestures.Add(action);
                window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(window.SelectedCharacter.Actions[action].Duration+40));
            }
            check(gestures.Zip(gestures.Skip(1)).All(pair=>pair.First!=pair.Second)
                &&gestures.Take(3).Distinct().Count()==3, "automatic upper-edge gestures use a balanced nonrepeating bag");
            window.WakeCharacterImmediately(); window.SummonCloud(); window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(1200));
            window.PlayCharacterInteraction(); window.TogglePositionLock();
            check(!window.CloudLanding && window.CloudArt.Opacity==0 && window.CloudLift.Y==0,
                "position lock clears settling transforms immediately for dependable placement");
            window.TogglePositionLock(); window.WakeCharacterImmediately(); window.SummonCloud();
            window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(1200)); window.PlayCharacterInteraction();
            window.SetCharacter(PetCharacter.Cat);
            check(!window.CloudLanding && window.CloudArt.Source is null && window.CloudLift.Y==0,
                "switching to the original cat clears a previous character's pending landing");
        }
        finally { await window.StopAsync(); window.Close(); }
    }
    private static void CaptureLanding(MainWindow window, string directory)
    {
        var encoder = new GifBitmapEncoder(); var delays = new List<int>();
        var background = window.Scene.Background; window.Scene.Background = new SolidColorBrush(Color.FromRgb(239,245,244));
        try
        {
            for(var i=0;i<9;i++)
            {
                var frame = WindowPreview.Capture(window,144);
                encoder.Frames.Add(BitmapFrame.Create(frame)); delays.Add(i==0 || i==8 ? 70 : 4);
                if(i<8) window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(40));
            }
            using var buffer = new MemoryStream(); encoder.Save(buffer);
            File.WriteAllBytes(Path.Combine(directory,"cloud-landing.gif"),ThroneMotionVerification.WithAnimationMetadata(buffer.ToArray(),delays));
        }
        finally { window.Scene.Background = background; }
    }
}
