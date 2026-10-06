using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media.Imaging;

namespace CutePet.Desktop;

public sealed record CharacterManifest
{
    public int FormatVersion { get; init; } = 1;
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public string Author { get; init; } = "";
    public string License { get; init; } = "";
    public double DisplayWidth { get; init; } = 178;
    public double DisplayHeight { get; init; } = 148;
    public bool Float { get; init; } = true;
    public int BlinkIntervalMs { get; init; } = 4000;
    public int RestAfterMs { get; init; }
    public int RestDurationMs { get; init; } = 20000;
    public double EdgeAnchorX { get; init; }
    public double EdgeTopAnchorY { get; init; }
    public double EdgeBottomAnchorY { get; init; } = 1;
    public CharacterCloud? Cloud { get; init; }
    public CharacterTopSwing? TopSwing { get; init; }
    public CharacterSideAnimation? SideAnimation { get; init; }
    public CharacterTopAnimation? TopAnimation { get; init; }
    public CharacterBottomAnimation? BottomAnimation { get; init; }
    public Dictionary<string, string[]>? Dialogue { get; init; }
    public Dictionary<string, CharacterAction> Actions { get; init; } = new();
}
public sealed record CharacterTopSwing
{
    public double SeatAnchorY { get; init; } = 0.69;
    public double SeatHalfWidth { get; init; } = 0.36;
    public string RopeColor { get; init; } = "#897055";
    public CharacterSwingOrnament? Ornament { get; init; }
    public CharacterSwingScenery? Scenery { get; init; }
}
public sealed record CharacterSwingScenery
{
    public string Image { get; init; } = "";
    public string Layout { get; init; } = "floating";
    public double DisplayWidth { get; init; } = 42;
    public double DisplayHeight { get; init; } = 42;
}
public sealed record CharacterSwingOrnament
{
    public string Image { get; init; } = "";
    public double DisplayWidth { get; init; } = 18;
    public double DisplayHeight { get; init; } = 27;
}
public sealed record CharacterCloud
{
    public string Image { get; init; } = "";
    public double DisplayWidth { get; init; } = 140;
    public double DisplayHeight { get; init; } = 32;
}
public sealed record CharacterAction
{
    public bool Loop { get; init; }
    public bool SmoothFrames { get; init; }
    public List<CharacterActionFrame> Frames { get; init; } = new();
}
// Continuous motion curves use a single registered texture, not pose sequences.
public sealed record CharacterSideAnimation
{
    public double IdleOffsetX { get; init; } = -32;
    public double HeadPivotX { get; init; } = .5;
    public double HeadPivotY { get; init; } = .52;
    public Dictionary<string, List<CharacterMotionKey>> Clips { get; init; } = new();
}
public sealed record CharacterMotionKey(double At, double Peek = 0, double Lift = 0, double Angle = 0, double Sway = 0, double Blink = 0);
public sealed record CharacterFrameRegion(int X, int Y, int Width, int Height);
// Optional registration of a differently sized source crop on the action canvas.
public sealed record CharacterFrameCanvas(int Width, int Height, double Scale, double OffsetX, double OffsetY);
public sealed record CharacterActionFrame(string Image, int DurationMs = 180)
{
    public CharacterFrameRegion? Region { get; init; }
    public CharacterFrameCanvas? Canvas { get; init; }
    public double? EdgeAnchorX { get; init; }
    public double? EdgeAnchorY { get; init; }
    public double? SwingSeatAnchorY { get; init; }
    public double? HeadAnchorX { get; init; }
    public double? HeadAnchorY { get; init; }
}
internal sealed record LoadedFrame(BitmapSource Image, int DurationMs, double? EdgeAnchorX = null, double? EdgeAnchorY = null, double? SwingSeatAnchorY = null,
    double? HeadAnchorX = null, double? HeadAnchorY = null);
internal sealed record LoadedAction(bool Loop, IReadOnlyList<LoadedFrame> Frames, bool SmoothFrames = false)
{
    public double Duration => Frames.Sum(frame => frame.DurationMs);
    public BitmapSource At(double elapsed)
        => Frames[PositionAt(elapsed).Index].Image;
    internal (int Index, double Fraction) PositionAt(double elapsed)
    {
        var remaining = Loop ? elapsed % Duration : Math.Min(elapsed, Duration - 1);
        for (var index = 0; index < Frames.Count; index++)
        {
            var frame = Frames[index];
            if (remaining < frame.DurationMs) return (index, remaining / frame.DurationMs);
            remaining -= frame.DurationMs;
        }
        return (Frames.Count - 1, 0.999999);
    }
}
internal sealed record CharacterPack(CharacterManifest Manifest, bool BuiltIn,
    IReadOnlyDictionary<string, LoadedAction> Actions, string? Directory = null, BitmapSource? CloudImage = null,
    BitmapSource? SwingOrnamentImage = null, BitmapSource? SwingSceneryImage = null, BitmapSource? TopClosedEyesImage = null,
    BitmapSource? BottomClosedEyesImage = null)
{
    public string Id => Manifest.Id;
    public string Name => Manifest.Name;
    public LoadedAction Idle => Actions["idle"];
    internal bool UsesFrameRegions(string action) => Manifest.Actions[action].Frames[0].Region is not null;
}

public sealed record CharacterTopAnimation
{
    public double HeadPivotX { get; init; } = .5;
    public double HeadPivotY { get; init; } = .46;
    public string ClosedEyesImage { get; init; } = "";
    public List<CharacterFrameRegion> Eyes { get; init; } = new();
    public Dictionary<string,List<CharacterMotionKey>> Clips { get; init; } = new();
}

public sealed record CharacterBottomAnimation
{
    public double HeadPivotX { get; init; } = .5;
    public double HeadPivotY { get; init; } = .74;
    public string ClosedEyesImage { get; init; } = "";
    public CharacterFrameRegion? ClosedEyesRegion { get; init; }
    public List<CharacterFrameRegion> Eyes { get; init; } = new();
    public Dictionary<string,List<CharacterMotionKey>> Clips { get; init; } = new();
}
