using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CutePet.Desktop;

// A separate input and placement surface sharing the pet's one quota model and session.
public partial class QuotaWindow : Window
{
    private readonly MainWindow host;
    private readonly bool verification;
    private bool closing, dragged;
    private Point? mouseStart;
    private Point grab, original;
    internal bool Dragging { get; private set; }
    internal Point RequestedPosition { get; private set; }
    internal Point Position => verification || !IsLoaded ? RequestedPosition : PhysicalPosition();
    internal Point PhysicalPosition() { var p = NativePlacement.Get(this); return new(p.Left, p.Top); }
    public static readonly DependencyProperty CompactColumnsProperty = DependencyProperty.Register(
        nameof(CompactColumns), typeof(int), typeof(QuotaWindow), new PropertyMetadata(1));
    public int CompactColumns { get => (int)GetValue(CompactColumnsProperty); private set => SetValue(CompactColumnsProperty, value); }

    internal QuotaWindow(MainWindow host, bool verification)
    {
        this.host = host;
        this.verification = verification;
        InitializeComponent();
        Icon = AppIcon.WindowIcon;
        if (verification) { Opacity = 0; Left = Top = -10000; }
        DataContext = host.Model;
        DetailsViewport.DataContext = host.Model;
        Topmost = host.Settings.AlwaysOnTop;
        QuotaScene.ContextMenu = DesktopMenuBuilder.Create(host);
        DetailsScene.ContextMenu = DesktopMenuBuilder.Create(host);
        DpiChanged += (_, _) => Dispatcher.BeginInvoke(() =>
        { if (IsLoaded && !Dragging) { MoveTo(Position); host.SavePlacement(); host.RepositionDetails(); } });
    }
    internal void ResizeCard(QuotaDock dock, double scale)
    {
        var horizontal = dock is QuotaDock.Top or QuotaDock.Bottom;
        QuotaSurface.Width = horizontal ? 224 : 116;
        QuotaSurface.Height = horizontal ? 40 : 68;
        CompactColumns = horizontal ? 2 : 1;
        Width = QuotaScene.Width = QuotaCard.Width = QuotaSurface.Width * scale;
        Height = QuotaScene.Height = QuotaCard.Height = QuotaSurface.Height * scale;
        UpdateLayout();
        if (IsLoaded) MoveTo(Position);
    }
    private void OnOpenCenter(object sender, RoutedEventArgs e) => host.OpenControlCenter();
    internal void MoveTo(Point point)
    {
        RequestedPosition = point;
        if (!verification && IsLoaded)
        { NativePlacement.Apply(this, point.X, point.Y); RequestedPosition = PhysicalPosition(); }
        host.RepositionDetails();
    }
    internal void ShowAt(Point point)
    {
        RequestedPosition = point;
        if (verification)
        {
            if (!IsVisible) Show();
            NativePlacement.MoveUnclamped(this, -10000, -10000);
            return;
        }
        if (!IsVisible)
        {
            // Set the initial location before Show so no default-location flash is visible.
            var dpi = VisualTreeHelper.GetDpi(this);
            Left = point.X / dpi.DpiScaleX; Top = point.Y / dpi.DpiScaleY;
            Show();
        }
        MoveTo(point);
    }
    internal void BeginDrag(Point offset)
    {
        if (Dragging || !host.CanDrag) return;
        host.CancelCloud();
        host.StopDetailsTimers(); host.ShowDetails(false);
        original = Position;
        var dpi = VisualTreeHelper.GetDpi(this);
        grab = new(offset.X * dpi.DpiScaleX, offset.Y * dpi.DpiScaleY);
        Dragging = dragged = true;
        if (!verification) { QuotaScene.Focus(); if (!QuotaScene.CaptureMouse()) EndDrag(true); }
    }
    // Pointer coordinates are physical desktop pixels; each window may have a different DPI.
    internal void UpdateDrag(Point pointer)
    { if (Dragging) MoveTo(new(pointer.X - grab.X, pointer.Y - grab.Y)); }
    internal void EndDrag(bool cancel)
    {
        if (!Dragging) return;
        Dragging = false;
        if (QuotaScene.IsMouseCaptured) QuotaScene.ReleaseMouseCapture();
        if (cancel) MoveTo(original);
        mouseStart = null;
        host.SavePlacement();
        if (host.Settings.Details == DetailsMode.Always) host.ShowDetails(true);
        else host.PointerChanged(QuotaScene.IsMouseOver || DetailsViewport.IsMouseOver);
    }
    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    { mouseStart = e.GetPosition(QuotaScene); dragged = false; }
    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) { if (Dragging) EndDrag(false); return; }
        if (!Dragging && mouseStart is Point start && (e.GetPosition(QuotaScene) - start).Length >= 4) BeginDrag(start);
        if (Dragging) { UpdateDrag(QuotaScene.PointToScreen(e.GetPosition(QuotaScene))); e.Handled = true; }
    }
    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    { if (Dragging) EndDrag(false); mouseStart = null; if (dragged) e.Handled = true; }
    private void OnCaptureLost(object sender, MouseEventArgs e) { if (Dragging) EndDrag(true); }
    private void OnKeyDown(object sender, KeyEventArgs e)
    { if (e.Key == Key.Escape && Dragging) { EndDrag(true); e.Handled = true; } }
    private void OnHoverEnter(object sender, MouseEventArgs e) => host.PointerChanged(true);
    private void OnHoverLeave(object sender, MouseEventArgs e) => host.PointerChanged(QuotaScene.IsMouseOver || DetailsViewport.IsMouseOver);
    private void OnDetailsOpened(object? sender, EventArgs e) => NativePlacement.SetPopupTopmost(DetailsViewport, Topmost);
    private void OnRefresh(object sender, RoutedEventArgs e) => host.RefreshQuota();
    private void OnPinDetails(object sender, RoutedEventArgs e) => host.SetDetailsMode(host.Settings.Details == DetailsMode.Always ? DetailsMode.Hover : DetailsMode.Always);
    private void OnCollapseDetails(object sender, RoutedEventArgs e) => host.CollapseDetails();
    internal void CloseForExit() { closing = true; Close(); }
    protected override void OnClosing(CancelEventArgs e)
    { if (!closing) { e.Cancel = true; host.RequestExit(); } base.OnClosing(e); }
}
