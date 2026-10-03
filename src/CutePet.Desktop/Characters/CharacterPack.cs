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
    public List<CharacterActionFrame> Frames { get; init; } = new();
}
public sealed record CharacterActionFrame(string Image, int DurationMs = 180);
internal sealed record LoadedFrame(BitmapSource Image, int DurationMs);
internal sealed record LoadedAction(bool Loop, IReadOnlyList<LoadedFrame> Frames)
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
    BitmapSource? SwingOrnamentImage = null, BitmapSource? SwingSceneryImage = null)
{
    public string Id => Manifest.Id;
    public string Name => Manifest.Name;
    public LoadedAction Idle => Actions["idle"];
}
