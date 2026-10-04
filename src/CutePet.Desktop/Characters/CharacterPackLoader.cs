using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CutePet.Desktop;

internal static class CharacterPackLoader
{
    internal const int MaxFrameReferences = 2048;
    internal const int MaxManifestBytes = 1024 * 1024;
    // Dense drawn transition sets need more cached source/canvas pixels. Keep a
    // finite 384 MiB conservative package budget; repeated views still share it.
    internal const long MaxDecodedPixels = 100_663_296;
    internal static readonly string[] ActionNames = { "idle", "blink", "greeting", "low", "look", "hover", "happy", "conjure", "sit", "stand", "summon-cloud", "cloud-idle", "cloud-blink", "sit-blink", "sit-greeting", "sit-happy", "edge-idle", "edge-peek", "edge-shy", "edge-sway", "edge-nod", "edge-top-idle", "edge-top-peek", "edge-top-look", "edge-top-smile", "edge-bottom-idle", "edge-bottom-peek", "edge-bottom-look", "edge-bottom-smile" };
    internal static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true, WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
    public static bool ValidId(string? id) => id is not null && Regex.IsMatch(id, "\\A[a-z][a-z0-9-]{0,63}\\z");
    public static bool SafeFile(string name) => name.Length <= 120
        && Regex.IsMatch(name, "\\A[a-zA-Z0-9_-]+(?:/[a-zA-Z0-9_-]+)*\\.png\\z", RegexOptions.IgnoreCase);
    public static CharacterPack Load(Stream manifestStream, Func<string, Stream> openImage, bool builtIn, string? directory = null)
    {
        using var limited = new MemoryStream();
        CopyLimited(manifestStream, limited, MaxManifestBytes);
        var jsonBytes = limited.ToArray().AsSpan();
        if (jsonBytes.StartsWith(new byte[] { 0xEF, 0xBB, 0xBF })) jsonBytes = jsonBytes[3..];
        var manifest = JsonSerializer.Deserialize<CharacterManifest>(jsonBytes, Json)
            ?? throw new InvalidDataException("缺少角色配置。");
        if (manifest.FormatVersion != 1) throw new InvalidDataException("角色包版本不支持，请使用 formatVersion: 1。");
        CharacterDialogue.Validate(manifest.Dialogue);
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
        if ((manifest.Actions.ContainsKey("summon-cloud") || manifest.Actions.ContainsKey("cloud-idle") || manifest.Actions.ContainsKey("cloud-blink")) && manifest.Cloud is null)
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
            || scenery.Layout is not ("floating" or "garden")
            || !double.IsFinite(scenery.DisplayWidth) || scenery.DisplayWidth < 12 || scenery.DisplayWidth > 60
            || !double.IsFinite(scenery.DisplayHeight) || scenery.DisplayHeight < 12 || scenery.DisplayHeight > 60))
            throw new InvalidDataException("秋千两侧装饰图片路径或显示大小不合法。");
        foreach (var baseAction in new[] { "edge-idle", "edge-top-idle", "edge-bottom-idle" })
            if (manifest.Actions.ContainsKey(EdgeActions.Peek(baseAction)) && !manifest.Actions.ContainsKey(baseAction))
                throw new InvalidDataException("探头回应需要配套 " + baseAction + " 贴边姿势。");
        if (SideEdgeMotion.Responses.Any(action => manifest.Actions.ContainsKey(action)) && !manifest.Actions.ContainsKey("edge-idle"))
            throw new InvalidDataException("左右贴边回应需要配套 edge-idle 姿势。");
        if (EdgeActions.TopResponses.Any(action => manifest.Actions.ContainsKey(action)) && !manifest.Actions.ContainsKey("edge-top-idle"))
            throw new InvalidDataException("上沿回应需要配套 edge-top-idle 姿势。");
        if (EdgeActions.BottomResponses.Any(action => manifest.Actions.ContainsKey(action)) && !manifest.Actions.ContainsKey("edge-bottom-idle"))
            throw new InvalidDataException("下沿回应需要配套 edge-bottom-idle 姿势。");
        if (!manifest.Actions.ContainsKey("sit") && (manifest.RestAfterMs != 0 || manifest.Actions.ContainsKey("conjure") || manifest.Actions.ContainsKey("stand")
            || manifest.Actions.Keys.Any(key => key.StartsWith("sit-", StringComparison.Ordinal))))
            throw new InvalidDataException("召唤、起身、坐姿回应或自动休息需要配套 sit 动作。");
        var images = new Dictionary<string, BitmapSource>(StringComparer.OrdinalIgnoreCase);
        var regions = new Dictionary<(string, CharacterFrameRegion), BitmapSource>();
        var canvases = new Dictionary<(string, CharacterFrameRegion, CharacterFrameCanvas), BitmapSource>();
        var actions = new Dictionary<string, LoadedAction>();
        long pixels = 0;
        var frameCount = 0;
        int? width = null, height = null;
        var edgeCanvases = new Dictionary<string, (int Width, int Height, bool Regions)>();
        foreach (var (name, action) in manifest.Actions)
        {
            if (action is null || action.Frames is null || action.Frames.Count == 0
                || (frameCount += action.Frames.Count) > MaxFrameReferences || action.Loop != (name is "idle" or "low" or "sit" or "cloud-idle" || EdgeActions.BaseOf(name) == name))
                throw new InvalidDataException("待机、低额度、坐姿、乘云和贴边姿势必须循环；其他动作必须有限播放，最多 2048 帧引用。");
            var loaded = new List<LoadedFrame>();
            foreach (var frame in action.Frames)
            {
                var minimumDuration = action.SmoothFrames && EdgeActions.BaseOf(name) == "edge-idle" ? 10 : 40;
                if (frame is null || frame.Image is null || !SafeFile(frame.Image) || frame.DurationMs < minimumDuration || frame.DurationMs > 10000)
                    throw new InvalidDataException("动作图片路径或帧时间不合法（普通帧40–10000毫秒；登记平滑侧边帧最低10毫秒）。");
                if (!images.TryGetValue(frame.Image, out var image))
                {
                    using var stream = openImage(frame.Image);
                    using var bytes = new MemoryStream();
                    CopyLimited(stream, bytes, 8 * 1024 * 1024);
                    bytes.Position = 0;
                    var decoder = new PngBitmapDecoder(bytes, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                    var decoded = decoder.Frames[0];
                    if (decoder.Frames.Count != 1 || decoded.PixelWidth > 2048 || decoded.PixelHeight > 2048
                        || (pixels += (long)decoded.PixelWidth * decoded.PixelHeight) > MaxDecodedPixels)
                        throw new InvalidDataException("图片超过大小限制：单帧最大 2048×2048，图集、区域及画布总预算为 384 MiB。");
                    decoded.Freeze();
                    images.Add(frame.Image, image = decoded);
                }
                var side = EdgeActions.BaseOf(name) == "edge-idle";
                var bottom = EdgeActions.BaseOf(name) == "edge-bottom-idle";
                var top = EdgeActions.BaseOf(name) == "edge-top-idle";
                var edge = side || bottom || top;
                var edgeBase = EdgeActions.BaseOf(name)!;
                if (edge && edgeCanvases.TryGetValue(edgeBase, out var prior) && prior.Regions != (frame.Region is not null))
                {
                    throw new InvalidDataException("同方向边缘姿势应统一使用图集区域或完整 PNG，不能混用两种画布。");
                }
                if (frame.EdgeAnchorX is not null && (!side || frame.Region is null)
                    || frame.EdgeAnchorY is not null && (!edge || frame.Region is null)
                    || frame.EdgeAnchorX is double x && (!double.IsFinite(x) || x < 0 || x > 0.5)
                    || frame.EdgeAnchorY is double y && (!double.IsFinite(y) || y < (bottom ? .5 : 0) || y > (top ? .5 : 1)))
                    throw new InvalidDataException("逐帧锚点用于边缘图集：左右 X 为 0–0.5、Y 为 0–1，上沿 Y 为 0–0.5，下沿 Y 为 0.5–1。");
                if (frame.SwingSeatAnchorY is double seatY && (!top || frame.Region is null || manifest.TopSwing is null
                    || !double.IsFinite(seatY) || seatY <= (frame.EdgeAnchorY ?? manifest.EdgeTopAnchorY) + .1 || seatY > .95))
                    throw new InvalidDataException("逐帧坐板锚点需要上沿秋千图集，位于悬挂点下方并不超过画布高度的 95%。");
                if (frame.Region is { } region)
                {
                    if (region.X < 0 || region.Y < 0 || region.Width < 1 || region.Height < 1
                        || (long)region.X + region.Width > image.PixelWidth || (long)region.Y + region.Height > image.PixelHeight)
                        throw new InvalidDataException("图集帧区域必须完整位于原 PNG 内。");
                    var key = (frame.Image.ToLowerInvariant(), region);
                    if (!regions.TryGetValue(key, out var cropped))
                    {
                        // Count both the atlas and each distinct view conservatively; repeated frames share one view.
                        if ((pixels += (long)region.Width * region.Height) > MaxDecodedPixels)
                            throw new InvalidDataException("图集及帧区域合计超过总解码像素限制。");
                        cropped = new CroppedBitmap(image, new Int32Rect(region.X, region.Y, region.Width, region.Height));
                        cropped.Freeze(); regions.Add(key, cropped);
                    }
                    image = cropped;
                }
                if (frame.Canvas is { } registration)
                {
                    if (frame.Region is null || registration.Width < 1 || registration.Height < 1
                        || registration.Width > 2048 || registration.Height > 2048
                        || !double.IsFinite(registration.Scale) || registration.Scale < .05 || registration.Scale > 4
                        || !double.IsFinite(registration.OffsetX) || !double.IsFinite(registration.OffsetY)
                        || registration.OffsetX < 0 || registration.OffsetY < 0
                        || registration.OffsetX + image.PixelWidth * registration.Scale > registration.Width + .000001
                        || registration.OffsetY + image.PixelHeight * registration.Scale > registration.Height + .000001)
                        throw new InvalidDataException("帧画布需要图集区域、合法缩放和完整位于画布内的放置位置。");
                    var key = (frame.Image.ToLowerInvariant(), frame.Region, registration);
                    if (!canvases.TryGetValue(key, out var registered))
                    {
                        if ((pixels += (long)registration.Width * registration.Height) > MaxDecodedPixels)
                            throw new InvalidDataException("图集、裁切区域及登记画布合计超过总解码像素限制。");
                        var visual = new DrawingVisual();
                        using (var drawing = visual.RenderOpen())
                            drawing.DrawImage(image, new Rect(registration.OffsetX, registration.OffsetY,
                                image.PixelWidth * registration.Scale, image.PixelHeight * registration.Scale));
                        var bitmap = new RenderTargetBitmap(registration.Width, registration.Height, 96, 96, PixelFormats.Pbgra32);
                        bitmap.Render(visual); bitmap.Freeze();
                        canvases.Add(key, registered = bitmap);
                    }
                    image = registered;
                }
                if (edge && frame.Region is not null)
                {
                    if (edgeCanvases.TryGetValue(edgeBase,out var canvas) && (image.PixelWidth != canvas.Width || image.PixelHeight != canvas.Height))
                        throw new InvalidDataException("同方向图集的所有姿势帧需要保持相同区域大小。");
                }
                else
                {
                    width ??= image.PixelWidth; height ??= image.PixelHeight;
                    if (image.PixelWidth != width || image.PixelHeight != height)
                        throw new InvalidDataException("同一角色的普通动作帧需要保持相同画布大小。");
                }
                if (edge) edgeCanvases.TryAdd(edgeBase, (image.PixelWidth,image.PixelHeight,frame.Region is not null));
                if (frame.HeadAnchorX.HasValue != frame.HeadAnchorY.HasValue
                    || frame.HeadAnchorX.HasValue && (!(side || top && manifest.TopSwing is not null) || frame.Canvas is null)
                    || frame.HeadAnchorX is double hx && (!double.IsFinite(hx) || hx<0 || hx>1)
                    || frame.HeadAnchorY is double hy && (!double.IsFinite(hy) || hy<0 || hy>1))
                    throw new InvalidDataException("头部配准点需要左右或秋千登记画布、同时提供X/Y并位于画布内。");
                loaded.Add(new(image, frame.DurationMs, frame.EdgeAnchorX, frame.EdgeAnchorY, frame.SwingSeatAnchorY, frame.HeadAnchorX, frame.HeadAnchorY));
            }
            if (action.SmoothFrames && (!(EdgeActions.BaseOf(name) is "edge-idle" or "edge-top-idle")
                || action.Frames.Any(f => f.Canvas is null)
                || loaded[0].EdgeAnchorY is null
                || EdgeActions.BaseOf(name) == "edge-idle" && loaded[0].EdgeAnchorX is null
                || EdgeActions.BaseOf(name) == "edge-top-idle" && (manifest.TopSwing is null || loaded[0].SwingSeatAnchorY is null)
                || loaded.Any(f=>f.HeadAnchorX.HasValue!=loaded[0].HeadAnchorX.HasValue)
                || loaded.Any(f => f.EdgeAnchorX != loaded[0].EdgeAnchorX || f.EdgeAnchorY != loaded[0].EdgeAnchorY
                    || f.SwingSeatAnchorY != loaded[0].SwingSeatAnchorY)))
                throw new InvalidDataException("连续过渡仅用于同一画布、固定双手或座椅锚点的左右贴边和秋千图集。");
            var clip = new LoadedAction(action.Loop, loaded, action.SmoothFrames);
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
                    || (pixels += (long)decoded.PixelWidth * decoded.PixelHeight) > MaxDecodedPixels)
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
