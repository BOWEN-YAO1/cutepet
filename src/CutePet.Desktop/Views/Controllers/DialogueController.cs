using System;
using System.ComponentModel;
using System.Windows.Controls.Primitives;
using System.Windows;

namespace CutePet.Desktop;

internal sealed class DialogueController : IDisposable
{
    private readonly MainWindow window;
    private readonly bool verification;
    private readonly CharacterDialogue dialogue = new();
    private bool stale, low;
    private Point? popupAnchor;
    internal DialogueController(MainWindow window, bool verification)
    {
        this.window = window; this.verification = verification;
        window.Model.PropertyChanged += ModelChanged;
        window.SpeechPopup.Opened += PopupOpened;
    }
    internal string Context => window.ScreenEdgeAttachment?.Side switch
    {
        ScreenEdge.Top => "edge-top", ScreenEdge.Bottom => "edge-bottom", ScreenEdge.Left or ScreenEdge.Right => "edge-side",
        _ => window.CharacterRestPose ? "rest" : window.CloudActive ? "cloud" : "ambient"
    };
    internal void Configure()
    {
        dialogue.Configure(window.SelectedCharacter.Manifest.Dialogue);
        UpdatePreferences(); Publish();
    }
    internal void UpdatePreferences()
    { dialogue.Frequency = window.Settings.SpeechFrequency; dialogue.Proactive = window.Settings.ProactiveSpeech; }
    internal void Startup(int hour) => Speak(hour < 11 && hour >= 5 ? "morning" : hour >= 19 || hour < 5 ? "evening" : "startup");
    internal void Speak(string context, bool proactive = false, bool important = false)
    {
        if (!window.IsVisible) return;
        if (dialogue.Speak(context, proactive, important)) Publish();
    }
    internal void Advance(TimeSpan elapsed)
    {
        if (!window.IsVisible) { Clear(); return; }
        var allow = window.CanPlayAmbient && !window.DetailsVisible && window.ControlCenter?.IsCharactersPage != true;
        dialogue.Advance(elapsed, allow);
        if (allow) dialogue.Speak(Context, proactive: true);
        Publish();
    }
    private void ModelChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Apply/Failure finish by publishing CharacterMessage; ignore intermediate refresh notifications.
        if (e.PropertyName != nameof(PetViewModel.CharacterMessage)) return;
        var model = window.Model;
        if (model.IsStale && !stale) Speak("offline", important: true);
        else if (!model.IsStale && model.HasData)
        {
            if (model.IsLow && !low) Speak("low", important: true);
            else if (stale || low && !model.IsLow) Speak("recovery", important: true);
            low = model.IsLow;
        }
        stale = model.IsStale;
    }
    private void Publish()
    {
        window.Model.SpeechText = dialogue.Text;
        window.Model.SpeechVisible = dialogue.Visible;
        window.SpeechPopup.Placement = window.ScreenEdgeAttachment?.Side switch
        {
            ScreenEdge.Top => PlacementMode.Bottom, ScreenEdge.Left => PlacementMode.Right,
            ScreenEdge.Right => PlacementMode.Left, _ => PlacementMode.Top
        };
        // WPF adjusts popups at monitor boundaries. The popup is anchored to the moving pet, never to quota.
        window.SpeechPopup.IsOpen = !verification && window.IsVisible && dialogue.Visible;
        if (window.SpeechPopup.IsOpen && PresentationSource.FromVisual(window.PetStage) is not null)
        {
            var anchor = window.PetStage.PointToScreen(new Point());
            if (popupAnchor == anchor) return;
            popupAnchor = anchor;
            // Refresh the native popup position when the separate pet HWND moves during a cloud trip.
            var offset = window.SpeechPopup.HorizontalOffset;
            window.SpeechPopup.HorizontalOffset = offset + .01;
            window.SpeechPopup.HorizontalOffset = offset;
        }
        else popupAnchor = null;
    }
    internal void Clear() { dialogue.Clear(); Publish(); }
    private void PopupOpened(object? sender, EventArgs e) => NativePlacement.SetPopupTopmost(window.SpeechBubble, window.Topmost);
    public void Dispose() { Clear(); window.Model.PropertyChanged -= ModelChanged; window.SpeechPopup.Opened -= PopupOpened; }
}
