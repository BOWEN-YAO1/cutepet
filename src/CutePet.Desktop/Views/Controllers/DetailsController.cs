using System;
using System.Windows.Threading;

namespace CutePet.Desktop;

// Owns detail popup timing and state; the host supplies preferences and drag/lifetime signals.
internal sealed class DetailsController
{
    private readonly MainWindow window;
    private readonly bool verification;
    private readonly Func<bool> isDragging, isExiting;
    private readonly DispatcherTimer openDetails = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private readonly DispatcherTimer closeDetails = new() { Interval = TimeSpan.FromMilliseconds(650) };
    private bool hovered;
    private int menusOpen;
    internal bool DetailsVisible { get; private set; }
    private Preferences Settings => window.Settings;
    private bool dragged => isDragging();
    private bool exiting => isExiting();
    public DetailsController(MainWindow window, bool verification, Func<bool> isDragging, Func<bool> isExiting)
    {
        this.window = window;
        this.verification = verification;
        this.isDragging = isDragging;
        this.isExiting = isExiting;
        openDetails.Tick += (_, _) => CompleteHoverOpen();
        closeDetails.Tick += (_, _) => CompleteHoverClose();
    }
    internal void TogglePinned() =>
        window.SetDetailsMode(Settings.Details == DetailsMode.Always ? DetailsMode.Hover : DetailsMode.Always);
    internal void Collapse()
    {
        window.SetDetailsMode(DetailsMode.Hover);
        hovered = false;
        ShowDetails(false);
    }

    internal void ResetHover() => hovered = false;

    internal void ApplyMode()
    {
        StopDetailsTimers();
        ShowDetails(Settings.Details == DetailsMode.Always || Settings.Details == DetailsMode.Hover && hovered && !dragged);
    }
    internal void PointerChanged(bool inside)
    {
        hovered = inside;
        StopDetailsTimers();
        if (Settings.Details != DetailsMode.Hover || dragged || menusOpen > 0 || exiting) return;
        if (inside && !DetailsVisible) openDetails.Start();
        else if (!inside && DetailsVisible) closeDetails.Start();
    }
    internal void CompleteHoverOpen()
    {
        openDetails.Stop();
        if (Settings.Details == DetailsMode.Hover && hovered && !dragged && menusOpen == 0 && !exiting) ShowDetails(true);
    }
    internal void CompleteHoverClose()
    {
        closeDetails.Stop();
        if (Settings.Details == DetailsMode.Hover && !hovered && menusOpen == 0) ShowDetails(false);
    }
    internal void BeginDetailsMenu() { menusOpen++; StopDetailsTimers(); }
    internal void EndDetailsMenu()
    {
        menusOpen = Math.Max(0, menusOpen - 1);
        PointerChanged(window.Scene.IsMouseOver || window.DetailsViewport.IsMouseOver);
    }
    internal void StopDetailsTimers() { openDetails.Stop(); closeDetails.Stop(); }
    internal void ShowDetails(bool visible)
    {
        DetailsVisible = visible;
        window.PinDetailsButton.Content = Settings.Details == DetailsMode.Always ? "取消固定" : "固定";
        window.DetailsPopup.IsOpen = visible && window.IsVisible && !verification;
    }
    internal void OnDetailsOpened() => NativePlacement.SetPopupTopmost(window.DetailsViewport, window.Topmost);
    internal void RepositionDetails()
    {
        if (!window.DetailsPopup.IsOpen) return;
        window.DetailsPopup.IsOpen = false;
        window.DetailsPopup.IsOpen = true;
    }

}
