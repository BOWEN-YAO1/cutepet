using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
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
internal static class CharacterPackLoader
{
    internal static readonly string[] ActionNames = { "idle", "blink", "greeting", "low" };
    internal static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true, WriteIndented = true };
    public static bool ValidId(string? id) => id is not null && Regex.IsMatch(id, "\\A[a-z][a-z0-9-]{0,63}\\z");
    public static bool SafeFile(string name) => name.Length <= 120
        && Regex.IsMatch(name, "\\A[a-zA-Z0-9_-]+(?:/[a-zA-Z0-9_-]+)*\\.png\\z", RegexOptions.IgnoreCase);
    public static CharacterPack Load(Stream manifestStream, Func<string, Stream> openImage, bool builtIn, string? directory = null)
    {
        using var limited = new MemoryStream();
        CopyLimited(manifestStream, limited, 64 * 1024);
        var jsonBytes = limited.ToArray().AsSpan();
        if (jsonBytes.StartsWith(new byte[] { 0xEF, 0xBB, 0xBF })) jsonBytes = jsonBytes[3..];
        var manifest = JsonSerializer.Deserialize<CharacterManifest>(jsonBytes, Json)
            ?? throw new InvalidDataException("缺少角色配置。");
        if (manifest.FormatVersion != 1) throw new InvalidDataException("角色包版本不支持，请使用 formatVersion: 1。");
        if (!ValidId(manifest.Id) || string.IsNullOrWhiteSpace(manifest.Name) || manifest.Name.Length > 60
            || manifest.Name.Any(char.IsControl) || manifest.Author is null || manifest.Author.Length > 200
            || manifest.License is null || manifest.License.Length > 2000)
            throw new InvalidDataException("角色编号、名称或作者说明不合法。");
        if (!double.IsFinite(manifest.DisplayWidth) || !double.IsFinite(manifest.DisplayHeight)
            || manifest.DisplayWidth < 40 || manifest.DisplayWidth > 210 || manifest.DisplayHeight < 40 || manifest.DisplayHeight > 148
            || manifest.BlinkIntervalMs < 1000 || manifest.BlinkIntervalMs > 60000)
            throw new InvalidDataException("角色显示大小或眨眼间隔超出允许范围。");
        if (manifest.Actions is null || !manifest.Actions.ContainsKey("idle") || manifest.Actions.Count > 4
            || manifest.Actions.Keys.Any(key => !ActionNames.Contains(key)))
            throw new InvalidDataException("必须提供 idle 动作；当前支持 idle、blink、greeting、low。");
        var images = new Dictionary<string, BitmapSource>(StringComparer.OrdinalIgnoreCase);
        var actions = new Dictionary<string, LoadedAction>();
        long pixels = 0;
        var frameCount = 0;
        int? width = null, height = null;
        foreach (var (name, action) in manifest.Actions)
        {
            if (action is null || action.Frames is null || action.Frames.Count == 0
                || (frameCount += action.Frames.Count) > 120 || action.Loop != (name is "idle" or "low"))
                throw new InvalidDataException("待机和低额度动作必须循环；眨眼和打招呼必须有限播放，最多 120 帧。");
            var loaded = new List<LoadedFrame>();
            foreach (var frame in action.Frames)
            {
                if (frame is null || frame.Image is null || !SafeFile(frame.Image) || frame.DurationMs < 40 || frame.DurationMs > 10000)
                    throw new InvalidDataException("动作图片路径或帧时间不合法（40–10000 毫秒）。");
                if (!images.TryGetValue(frame.Image, out var image))
                {
                    using var stream = openImage(frame.Image);
                    using var bytes = new MemoryStream();
                    CopyLimited(stream, bytes, 8 * 1024 * 1024);
                    bytes.Position = 0;
                    var decoder = new PngBitmapDecoder(bytes, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    var decoded = decoder.Frames[0];
                    if (decoder.Frames.Count != 1 || decoded.PixelWidth > 2048 || decoded.PixelHeight > 2048
                        || (pixels += (long)decoded.PixelWidth * decoded.PixelHeight) > 16_777_216)
                        throw new InvalidDataException("图片超过大小限制：单帧最大 2048×2048，总解码像素最大 1600 万。");
                    width ??= decoded.PixelWidth;
                    height ??= decoded.PixelHeight;
                    if (decoded.PixelWidth != width || decoded.PixelHeight != height)
                        throw new InvalidDataException("同一角色的所有动作帧需要保持相同画布大小。");
                    decoded.Freeze();
                    images.Add(frame.Image, image = decoded);
                }
                loaded.Add(new(image, frame.DurationMs));
            }
            var clip = new LoadedAction(action.Loop, loaded);
            if (clip.Duration > 30000) throw new InvalidDataException("一个动作最多持续 30 秒。");
            actions.Add(name, clip);
        }
        return new(manifest, builtIn, actions, directory);
    }
    internal static void CopyLimited(Stream source, Stream destination, long limit)
    {
        var buffer = new byte[81920];
        long total = 0;
        int count;
        while ((count = source.Read(buffer, 0, buffer.Length)) != 0)
        {
            total += count;
            if (total > limit) throw new InvalidDataException("角色包文件超过大小限制。");
            destination.Write(buffer, 0, count);
        }
    }
}
