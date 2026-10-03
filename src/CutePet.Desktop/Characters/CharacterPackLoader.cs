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
    internal static readonly string[] ActionNames = { "idle", "blink", "greeting", "low", "look", "hover", "happy", "conjure", "sit", "stand", "summon-cloud", "sit-blink", "sit-greeting", "sit-happy", "edge-idle", "edge-peek", "edge-top-idle", "edge-top-peek", "edge-bottom-idle", "edge-bottom-peek" };
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
            || manifest.RestDurationMs < 1000 || manifest.RestDurationMs > 300000
            || !double.IsFinite(manifest.EdgeAnchorX) || manifest.EdgeAnchorX < 0 || manifest.EdgeAnchorX > 0.5
            || !double.IsFinite(manifest.EdgeTopAnchorY) || manifest.EdgeTopAnchorY < 0 || manifest.EdgeTopAnchorY > 0.5
            || !double.IsFinite(manifest.EdgeBottomAnchorY) || manifest.EdgeBottomAnchorY < 0.5 || manifest.EdgeBottomAnchorY > 1)
            throw new InvalidDataException("角色显示大小或眨眼间隔超出允许范围。");
        if (manifest.Actions is null || !manifest.Actions.ContainsKey("idle") || manifest.Actions.Count > ActionNames.Length
            || manifest.Actions.Keys.Any(key => !ActionNames.Contains(key)))
            throw new InvalidDataException("必须提供 idle 动作；支持 " + string.Join("、", ActionNames) + "。");
        if (manifest.Cloud is { } cloud && (cloud.Image is null || !SafeFile(cloud.Image)
            || !double.IsFinite(cloud.DisplayWidth) || !double.IsFinite(cloud.DisplayHeight)
            || cloud.DisplayWidth < 40 || cloud.DisplayWidth > 190 || cloud.DisplayHeight < 12 || cloud.DisplayHeight > 40))
            throw new InvalidDataException("云层图片路径或显示大小不合法。");
        if (manifest.Actions.ContainsKey("summon-cloud") && manifest.Cloud is null)
            throw new InvalidDataException("召唤云动作需要配套 cloud 图层。");
        if (manifest.TopSwing is { } swing && (!manifest.Actions.ContainsKey("edge-top-idle")
            || !double.IsFinite(swing.SeatAnchorY) || swing.SeatAnchorY <= manifest.EdgeTopAnchorY + 0.1 || swing.SeatAnchorY > 0.95
            || !double.IsFinite(swing.SeatHalfWidth) || swing.SeatHalfWidth < 0.1 || swing.SeatHalfWidth > 0.5
            || swing.RopeColor is null || !Regex.IsMatch(swing.RopeColor, "\\A#[0-9a-fA-F]{6}\\z")))
            throw new InvalidDataException("上沿秋千需要上侧基础动作、合法坐板位置和六位十六进制绳索颜色。");
        if (manifest.TopSwing?.Ornament is { } ornament && (ornament.Image is null || !SafeFile(ornament.Image)
            || !double.IsFinite(ornament.DisplayWidth) || ornament.DisplayWidth < 6 || ornament.DisplayWidth > 30
            || !double.IsFinite(ornament.DisplayHeight) || ornament.DisplayHeight < 8 || ornament.DisplayHeight > 44))
            throw new InvalidDataException("秋千挂饰图片路径或显示大小不合法。");
        if (manifest.TopSwing?.Scenery is { } scenery && (scenery.Image is null || !SafeFile(scenery.Image)
            || !double.IsFinite(scenery.DisplayWidth) || scenery.DisplayWidth < 12 || scenery.DisplayWidth > 60
            || !double.IsFinite(scenery.DisplayHeight) || scenery.DisplayHeight < 12 || scenery.DisplayHeight > 60))
            throw new InvalidDataException("秋千两侧装饰图片路径或显示大小不合法。");
        foreach (var baseAction in new[] { "edge-idle", "edge-top-idle", "edge-bottom-idle" })
            if (manifest.Actions.ContainsKey(EdgeActions.Peek(baseAction)) && !manifest.Actions.ContainsKey(baseAction))
                throw new InvalidDataException("探头回应需要配套 " + baseAction + " 贴边姿势。");
        if (!manifest.Actions.ContainsKey("sit") && (manifest.RestAfterMs != 0 || manifest.Actions.ContainsKey("conjure") || manifest.Actions.ContainsKey("stand")
            || manifest.Actions.Keys.Any(key => key.StartsWith("sit-", StringComparison.Ordinal))))
            throw new InvalidDataException("召唤、起身、坐姿回应或自动休息需要配套 sit 动作。");
        var images = new Dictionary<string, BitmapSource>(StringComparer.OrdinalIgnoreCase);
        var actions = new Dictionary<string, LoadedAction>();
        long pixels = 0;
        var frameCount = 0;
        int? width = null, height = null;
        foreach (var (name, action) in manifest.Actions)
        {
            if (action is null || action.Frames is null || action.Frames.Count == 0
                || (frameCount += action.Frames.Count) > 120 || action.Loop != (name is "idle" or "low" or "sit" || EdgeActions.BaseOf(name) == name))
                throw new InvalidDataException("待机、低额度、坐姿和贴边姿势必须循环；其他动作必须有限播放，最多 120 帧。");
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
                        || (pixels += (long)decoded.PixelWidth * decoded.PixelHeight) > 44_040_192)
                        throw new InvalidDataException("图片超过大小限制：单帧最大 2048×2048，总解码像素最大 44,040,192。");
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
        var cloudImage = manifest.Cloud is { } layer ? LoadLayer(layer.Image) : null;
        var ornamentImage = manifest.TopSwing?.Ornament is { } decoration ? LoadLayer(decoration.Image) : null;
        var sceneryImage = manifest.TopSwing?.Scenery is { } sceneryLayer ? LoadLayer(sceneryLayer.Image) : null;
        return new(manifest, builtIn, actions, directory, cloudImage, ornamentImage, sceneryImage);

        BitmapSource LoadLayer(string name)
        {
            if (!images.TryGetValue(name, out var image))
            {
                using var stream = openImage(name);
                using var bytes = new MemoryStream();
                CopyLimited(stream, bytes, 8 * 1024 * 1024);
                bytes.Position = 0;
                var decoder = new PngBitmapDecoder(bytes, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                var decoded = decoder.Frames[0];
                if (decoder.Frames.Count != 1 || decoded.PixelWidth > 2048 || decoded.PixelHeight > 2048
                    || (pixels += (long)decoded.PixelWidth * decoded.PixelHeight) > 44_040_192)
                    throw new InvalidDataException("角色附加图层超过解码大小限制。");
                decoded.Freeze();
                images.Add(name, image = decoded);
            }
            return image;
        }
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
