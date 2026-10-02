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
        characterAnimation.Configure(window.SelectedCharacter);
        ResetAmbient();
        window.GreetingTilt.BeginAnimation(RotateTransform.AngleProperty, null);
        window.CharacterArt.Width = window.SelectedCharacter.Manifest.DisplayWidth;
        window.CharacterArt.Height = window.SelectedCharacter.Manifest.DisplayHeight;
        blink.Interval = TimeSpan.FromMilliseconds(window.SelectedCharacter.Manifest.BlinkIntervalMs);
        RefreshCharacterFrame();
        ApplyFloating();
        window.PetStage.ToolTip = window.SelectedCharacter.Name;
        AutomationProperties.SetName(window.PetStage, window.SelectedCharacter.Name);
    }

    private void Blink()
    {
        if (window.IsVisible && !verification && window.CanPlayAmbient) StartCharacterBlink();
    }

    internal void StartCharacterBlink() { characterAnimation.Blink(); RefreshCharacterFrame(); }
    internal void StartAnimationClock() { animationClock.Restart(); frameTimer.Start(); }
    internal void AdvanceCharacterAnimation(TimeSpan elapsed)
    {
        characterAnimation.Advance(elapsed);
        RefreshCharacterFrame();
        if (window.IsVisible && !verification) AdvanceAmbient(elapsed);
    }
    internal void PointerChanged(bool inside)
    {
        if (hovered == inside) return;
        hovered = inside;
        hoverElapsed = 0;
        hoverPlayed = false;
    }
    private void ResetAmbient()
    { hovered = hoverPlayed = false; hoverElapsed = lookElapsed = 0; nextLook = NextLook(); }
    internal void AdvanceAmbient(TimeSpan elapsed)
    {
        if (!window.CanPlayAmbient || characterAnimation.Low) return;
        var ms = Math.Max(0, elapsed.TotalMilliseconds);
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
        characterAnimation.ReactToClick(Random.Shared.Next(2));
        hoverPlayed = hovered;
        lookElapsed = 0;
        nextLook = NextLook();
        RefreshCharacterFrame();
        PlayTilt();
    }
    internal void PlayGreeting()
    {
        characterAnimation.Greet();
        RefreshCharacterFrame();
        PlayTilt();
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
        characterAnimation.Low = window.Model.IsLow && !window.Model.IsStale;
        if (window.CharacterArt.Source != characterAnimation.Image) window.CharacterArt.Source = characterAnimation.Image;
    }

    private void ApplyFloating()
    {
        window.Bob.BeginAnimation(TranslateTransform.YProperty, null);
        if (window.IsVisible && !verification && window.SelectedCharacter.Manifest.Float)
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
            blink.Stop();
            frameTimer.Stop();
            animationClock.Reset();
            characterAnimation.ResetTransient();
            ResetAmbient();
            window.GreetingTilt.BeginAnimation(RotateTransform.AngleProperty, null);
            RefreshCharacterFrame();
            window.Bob.BeginAnimation(TranslateTransform.YProperty, null);
        }
    }

}
