using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Automation;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace CutePet.Desktop;

// Owns the WPF presentation clock; CharacterAnimation remains the package-driven player.
internal sealed class CharacterPresenter
{
    private readonly MainWindow window;
    private readonly bool verification;
    private readonly DispatcherTimer blink = new() { Interval = TimeSpan.FromSeconds(4) };
    private readonly Stopwatch animationClock = new();
    private bool rendering;
    private readonly CharacterAnimation characterAnimation = new();
    private readonly FrameInterpolator interpolator = new();
    private readonly BehaviorRhythm rhythm;
    private readonly IdleFloat idleFloat = new();
    private bool hovered, hoverPlayed;
    private double hoverElapsed, lookElapsed, nextLook;
    private double seatedElapsed, restDuration, cloudBlinkElapsed, nextCloudBlink;
    private bool automaticRest, floating;
    internal bool Resting => characterAnimation.Resting;
    internal bool RestPose => characterAnimation.RestPose;
    internal bool Idle => characterAnimation.Action == "idle";
    private double NextLook() => window.ScreenEdgeActive ? rhythm.Between(12000,26000)
        : RestPose ? rhythm.Between(10000,22000) : rhythm.Between(8000,16000);
    private void PlanActivity() => rhythm.Plan(window.Settings.AutoRest && window.SelectedCharacter.Manifest.RestAfterMs > 0,
        window.Settings.AutoCloud && !window.Settings.PositionLocked && window.SelectedCharacter.CloudImage is not null,
        window.SelectedCharacter.Manifest.RestAfterMs);
    internal double NextActivityDelay { get { PlanActivity(); return rhythm.RemainingMs; } }
    internal QuietActivity NextActivity { get { PlanActivity(); return rhythm.Next; } }
    internal string ChooseAmbient(string group, IEnumerable<string> actions) => rhythm.Choose(group, actions);
    internal void ActivityStarted(QuietActivity activity) { rhythm.Started(activity); lookElapsed = 0; nextLook = NextLook(); }
    internal void ActivityFinished() { rhythm.ResetQuiet(); lookElapsed = 0; nextLook = NextLook(); }
    internal CharacterFrame CurrentFrame => characterAnimation.Frame;
    internal LoadedFrame SpriteFrame => characterAnimation.SpriteFrame;
    public CharacterPresenter(MainWindow window, bool verification)
    {
        this.window = window;
        this.verification = verification;
        rhythm = new BehaviorRhythm(verification ? () => 0 : null);
        nextLook = rhythm.Between(8000,16000);
        blink.Tick += (_, _) => Blink();
    }

    private void OnRendering(object? sender, EventArgs args)
    {
        var elapsed = animationClock.Elapsed;
        animationClock.Restart();
        AdvanceCharacterAnimation(elapsed);
    }
    internal void ApplyPack()
    {
        window.CancelScreenEdge();
        window.CancelCloud();
        characterAnimation.Configure(window.SelectedCharacter);
        interpolator.Reset();
        rhythm.Reset(); idleFloat.Reset(); floating = false; window.Bob.Y = 0;
        ResetAmbient();
        window.GreetingTilt.BeginAnimation(RotateTransform.AngleProperty, null);
        window.CharacterArt.Width = window.SelectedCharacter.Manifest.DisplayWidth;
        window.CharacterArt.Height = window.SelectedCharacter.Manifest.DisplayHeight;
        window.CloudArt.Source = window.SelectedCharacter.CloudImage;
        window.CloudArt.Width = window.SelectedCharacter.Manifest.Cloud?.DisplayWidth ?? 140;
        window.CloudArt.Height = window.SelectedCharacter.Manifest.Cloud?.DisplayHeight ?? 32;
        blink.Interval = TimeSpan.FromMilliseconds(window.SelectedCharacter.Manifest.BlinkIntervalMs);
        RefreshCharacterFrame();
        ApplyFloating();
        window.PetStage.ToolTip = window.SelectedCharacter.Name;
        AutomationProperties.SetName(window.PetStage, window.SelectedCharacter.Name);
    }

    private void Blink()
    {
        if (window.IsVisible && !verification && window.CanPlayAmbient && !window.CloudActive) StartCharacterBlink();
        blink.Interval = TimeSpan.FromMilliseconds(rhythm.Between(window.SelectedCharacter.Manifest.BlinkIntervalMs*.8,
            window.SelectedCharacter.Manifest.BlinkIntervalMs*1.4));
    }

    internal void StartCharacterBlink() { if (!window.CloudActive) characterAnimation.Blink(); RefreshCharacterFrame(); }
    internal void StartAnimationClock()
    {
        if (rendering) return;
        animationClock.Restart();
        CompositionTarget.Rendering += OnRendering;
        rendering = true;
    }
    internal void AdvanceCharacterAnimation(TimeSpan elapsed)
    {
        if ((!window.CloudActive || window.CanCloudMove) && (!window.ScreenEdgeActive || window.CanPlayAmbient)) characterAnimation.Advance(elapsed);
        window.AdvanceScreenEdge(elapsed);
        window.AdvanceCloud(elapsed);
        if (!window.CloudActive) cloudBlinkElapsed = 0;
        else if (window.CanCloudMove && window.CloudArt.Opacity == 1)
        {
            cloudBlinkElapsed += Math.Max(0, elapsed.TotalMilliseconds);
            if (cloudBlinkElapsed >= nextCloudBlink && characterAnimation.Action is "idle" or "cloud-idle")
            { characterAnimation.Blink(); cloudBlinkElapsed = 0; nextCloudBlink = rhythm.Between(4200,7000); }
        }
        RefreshCharacterFrame();
        idleFloat.Advance(elapsed);
        if (!verification) window.Bob.Y = idleFloat.Value;
        if (window.IsVisible && !verification) AdvanceAmbient(elapsed);
        window.Dialogue.Advance(elapsed);
    }
    internal void PointerChanged(bool inside)
    {
        if (hovered == inside) return;
        hovered = inside;
        hoverElapsed = 0;
        hoverPlayed = false;
        rhythm.ResetQuiet();
    }
    private void ResetAmbient()
    { hovered = hoverPlayed = automaticRest = false; hoverElapsed = lookElapsed = seatedElapsed = 0; rhythm.ResetQuiet(); nextLook = NextLook(); }
    internal void AdvanceAmbient(TimeSpan elapsed)
    {
        var ms = Math.Max(0, elapsed.TotalMilliseconds);
        if (window.CloudActive || window.CloudLanding) return;
        if (!window.CanPlayAmbient || characterAnimation.Low || window.DetailsVisible || window.ControlCenter?.IsCharactersPage == true) return;
        if (window.ScreenEdgeActive)
        {
            if (hovered && !hoverPlayed)
            {
                hoverElapsed += ms;
                if (hoverElapsed >= 400) { window.PeekScreenEdge(proactive: false); hoverPlayed = true; }
            }
            if (!hovered)
            {
                lookElapsed += ms;
                if (lookElapsed >= nextLook) { window.PeekScreenEdge(proactive: true); lookElapsed = 0; nextLook = NextLook(); }
            }
            return;
        }
        if (characterAnimation.RestPose)
        {
            if (automaticRest && characterAnimation.Action == "sit")
            {
                seatedElapsed += ms;
                if (seatedElapsed >= restDuration)
                { characterAnimation.StandUp(); window.Speak("wake", proactive: true); automaticRest = false; seatedElapsed = 0; ActivityFinished(); }
            }
            if (!hovered && characterAnimation.Action == "sit")
            {
                lookElapsed += ms;
                var choices = new[] { "sit-happy", "sit-greeting", "sit-blink" }.Where(window.SelectedCharacter.Actions.ContainsKey).ToArray();
                if (lookElapsed >= nextLook && choices.Length > 0)
                { characterAnimation.TryAmbient(rhythm.Choose("seated", choices)); lookElapsed = 0; nextLook = NextLook(); }
            }
            RefreshCharacterFrame();
            return;
        }
        if (!hovered && window.CanCloudMove && characterAnimation.Action == "idle")
        {
            PlanActivity();
            var next = rhythm.Advance(elapsed);
            if (next == QuietActivity.Rest && characterAnimation.SitDown())
            {
                ActivityStarted(QuietActivity.Rest);
                automaticRest = true;
                restDuration = rhythm.Between(window.SelectedCharacter.Manifest.RestDurationMs*.85, window.SelectedCharacter.Manifest.RestDurationMs*1.3);
                window.Speak("rest", proactive: true);
                seatedElapsed = 0;
                RefreshCharacterFrame();
                return;
            }
            if (next == QuietActivity.Cloud && window.TryAutomaticCloud()) return;
            if (next != QuietActivity.None) rhythm.ResetQuiet();
        }
        if (hovered && !hoverPlayed)
        {
            hoverElapsed += ms;
            if (hoverElapsed >= 400) { characterAnimation.TryAmbient("hover"); window.Speak("hover"); hoverPlayed = true; }
        }
        if (!hovered)
        {
            lookElapsed += ms;
            if (lookElapsed >= nextLook && characterAnimation.TryAmbient("look"))
            { lookElapsed = 0; nextLook = NextLook(); }
        }
        RefreshCharacterFrame();
    }
    internal void PlayInteraction()
    {
        if (window.ScreenEdgeActive) { window.PeekScreenEdge(proactive: false); return; }
        window.LandCloud();
        window.Speak(characterAnimation.RestPose ? "rest" : "click");
        characterAnimation.ReactToClick(Random.Shared.Next(2));
        hoverPlayed = hovered;
        lookElapsed = 0;
        nextLook = NextLook();
        rhythm.ResetQuiet(); seatedElapsed = 0;
        if (!characterAnimation.Resting) automaticRest = false;
        RefreshCharacterFrame();
        PlayTilt();
    }
    internal void PlayGreeting()
    {
        if (window.ScreenEdgeActive) { window.PeekScreenEdge(proactive: false); return; }
        window.LandCloud();
        rhythm.ResetQuiet();
        window.Speak(characterAnimation.RestPose ? "rest" : "click");
        characterAnimation.Greet();
        RefreshCharacterFrame();
        PlayTilt();
    }
    internal void ToggleRest()
    {
        if (window.ScreenEdgeActive) WakeImmediately();
        window.LandCloud();
        ResetAmbient();
        if (characterAnimation.Resting) { characterAnimation.StandUp(); window.Speak("wake"); }
        else if (characterAnimation.SitDown()) { ActivityStarted(QuietActivity.Rest); window.Speak("rest"); }
        window.GreetingTilt.BeginAnimation(RotateTransform.AngleProperty, null);
        RefreshCharacterFrame();
    }
    internal void WakeImmediately()
    {
        window.CancelScreenEdge();
        window.CancelCloud();
        characterAnimation.Reset();
        ResetAmbient();
        window.GreetingTilt.BeginAnimation(RotateTransform.AngleProperty, null);
        RefreshCharacterFrame();
    }
    private void PlayTilt()
    {
        if (verification || !window.IsVisible || characterAnimation.Action is not ("greeting" or "happy")) return;
        var duration = window.SelectedCharacter.Actions[characterAnimation.Action].Duration;
        var tilt = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(duration), FillBehavior = FillBehavior.Stop };
        tilt.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        tilt.KeyFrames.Add(new LinearDoubleKeyFrame(-3, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(duration / 6))));
        tilt.KeyFrames.Add(new LinearDoubleKeyFrame(3, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(duration / 2))));
        tilt.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(duration))));
        window.GreetingTilt.BeginAnimation(RotateTransform.AngleProperty, tilt);

    }
    internal void RefreshCharacterFrame()
    {
        if (window.Model.IsLow && !window.Model.IsStale) rhythm.ResetQuiet();
        if (window.Model.IsLow && !window.Model.IsStale) window.CancelScreenEdge();
        if (window.Model.IsLow && !window.Model.IsStale) window.CancelCloud();
        characterAnimation.Low = window.Model.IsLow && !window.Model.IsStale;
        characterAnimation.Flying = window.CloudActive && window.CloudArt.Opacity == 1;
        var sample = characterAnimation.Presentation;
        var image = interpolator.Sample(sample.From, sample.To, sample.Fraction);
        if (window.CharacterArt.Source != image) window.CharacterArt.Source = image;
        ApplyFloating();
    }

    private void ApplyFloating()
    {
        var enabled = window.IsVisible && !verification && window.SelectedCharacter.Manifest.Float
            && !characterAnimation.RestPose && !window.CloudActive && !window.CloudLanding && !window.ScreenEdgeActive;
        if (floating == enabled) return;
        floating = enabled;
        idleFloat.SetEnabled(enabled);
    }

    internal void Animate(bool active)
    {
        if (active && !verification)
        {
            blink.Start();
            StartAnimationClock();
            ApplyFloating();
        }
        else
        {
            window.CancelScreenEdge();
            window.CancelCloud();
            blink.Stop();
            if (rendering) CompositionTarget.Rendering -= OnRendering;
            rendering = false;
            animationClock.Reset();
            characterAnimation.Reset();
            interpolator.Reset();
            ResetAmbient();
            window.GreetingTilt.BeginAnimation(RotateTransform.AngleProperty, null);
            RefreshCharacterFrame();
            window.Bob.BeginAnimation(TranslateTransform.YProperty, null);
            idleFloat.Reset(); window.Bob.Y = 0;
            floating = false;
        }
    }

    internal void StartCloudSpell()
    {
        cloudBlinkElapsed = 0;
        nextCloudBlink = rhythm.Between(4200,7000);
        characterAnimation.Preview("summon-cloud");
        window.GreetingTilt.BeginAnimation(RotateTransform.AngleProperty, null);
        RefreshCharacterFrame();
    }
    internal void CancelCloudSpell()
    { characterAnimation.Flying = false; if (characterAnimation.Action == "summon-cloud") characterAnimation.ResetTransient(); }
    internal void AttachEdge(string baseAction)
    {
        characterAnimation.AttachEdge(baseAction);
        ResetAmbient();
        window.GreetingTilt.BeginAnimation(RotateTransform.AngleProperty, null);
        RefreshCharacterFrame();
    }
    internal void PeekEdge(string? action = null) { characterAnimation.PeekEdge(action); RefreshCharacterFrame(); }

}
