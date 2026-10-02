using System;
using System.Diagnostics;
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
    private readonly DispatcherTimer frameTimer = new() { Interval = TimeSpan.FromMilliseconds(40) };
    private readonly Stopwatch animationClock = new();
    private readonly CharacterAnimation characterAnimation = new();
    private bool hovered, hoverPlayed;
    private double hoverElapsed, lookElapsed, nextLook = NextLook();
    private double idleForRest, seatedElapsed;
    private bool automaticRest, floating;
    internal bool Resting => characterAnimation.Resting;
    internal bool RestPose => characterAnimation.RestPose;
    internal bool Idle => characterAnimation.Action == "idle";
    private static double NextLook() => Random.Shared.Next(8000, 16001);
    internal CharacterFrame CurrentFrame => characterAnimation.Frame;
    public CharacterPresenter(MainWindow window, bool verification)
    {
        this.window = window;
        this.verification = verification;
        blink.Tick += (_, _) => Blink();
        frameTimer.Tick += (_, _) =>
        {
            var elapsed = animationClock.Elapsed;
            animationClock.Restart();
            AdvanceCharacterAnimation(elapsed);
        };
    }
    internal void ApplyPack()
    {
        window.CancelScreenEdge();
        window.CancelCloud();
        characterAnimation.Configure(window.SelectedCharacter);
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
    }

    internal void StartCharacterBlink() { if (!window.CloudActive) characterAnimation.Blink(); RefreshCharacterFrame(); }
    internal void StartAnimationClock() { animationClock.Restart(); frameTimer.Start(); }
    internal void AdvanceCharacterAnimation(TimeSpan elapsed)
    {
        if ((!window.CloudActive || window.CanCloudMove) && (!window.ScreenEdgeActive || window.CanPlayAmbient)) characterAnimation.Advance(elapsed);
        window.AdvanceScreenEdge(elapsed);
        window.AdvanceCloud(elapsed);
        RefreshCharacterFrame();
        if (window.IsVisible && !verification) AdvanceAmbient(elapsed);
    }
    internal void PointerChanged(bool inside)
    {
        if (hovered == inside) return;
        hovered = inside;
        hoverElapsed = 0;
        hoverPlayed = false;
        idleForRest = 0;
    }
    private void ResetAmbient()
    { hovered = hoverPlayed = automaticRest = false; hoverElapsed = lookElapsed = idleForRest = seatedElapsed = 0; nextLook = NextLook(); }
    internal void AdvanceAmbient(TimeSpan elapsed)
    {
        var ms = Math.Max(0, elapsed.TotalMilliseconds);
        if (window.CloudActive) return;
        if (!window.CanPlayAmbient || characterAnimation.Low) { idleForRest = 0; return; }
        if (window.ScreenEdgeActive)
        {
            if (hovered && !hoverPlayed)
            {
                hoverElapsed += ms;
                if (hoverElapsed >= 400) { window.PeekScreenEdge(); hoverPlayed = true; }
            }
            if (!hovered)
            {
                lookElapsed += ms;
                if (lookElapsed >= nextLook) { window.PeekScreenEdge(); lookElapsed = 0; nextLook = NextLook(); }
            }
            return;
        }
        if (characterAnimation.RestPose)
        {
            if (automaticRest && characterAnimation.Action == "sit")
            {
                seatedElapsed += ms;
                if (seatedElapsed >= window.SelectedCharacter.Manifest.RestDurationMs)
                { characterAnimation.StandUp(); automaticRest = false; idleForRest = seatedElapsed = 0; }
            }
            if (!hovered && characterAnimation.Action == "sit")
            {
                lookElapsed += ms;
                if (lookElapsed >= nextLook && characterAnimation.TryAmbient("sit-happy"))
                { lookElapsed = 0; nextLook = NextLook(); }
            }
            RefreshCharacterFrame();
            return;
        }
        if (!hovered && window.Settings.AutoRest && window.SelectedCharacter.Manifest.RestAfterMs > 0)
        {
            idleForRest += ms;
            if (idleForRest >= window.SelectedCharacter.Manifest.RestAfterMs && characterAnimation.Action == "idle"
                && characterAnimation.SitDown())
            {
                automaticRest = true;
                seatedElapsed = idleForRest = 0;
                RefreshCharacterFrame();
                return;
            }
        }
        if (hovered && !hoverPlayed)
        {
            hoverElapsed += ms;
            if (hoverElapsed >= 400 && characterAnimation.TryAmbient("hover")) hoverPlayed = true;
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
        if (window.ScreenEdgeActive) { window.PeekScreenEdge(); return; }
        window.CancelCloud();
        characterAnimation.ReactToClick(Random.Shared.Next(2));
        hoverPlayed = hovered;
        lookElapsed = 0;
        nextLook = NextLook();
        idleForRest = seatedElapsed = 0;
        if (!characterAnimation.Resting) automaticRest = false;
        RefreshCharacterFrame();
        PlayTilt();
    }
    internal void PlayGreeting()
    {
        if (window.ScreenEdgeActive) { window.PeekScreenEdge(); return; }
        window.CancelCloud();
        characterAnimation.Greet();
        RefreshCharacterFrame();
        PlayTilt();
    }
    internal void ToggleRest()
    {
        if (window.ScreenEdgeActive) WakeImmediately();
        window.CancelCloud();
        ResetAmbient();
        if (characterAnimation.Resting) characterAnimation.StandUp();
        else characterAnimation.SitDown();
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
        if (window.Model.IsLow && !window.Model.IsStale) window.CancelScreenEdge();
        if (window.Model.IsLow && !window.Model.IsStale) window.CancelCloud();
        characterAnimation.Low = window.Model.IsLow && !window.Model.IsStale;
        if (window.CharacterArt.Source != characterAnimation.Image) window.CharacterArt.Source = characterAnimation.Image;
        ApplyFloating();
    }

    private void ApplyFloating()
    {
        var enabled = window.IsVisible && !verification && window.SelectedCharacter.Manifest.Float
            && !characterAnimation.RestPose && !window.CloudActive && !window.ScreenEdgeActive;
        if (floating == enabled) return;
        floating = enabled;
        window.Bob.BeginAnimation(TranslateTransform.YProperty, null);
        if (enabled)
            window.Bob.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, -4, TimeSpan.FromSeconds(2.2))
            { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase() });
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
            frameTimer.Stop();
            animationClock.Reset();
            characterAnimation.Reset();
            ResetAmbient();
            window.GreetingTilt.BeginAnimation(RotateTransform.AngleProperty, null);
            RefreshCharacterFrame();
            window.Bob.BeginAnimation(TranslateTransform.YProperty, null);
            floating = false;
        }
    }

    internal void StartCloudSpell()
    {
        characterAnimation.Preview("summon-cloud");
        window.GreetingTilt.BeginAnimation(RotateTransform.AngleProperty, null);
        RefreshCharacterFrame();
    }
    internal void CancelCloudSpell()
    { if (characterAnimation.Action == "summon-cloud") characterAnimation.ResetTransient(); }
    internal void AttachEdge()
    {
        characterAnimation.AttachEdge();
        ResetAmbient();
        window.GreetingTilt.BeginAnimation(RotateTransform.AngleProperty, null);
        RefreshCharacterFrame();
    }
    internal void PeekEdge() { characterAnimation.PeekEdge(); RefreshCharacterFrame(); }

}
