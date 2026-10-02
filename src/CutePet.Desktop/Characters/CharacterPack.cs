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
    public Dictionary<string, CharacterAction> Actions { get; init; } = new();
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
    {
        var remaining = Loop ? elapsed % Duration : Math.Min(elapsed, Duration - 1);
        foreach (var frame in Frames)
        {
            if (remaining < frame.DurationMs) return frame.Image;
            remaining -= frame.DurationMs;
        }
        return Frames[^1].Image;
    }
}
internal sealed record CharacterPack(CharacterManifest Manifest, bool BuiltIn,
    IReadOnlyDictionary<string, LoadedAction> Actions, string? Directory = null)
{
    public string Id => Manifest.Id;
    public string Name => Manifest.Name;
    public LoadedAction Idle => Actions["idle"];
}
