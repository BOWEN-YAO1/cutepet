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
    internal double PeekOffset { get; private set; }
    internal EdgeMotionController(MainWindow window, bool verification)
    { this.window = window; this.verification = verification; }

    internal bool Attach(Rect area, Size size, Point released, double dpi)
    {
        if (!window.CanDrag || !window.Settings.EdgeInteraction || !window.SelectedCharacter.Actions.ContainsKey("edge-idle")
            || window.Model.IsLow && !window.Model.IsStale) return false;
        var plan = ScreenEdgeLayout.Plan(area, size, released, dpi);
        if (plan is null) return false;
        window.WakeCharacterImmediately();
        Attachment = plan;
        entering = 0;
        window.StartEdgePose();
        Render();
        if (!verification) NativePlacement.Apply(window, plan.Position.X, plan.Position.Y);
        window.SavePlacement();
        return true;
    }
    internal void Peek()
    {
        if (!Active || peekElapsed >= 0 || !window.SelectedCharacter.Actions.ContainsKey("edge-peek")) return;
        peekElapsed = 0;
        window.StartEdgePeek();
        Render();
    }
    internal void Advance(TimeSpan elapsed)
    {
        if (!Active) return;
        if (!verification)
        {
            var bounds = NativePlacement.RoamingBounds(window);
            if (bounds.Area != Attachment!.Area || bounds.Size != Attachment.PetSize)
            { window.WakeCharacterImmediately(); NativePlacement.Apply(window, bounds.Origin.X, bounds.Origin.Y); window.SavePlacement(); return; }
        }
        if (!window.CanPlayAmbient) return;
        entering = Math.Min(320, entering + Math.Max(0, elapsed.TotalMilliseconds));
        if (peekElapsed < 0) { Render(); return; }
        var duration = window.SelectedCharacter.Actions["edge-peek"].Duration;
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
        window.EdgeShift.X = 0;
        window.Scene.ClipToBounds = false;
        window.GroundShadow.Visibility = Visibility.Visible;
    }
    private void Render()
    {
        if (Attachment is null) return;
        var right = Attachment.Side == ScreenEdge.Right;
        var width = window.CharacterArt.Width;
        var image = window.SelectedCharacter.Actions["edge-idle"].Frames[0].Image;
        var renderedWidth = Math.Min(width, window.CharacterArt.Height * image.PixelWidth / image.PixelHeight);
        // The package artwork's left canvas edge is the virtual border. Hands may overlap it.
        var anchor = (230 - renderedWidth) / 2 + renderedWidth * window.SelectedCharacter.Manifest.EdgeAnchorX;
        var t = peekElapsed < 0 ? 0 : peekElapsed / window.SelectedCharacter.Actions["edge-peek"].Duration;
        PeekOffset = 8 * Math.Pow(Math.Sin(Math.PI * t), 2);
        window.EdgeMirror.ScaleX = right ? -1 : 1;
        var entry = entering / 320;
        entry = entry * entry * (3 - 2 * entry);
        window.EdgeShift.X = (right ? 1 : -1) * (anchor * entry - PeekOffset);
        window.Scene.ClipToBounds = true;
        window.GroundShadow.Visibility = Visibility.Hidden;
    }
}
