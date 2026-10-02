using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Media.Imaging;

namespace CutePet.Desktop;

internal static class CharacterPackLoader
{
    internal static readonly string[] ActionNames = { "idle", "blink", "greeting", "low", "look", "hover", "happy", "conjure", "sit", "stand" };
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
            || manifest.BlinkIntervalMs < 1000 || manifest.BlinkIntervalMs > 60000
            || (manifest.RestAfterMs != 0 && (manifest.RestAfterMs < 5000 || manifest.RestAfterMs > 300000))
            || manifest.RestDurationMs < 1000 || manifest.RestDurationMs > 300000)
            throw new InvalidDataException("角色显示大小或眨眼间隔超出允许范围。");
        if (manifest.Actions is null || !manifest.Actions.ContainsKey("idle") || manifest.Actions.Count > ActionNames.Length
            || manifest.Actions.Keys.Any(key => !ActionNames.Contains(key)))
            throw new InvalidDataException("必须提供 idle 动作；支持 idle、blink、greeting、low、look、hover、happy、conjure、sit、stand。");
        if (!manifest.Actions.ContainsKey("sit") && (manifest.RestAfterMs != 0 || manifest.Actions.ContainsKey("conjure") || manifest.Actions.ContainsKey("stand")))
            throw new InvalidDataException("召唤、起身或自动休息需要配套 sit 动作。");
        var images = new Dictionary<string, BitmapSource>(StringComparer.OrdinalIgnoreCase);
        var actions = new Dictionary<string, LoadedAction>();
        long pixels = 0;
        var frameCount = 0;
        int? width = null, height = null;
        foreach (var (name, action) in manifest.Actions)
        {
            if (action is null || action.Frames is null || action.Frames.Count == 0
                || (frameCount += action.Frames.Count) > 120 || action.Loop != (name is "idle" or "low" or "sit"))
                throw new InvalidDataException("待机、低额度和坐姿必须循环；其他动作必须有限播放，最多 120 帧。");
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
                        || (pixels += (long)decoded.PixelWidth * decoded.PixelHeight) > 25_165_824)
                        throw new InvalidDataException("图片超过大小限制：单帧最大 2048×2048，总解码像素最大 25,165,824。");
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
