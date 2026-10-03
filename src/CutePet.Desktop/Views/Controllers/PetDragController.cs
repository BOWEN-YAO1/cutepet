using System;
using System.Windows;
using System.Windows.Input;

namespace CutePet.Desktop;

// Pet gestures move only the pet window. Quota gestures belong to its independent window.
internal sealed class PetDragController
{
    private readonly MainWindow window;
    private Point? mouseStart;
    private bool dragged;
    internal bool IsDragging => window.QuotaHost.Dragging;
    internal bool Dragged => dragged || IsDragging;
    public PetDragController(MainWindow window, bool verification) { this.window = window; }
    internal void CancelPointer() => mouseStart = null;
    internal void OnMouseDown(MouseButtonEventArgs e)
    { mouseStart = e.GetPosition(window.Scene); dragged = false; }
    internal void OnMouseMove(MouseEventArgs e)
    {
        if (mouseStart is not Point start || e.LeftButton != MouseButtonState.Pressed || dragged) return;
        if ((e.GetPosition(window.Scene) - start).Length < 4) return;
        if (!window.CanDrag) { mouseStart = null; return; }
        dragged = true;
        window.WakeCharacterImmediately();
        try { window.DragMove(); } catch (InvalidOperationException) { }
        finally
        {
            mouseStart = null;
            var bounds = NativePlacement.EdgeBounds(window);
            window.CompletePetDrag(bounds.Area, bounds.Size, bounds.Origin,
                System.Windows.Media.VisualTreeHelper.GetDpi(window).DpiScaleX);
            dragged = false;
        }
    }
    internal bool OnMouseUp(MouseButtonEventArgs e)
    {
        var clicked = mouseStart is not null && !dragged && window.PetStage.IsMouseOver;
        mouseStart = null;
        return clicked;
    }
    internal void BeginQuotaDrag(Point offset) => window.QuotaHost.BeginDrag(offset);
    internal void UpdateQuotaDrag(Point pointer) => window.QuotaHost.UpdateDrag(pointer);
    internal void EndQuotaDrag(bool cancel) => window.QuotaHost.EndDrag(cancel);
}
