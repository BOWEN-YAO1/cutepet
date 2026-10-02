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
        if (window.IsVisible && !verification) StartCharacterBlink();
    }

    internal void StartCharacterBlink() { characterAnimation.Blink(); RefreshCharacterFrame(); }
    internal void StartAnimationClock() { animationClock.Restart(); frameTimer.Start(); }
    internal void AdvanceCharacterAnimation(TimeSpan elapsed) { characterAnimation.Advance(elapsed); RefreshCharacterFrame(); }
    internal void PlayGreeting()
    {
        characterAnimation.Greet();
        RefreshCharacterFrame();
        if (verification || !window.IsVisible || !window.SelectedCharacter.Actions.ContainsKey("greeting")) return;
        var tilt = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromMilliseconds(1080), FillBehavior = FillBehavior.Stop };
        tilt.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        tilt.KeyFrames.Add(new LinearDoubleKeyFrame(-3, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(180))));
        tilt.KeyFrames.Add(new LinearDoubleKeyFrame(3, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(540))));
        tilt.KeyFrames.Add(new LinearDoubleKeyFrame(0, KeyTime.FromTimeSpan(TimeSpan.FromMilliseconds(1080))));
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
            window.GreetingTilt.BeginAnimation(RotateTransform.AngleProperty, null);
            RefreshCharacterFrame();
            window.Bob.BeginAnimation(TranslateTransform.YProperty, null);
        }
    }

}
