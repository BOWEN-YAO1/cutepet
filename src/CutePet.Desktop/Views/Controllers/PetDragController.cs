using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CutePet.Desktop;

// Owns both pointer gestures and the temporary quota docking workspace.
internal sealed class PetDragController
{
    private readonly MainWindow window;
    private readonly bool verification;
    private Point? mouseStart;
    private bool dragged, quotaGesture, quotaDragging;
    private Point quotaGrabOffset;
    private QuotaDock originalDock, draftDock;
    private Preferences Settings => window.Settings;
    private bool CanDrag => window.CanDrag;
    internal bool IsDragging => quotaDragging;
    internal bool Dragged => dragged;
    public PetDragController(MainWindow window, bool verification)
    { this.window = window; this.verification = verification; }
    internal void CancelPointer() => mouseStart = null;
    internal void OnMouseDown(MouseButtonEventArgs e)
    {
        // Let buttons and scroll bars keep their own mouse gestures.
        var target = e.OriginalSource as DependencyObject;
        quotaGesture = false;
        while (target is not null && target != window.Scene)
        {
            if (target is System.Windows.Controls.Primitives.ButtonBase or System.Windows.Controls.Primitives.ScrollBar) return;
            if (target == window.QuotaCard) quotaGesture = true;
            target = target is Visual ? VisualTreeHelper.GetParent(target) : LogicalTreeHelper.GetParent(target);
        }
        if (target != window.Scene) return; // Popup controls keep their own input surface.
        mouseStart = e.GetPosition(window.Scene);
        quotaGrabOffset = e.GetPosition(window.QuotaCard);
        dragged = false;
    }
    internal void OnMouseMove(MouseEventArgs e)
    {
        if (quotaDragging)
        {
            if (e.LeftButton == MouseButtonState.Pressed) UpdateQuotaDrag(e.GetPosition(window.Scene));
            else EndQuotaDrag(cancel: false);
            e.Handled = true;
            return;
        }
        if (mouseStart is not Point start || e.LeftButton != MouseButtonState.Pressed || dragged) return;
        if ((e.GetPosition(window.Scene) - start).Length < 4) return;
        if (!CanDrag) { mouseStart = null; return; }
        if (quotaGesture)
        {
            BeginQuotaDrag(quotaGrabOffset);
            if (quotaDragging) UpdateQuotaDrag(e.GetPosition(window.Scene));
            e.Handled = true;
            return;
        }
        dragged = true;
        window.WakeCharacterImmediately();
        window.StopDetailsTimers();
        window.ShowDetails(false);
        try { window.DragMove(); } catch (InvalidOperationException) { }
        finally
        {
            mouseStart = null;
            var current = NativePlacement.Get(window);
            NativePlacement.Apply(window, current.Left, current.Top);
            window.SavePlacement();
            dragged = false;
            if (Settings.Details == DetailsMode.Always) window.ShowDetails(true);
            else window.PointerChanged(window.Scene.IsMouseOver);
        }
    }
    internal bool OnMouseUp(MouseButtonEventArgs e)
    {
        if (quotaDragging) { EndQuotaDrag(cancel: false); e.Handled = true; return false; }
        var clicked = mouseStart is not null && !dragged && window.PetStage.IsMouseOver;
        mouseStart = null;
        return clicked;
    }

    internal void BeginQuotaDrag(Point grabOffset)
    {
        if (quotaDragging || !CanDrag) return;
        var anchor = window.PetScreenOrigin();
        originalDock = draftDock = Settings.QuotaPosition;
        quotaGrabOffset = grabOffset;
        quotaDragging = dragged = true;
        window.WakeCharacterImmediately();
        mouseStart = null;
        window.StopDetailsTimers();
        window.ShowDetails(false);
        window.ApplyDockLayout(DockLayout.DragWorkspace(originalDock, Settings.EffectiveCharacterScale, Settings.EffectiveQuotaScale), anchor, constrain: false);
        window.DockTarget.Visibility = window.DockHint.Visibility = Visibility.Visible;
        ShowDockTarget();
        if (!verification)
        {
            window.Scene.Focus();
            if (!window.Scene.CaptureMouse()) EndQuotaDrag(cancel: true);
        }
    }
    internal void UpdateQuotaDrag(Point pointer)
    {
        if (!quotaDragging) return;
        var x = pointer.X - quotaGrabOffset.X;
        var y = pointer.Y - quotaGrabOffset.Y;
        var center = new Point(x + window.QuotaCard.Width / 2, y + window.QuotaCard.Height / 2);
        draftDock = DockLayout.Nearest(center, new(Canvas.GetLeft(window.PetStage), Canvas.GetTop(window.PetStage)), draftDock,
            Settings.EffectiveCharacterScale, Settings.EffectiveQuotaScale);
        Canvas.SetLeft(window.QuotaCard, Math.Clamp(x, 0, window.Scene.Width - window.QuotaCard.Width));
        Canvas.SetTop(window.QuotaCard, Math.Clamp(y, 0, window.Scene.Height - window.QuotaCard.Height));
        ShowDockTarget();
    }
    private void ShowDockTarget()
    {
        var target = DockLayout.Target(draftDock, new(Canvas.GetLeft(window.PetStage), Canvas.GetTop(window.PetStage)),
            Settings.EffectiveCharacterScale, Settings.EffectiveQuotaScale);
        MainWindow.Place(window.DockTarget, target);
        window.DockHintText.Text = "松开 → " + DockLayout.Label(draftDock) + " · Esc 取消";
        Canvas.SetLeft(window.DockHint, Math.Clamp(target.X, 0, window.Scene.Width - 150));
        Canvas.SetTop(window.DockHint, Math.Max(0, target.Y - 27));
    }
    internal void EndQuotaDrag(bool cancel)
    {
        if (!quotaDragging) return;
        var anchor = window.PetScreenOrigin();
        quotaDragging = dragged = quotaGesture = false;
        if (window.Scene.IsMouseCaptured) window.Scene.ReleaseMouseCapture();
        window.DockTarget.Visibility = window.DockHint.Visibility = Visibility.Collapsed;
        window.UpdateDockPreference(cancel ? originalDock : draftDock);
        window.ApplyDockLayout(window.CurrentLayout, anchor, constrain: true);
        window.SavePlacement();
        if (Settings.Details == DetailsMode.Always) window.ShowDetails(true);
        else window.PointerChanged(window.Scene.IsMouseOver);
    }
    internal void OnDockCaptureLost()
    {
        if (quotaDragging && !window.Scene.IsMouseCaptured) EndQuotaDrag(cancel: true);
    }
    internal void OnDockKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.Escape && quotaDragging) { EndQuotaDrag(cancel: true); e.Handled = true; }
    }

}
