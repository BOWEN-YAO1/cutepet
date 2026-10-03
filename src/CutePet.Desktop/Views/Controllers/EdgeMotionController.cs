using System;
using System.Windows;
using System.Windows.Media;

namespace CutePet.Desktop;

// Keep the whole HWND inside its monitor. Clip only the character artwork at the edge.
internal sealed class EdgeMotionController
{
    private readonly MainWindow window;
    private readonly bool verification;
    private double peekElapsed = -1;
    private double entering;
    internal EdgeAttachment? Attachment { get; private set; }
    internal bool Active => Attachment is not null;
    private string BaseAction => EdgeActions.Base(Attachment!.Side);
    private string PeekAction => EdgeActions.Peek(BaseAction);
    internal double PeekOffset { get; private set; }
    internal EdgeMotionController(MainWindow window, bool verification)
    { this.window = window; this.verification = verification; }

    internal bool Attach(Rect area, Size size, Point released, double dpi)
    {
        if (!window.CanDrag || !window.Settings.EdgeInteraction || !EdgeActions.Available(window.SelectedCharacter)
            || window.Model.IsLow && !window.Model.IsStale) return false;
        var plan = ScreenEdgeLayout.Plan(area, size, released, dpi, EdgeActions.Supported(window.SelectedCharacter));
        if (plan is null) return false;
        window.WakeCharacterImmediately();
        Attachment = plan;
        entering = 0;
        window.StartEdgePose(BaseAction);
        Render();
        if (!verification) NativePlacement.Apply(window, plan.Position.X, plan.Position.Y);
        window.SavePlacement();
        return true;
    }
    internal void Peek()
    {
        if (!Active || peekElapsed >= 0 || !window.SelectedCharacter.Actions.ContainsKey(PeekAction)) return;
        peekElapsed = 0;
        window.StartEdgePeek();
        Render();
    }
    internal void Advance(TimeSpan elapsed)
    {
        if (!Active) return;
        if (!verification)
        {
            var bounds = NativePlacement.EdgeBounds(window);
            if (bounds.Area != Attachment!.Area || bounds.Size != Attachment.PetSize)
            { Reanchor(bounds.Area, bounds.Size); if (!Active) return; }
        }
        if (!window.CanPlayAmbient) return;
        entering = Math.Min(320, entering + Math.Max(0, elapsed.TotalMilliseconds));
        if (peekElapsed < 0) { Render(); return; }
        var duration = window.SelectedCharacter.Actions[PeekAction].Duration;
        peekElapsed += Math.Max(0, elapsed.TotalMilliseconds);
        if (peekElapsed >= duration) peekElapsed = -1;
        Render();
    }
    internal void Cancel()
    {
        Attachment = null;
        peekElapsed = -1;
        PeekOffset = 0;
        window.EdgeMirror.ScaleX = 1;
        window.EdgeStretch.ScaleY = 1;
        window.EdgeStretch.CenterY = 0;
        window.EdgeShift.X = 0;
        window.EdgeShift.Y = 0;
        window.Scene.ClipToBounds = false;
        window.GroundShadow.Visibility = Visibility.Visible;
    }
    internal void Reanchor(Rect area, Size size)
    {
        if (Attachment is null) return;
        var plan = ScreenEdgeLayout.Anchor(Attachment.Side, area, size, Attachment.Position);
        if (plan is null)
        {
            window.WakeCharacterImmediately();
            if (!verification) { var current = NativePlacement.Get(window); NativePlacement.Apply(window, current.Left, current.Top); }
            window.SavePlacement(); return;
        }
        Attachment = plan;
        Render();
        if (!verification) NativePlacement.Apply(window, plan.Position.X, plan.Position.Y);
        window.SavePlacement();
    }
    private void Render()
    {
        if (Attachment is null) return;
        var right = Attachment.Side == ScreenEdge.Right;
        var width = window.CharacterArt.Width;
        var image = window.SelectedCharacter.Actions[BaseAction].Frames[0].Image;
        var renderedWidth = Math.Min(width, window.CharacterArt.Height * image.PixelWidth / image.PixelHeight);
        var renderedHeight = Math.Min(window.CharacterArt.Height, width * image.PixelHeight / image.PixelWidth);
        // The package artwork's left canvas edge is the virtual border. Hands may overlap it.
        var anchor = (230 - renderedWidth) / 2 + renderedWidth * window.SelectedCharacter.Manifest.EdgeAnchorX;
        var t = peekElapsed < 0 ? 0 : peekElapsed / window.SelectedCharacter.Actions[PeekAction].Duration;
        PeekOffset = 8 * Math.Pow(Math.Sin(Math.PI * t), 2);
        window.EdgeMirror.ScaleX = right ? -1 : 1;
        var entry = entering / 320;
        entry = entry * entry * (3 - 2 * entry);
        window.EdgeShift.X = Attachment.Side is ScreenEdge.Left or ScreenEdge.Right
            ? (right ? 1 : -1) * (anchor * entry - PeekOffset) : 0;
        var imageTop = 16 + (window.CharacterArt.Height - renderedHeight) / 2;
        var verticalAnchor = Attachment.Side == ScreenEdge.Top ? window.SelectedCharacter.Manifest.EdgeTopAnchorY
            : window.SelectedCharacter.Manifest.EdgeBottomAnchorY;
        // Top peeking changes expression only; bottom keeps a small elbow-anchored lift.
        var vertical = Attachment.Side is ScreenEdge.Top or ScreenEdge.Bottom;
        window.EdgeStretch.CenterY = vertical ? imageTop - 16 + renderedHeight * verticalAnchor - window.CharacterArt.Height / 2 : 0;
        window.EdgeStretch.ScaleY = Attachment.Side == ScreenEdge.Bottom ? 1 + 0.04 * PeekOffset / 8 : 1;
        window.EdgeShift.Y = Attachment.Side switch
        {
            ScreenEdge.Top => -(imageTop + renderedHeight * window.SelectedCharacter.Manifest.EdgeTopAnchorY) * entry,
            ScreenEdge.Bottom => (178 - imageTop - renderedHeight * window.SelectedCharacter.Manifest.EdgeBottomAnchorY) * entry,
            _ => 0
        };
        window.Scene.ClipToBounds = true;
        window.GroundShadow.Visibility = Visibility.Hidden;
    }
}
