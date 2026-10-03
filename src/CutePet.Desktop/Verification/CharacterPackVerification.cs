using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Windows.Media.Imaging;

namespace CutePet.Desktop;

internal static class CharacterPackVerification
{
    public static void Run(string directory, Action<bool, string> check)
    {
        var area = Path.Combine(directory, "character-pack-tests");
        Directory.CreateDirectory(area);
        var library = new CharacterLibrary(Path.Combine(area, "installed"));
        var cat = library.Find("cat");
        check(library.Packs.Count == 2 && cat.BuiltIn && library.Find("tianyi").Actions.Count == 19 && cat.Actions.Count == 4,
            "both built-in characters load from independent manifests and PNG clips");
        foreach (var builtIn in library.Packs)
        {
            var exportPath = Path.Combine(area, builtIn.Id + ".cutepet.zip");
            library.Export(builtIn, exportPath);
            using var archive = ZipFile.OpenRead(exportPath);
            check(archive.GetEntry("character.json") is not null && archive.GetEntry("idle.png") is not null
                && archive.GetEntry("LICENSE.txt") is not null && archive.GetEntry("SOURCE.md") is not null,
                $"{builtIn.Id} export contains real frames, manifest, source and rights notices");
        }
        var png = Path.Combine(area, "my-character.png");
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(cat.Idle.Frames[0].Image));
        using (var stream = File.Create(png)) encoder.Save(stream);
        var imported = library.Import(png);
        check(!imported.BuiltIn && imported.Name == "my-character" && imported.Actions.Count == 1,
            "single PNG becomes a locally installed static character");
        File.Delete(png);
        library.Reload();
        imported = library.Find(imported.Id);
        check(!imported.BuiltIn && imported.Idle.Frames[0].Image.IsFrozen,
            "PNG import copies resources and survives removal of the original file");
        var player = new CharacterAnimation();
        player.Configure(imported);
        player.Low = true;
        player.Greet();
        player.Blink();
        player.Advance(TimeSpan.FromSeconds(5));
        check(player.Action == "idle" && player.Image == imported.Idle.Frames[0].Image,
            "missing optional actions safely fall back to idle");
        var exported = Path.Combine(area, "static.cutepet.zip");
        library.Export(imported, exported);
        var other = new CharacterLibrary(Path.Combine(area, "other"));
        var roundTrip = other.Import(exported);
        check(roundTrip.Id == imported.Id && roundTrip.Name == imported.Name && roundTrip.Actions.Count == 1,
            "custom export is a portable package and imports into another library");
        var count = other.Packs.Count;
        Reject(() => other.Import(exported), "duplicate IDs cannot overwrite an installed character");
        check(other.Packs.Count == count && !Directory.EnumerateDirectories(other.Root, ".import-*").Any(),
            "failed imports clean their staging directory and keep existing characters");
        other.Remove(roundTrip);
        check(other.Find(roundTrip.Id).Id == "cat" && Directory.EnumerateDirectories(Path.Combine(other.Root, ".removed")).Any(),
            "custom removal preserves an archived copy and resolves missing IDs to the cat");
        Reject(() => library.Remove(cat), "built-in characters cannot be removed");
        var damaged = Path.Combine(library.Root, "broken");
        Directory.CreateDirectory(damaged);
        File.WriteAllText(Path.Combine(damaged, "character.json"), "bad json");
        library.Reload();
        check(library.Warning is not null && library.Find(imported.Id).Id == imported.Id && library.Find("cat").BuiltIn,
            "a corrupt custom package is skipped without losing good packages or built-ins");

        var imageBytes = File.ReadAllBytes(Path.Combine(imported.Directory!, "idle.png"));
        var animated = new CharacterManifest { Id = "test-animation", Name = "动作测试", DisplayWidth = 166,
            Actions = new() {
                ["idle"] = Clip(true, ("idle.png", 120), ("second.png", 80)),
                ["blink"] = Clip(false, ("second.png", 160)),
                ["greeting"] = Clip(false, ("second.png", 240), ("idle.png", 120)),
                ["low"] = Clip(true, ("second.png", 100)) } };
        var animatedZip = Zip("animated", animated, new() { ["idle.png"] = imageBytes, ["second.png"] = imageBytes });
        var animatedPack = library.Import(animatedZip);
        player.Configure(animatedPack);
        var initial = player.Image;
        player.Advance(TimeSpan.FromMilliseconds(130));
        check(player.Image != initial, "idle animation follows per-frame durations from its own package");
        player.Advance(TimeSpan.FromMilliseconds(80));
        check(player.Image == initial, "idle animation loops at the configured clip boundary");
        player.Greet();
        player.Advance(TimeSpan.FromMilliseconds(180));
        check(player.Action == "greeting" && player.Image == animatedPack.Actions["greeting"].Frames[0].Image,
            "greeting duration comes from the package rather than a character-specific constant");
        player.Low = true;
        player.Advance(TimeSpan.FromMilliseconds(300));
        check(player.Action == "low", "a finite greeting returns to the current quota state");
        player.Configure(imported);
        check(player.Action == "idle", "switching packages clears transient and low state");
        player.ReactToClick(1);
        check(player.Action == "idle" && !player.TryAmbient("hover"), "static PNG safely ignores optional new actions");
        player.Configure(cat);
        player.ReactToClick(1);
        check(player.Action == "greeting", "old four-action packs retain their greeting on either click choice");
        var tianyi = library.Find("tianyi");
        using (var edgeArchive = ZipFile.OpenRead(Path.Combine(area, "tianyi.cutepet.zip")))
            check(edgeArchive.GetEntry("edge-v1.png") is not null && edgeArchive.GetEntry("edge-smile-v1.png") is not null,
                "Tianyi export carries both new screen-edge artwork frames");
        var edgeFiles = new Dictionary<string, byte[]>();
        using (var edgeArchive = ZipFile.OpenRead(Path.Combine(area, "tianyi.cutepet.zip")))
            foreach (var entry in edgeArchive.Entries.Where(entry => entry.FullName.EndsWith(".png", StringComparison.OrdinalIgnoreCase)))
            { using var input = entry.Open(); using var bytes = new MemoryStream(); input.CopyTo(bytes); edgeFiles.Add(entry.FullName, bytes.ToArray()); }
        var edgeRoundTrip = other.Import(Zip("edge-roundtrip", tianyi.Manifest with { Id = "edge-roundtrip" }, edgeFiles));
        check(edgeRoundTrip.Actions.ContainsKey("edge-idle") && edgeRoundTrip.Actions.ContainsKey("edge-peek")
            && edgeRoundTrip.Manifest.EdgeAnchorX == 0.06, "edge poses and anchor survive actual package export and import");
        check(edgeRoundTrip.Actions.ContainsKey("edge-top-peek") && edgeRoundTrip.Actions.ContainsKey("edge-bottom-peek")
            && edgeRoundTrip.Manifest.EdgeTopAnchorY == 0 && edgeRoundTrip.Manifest.EdgeBottomAnchorY == 0.9,
            "all vertical actions and contact anchors survive actual package export and import");
        other.Remove(edgeRoundTrip);
        check(edgeRoundTrip.Manifest.TopSwing is { SeatAnchorY: 0.69, SeatHalfWidth: 0.36, RopeColor: "#77B4A8" },
            "swing configuration survives real export and import");
        check(edgeRoundTrip.Manifest.TopSwing?.Ornament is { Image: "rope-ornament-v1.png", DisplayWidth: 18, DisplayHeight: 27 }
            && edgeRoundTrip.SwingOrnamentImage is { IsFrozen: true } && edgeFiles.ContainsKey("rope-ornament-v1.png"),
            "ornament artwork and size survive actual built-in export and package import");
        foreach (var ornament in new[] {
            new CharacterSwingOrnament { Image = "../ornament.png" },
            new CharacterSwingOrnament { Image = "ornament.gif" },
            new CharacterSwingOrnament { Image = "rope-ornament-v1.png", DisplayWidth = 31 },
            new CharacterSwingOrnament { Image = "rope-ornament-v1.png", DisplayWidth = 0 },
            new CharacterSwingOrnament { Image = "rope-ornament-v1.png", DisplayHeight = 45 },
            new CharacterSwingOrnament { Image = "rope-ornament-v1.png", DisplayHeight = 0 },
            new CharacterSwingOrnament { Image = "missing-ornament.png" } })
            Reject(() => library.Import(Zip("ornament-invalid", tianyi.Manifest with { Id = "ornament-invalid",
                TopSwing = tianyi.Manifest.TopSwing! with { Ornament = ornament } }, edgeFiles)),
                "ornament path, size and required artwork are validated " + ornament);
        foreach (var (config, reason) in new[] {
            (new CharacterTopSwing { SeatAnchorY = 0.05 }, "seat above suspension anchor"),
            (new CharacterTopSwing { SeatAnchorY = 1 }, "seat outside canvas"),
            (new CharacterTopSwing { SeatHalfWidth = 0.51 }, "rope span outside canvas"),
            (new CharacterTopSwing { SeatHalfWidth = 0 }, "zero rope span"),
            (new CharacterTopSwing { RopeColor = "Transparent" }, "unsupported rope color") })
            Reject(() => library.Import(Zip("swing-invalid", tianyi.Manifest with { Id = "swing-invalid", TopSwing = config }, edgeFiles)),
                "swing loader rejects " + reason);
        Reject(() => library.Import(Zip("swing-orphan", animated with { Id = "swing-orphan", TopSwing = new() },
            new() { ["idle.png"] = imageBytes, ["second.png"] = imageBytes })), "swing requires its own top base artwork");
        var edgeOnly = animated with { Id = "edge-only", Actions = new() {
            ["idle"] = Clip(true, ("idle.png", 100)), ["edge-idle"] = Clip(true, ("idle.png", 100)) } };
        var edgeCustom = library.Import(Zip("edge-only", edgeOnly, new() { ["idle.png"] = imageBytes }));
        player.Configure(edgeCustom); player.AttachEdge(); player.ReactToClick(0); player.Advance(TimeSpan.FromSeconds(10));
        check(player.Action == "edge-idle", "custom edge base works without optional peek action");
        Reject(() => library.Import(Zip("edge-orphan", animated with { Id = "edge-orphan", Actions = new() {
            ["idle"] = Clip(true, ("idle.png", 100)), ["edge-peek"] = Clip(false, ("idle.png", 100)) } },
            new() { ["idle.png"] = imageBytes })), "edge response requires its own edge base");
        Reject(() => library.Import(Zip("edge-loop", edgeOnly with { Id = "edge-loop", Actions = new() {
            ["idle"] = Clip(true, ("idle.png", 100)), ["edge-idle"] = Clip(false, ("idle.png", 100)) } },
            new() { ["idle.png"] = imageBytes })), "edge base must loop");
        Reject(() => library.Import(Zip("peek-loop", edgeOnly with { Id = "peek-loop", Actions = new() {
            ["idle"] = Clip(true, ("idle.png", 100)), ["edge-idle"] = Clip(true, ("idle.png", 100)),
            ["edge-peek"] = Clip(true, ("idle.png", 100)) } }, new() { ["idle.png"] = imageBytes })), "peek response cannot loop forever");
        Reject(() => library.Import(Zip("edge-anchor", edgeOnly with { Id = "edge-anchor", EdgeAnchorX = 0.6 },
            new() { ["idle.png"] = imageBytes })), "edge anchor cannot hide more than half the artwork canvas");
        foreach (var direction in new[] { "top", "bottom" })
        {
            var idleAction = "edge-" + direction + "-idle";
            var peekAction = "edge-" + direction + "-peek";
            var verticalOnly = animated with { Id = "edge-" + direction + "-only", Actions = new() {
                ["idle"] = Clip(true, ("idle.png", 100)), [idleAction] = Clip(true, ("idle.png", 100)) } };
            var verticalPack = library.Import(Zip(verticalOnly.Id, verticalOnly, new() { ["idle.png"] = imageBytes }));
            player.Configure(verticalPack); player.AttachEdge(idleAction); player.ReactToClick(0);
            check(player.Action == idleAction && EdgeActions.Supported(verticalPack).Count() == 1,
                "vertical-only custom pack requires no side artwork or response " + direction);
            Reject(() => library.Import(Zip("edge-" + direction + "-orphan", verticalOnly with { Id = "edge-" + direction + "-orphan", Actions = new() {
                ["idle"] = Clip(true, ("idle.png", 100)), [peekAction] = Clip(false, ("idle.png", 100)) } },
                new() { ["idle.png"] = imageBytes })), "vertical response needs matching base " + direction);
            Reject(() => library.Import(Zip("edge-" + direction + "-loop", verticalOnly with { Id = "edge-" + direction + "-loop", Actions = new() {
                ["idle"] = Clip(true, ("idle.png", 100)), [idleAction] = Clip(false, ("idle.png", 100)) } },
                new() { ["idle.png"] = imageBytes })), "vertical base must loop " + direction);
            Reject(() => library.Import(Zip("peek-" + direction + "-loop", verticalOnly with { Id = "peek-" + direction + "-loop", Actions = new() {
                ["idle"] = Clip(true, ("idle.png", 100)), [idleAction] = Clip(true, ("idle.png", 100)), [peekAction] = Clip(true, ("idle.png", 100)) } },
                new() { ["idle.png"] = imageBytes })), "vertical response cannot loop " + direction);
        }
        Reject(() => library.Import(Zip("top-anchor", edgeOnly with { Id = "top-anchor", EdgeTopAnchorY = 0.6 },
            new() { ["idle.png"] = imageBytes })), "top contact anchor is bounded");
        Reject(() => library.Import(Zip("bottom-anchor", edgeOnly with { Id = "bottom-anchor", EdgeBottomAnchorY = 0.4 },
            new() { ["idle.png"] = imageBytes })), "bottom contact anchor is bounded");
        check(tianyi.CloudImage is { IsFrozen: true } && cat.CloudImage is null,
            "cloud is a cached optional package layer independent of the character canvas");
        using (var cloudArchive = ZipFile.OpenRead(Path.Combine(area, "tianyi.cutepet.zip")))
            check(cloudArchive.GetEntry("cloud-v1.png") is not null, "built-in export includes the cloud layer");
        player.Configure(tianyi);
        check(player.TryAmbient("look"), "Tianyi can start its package-defined look clip");
        player.Advance(TimeSpan.FromMilliseconds(600));
        check(player.Image == tianyi.Actions["look"].Frames[2].Image, "look clip progresses from left glance to right glance");
        check(!player.TryAmbient("hover") && !tianyi.Actions.ContainsKey("hover"), "Tianyi no longer contains or plays the withdrawn head tilt");
        player.ReactToClick(1);
        check(player.Action == "happy" && !player.TryAmbient("look"), "click happy response takes priority over an ambient look");
        player.Advance(TimeSpan.FromMilliseconds(500));
        player.ReactToClick(1);
        player.Advance(TimeSpan.FromMilliseconds(500));
        check(player.Action == "happy", "repeated click restarts its clip without adding a queue");
        player.Low = true;
        player.Advance(TimeSpan.FromSeconds(1));
        check(player.Action == "low", "happy response finishes at the current low quota base");
        check(!player.TryAmbient("look") && !player.TryAmbient("hover"), "low quota suppresses optional ambient clips");
        player.Low = false;
        player.TryAmbient("look");
        player.Low = true;
        check(player.Action == "low", "entering low quota clears an unfinished ambient response");
        player.ReactToClick(0);
        check(player.Action == "greeting", "explicit click is still allowed while quota is low");
        player.Low = false;
        player.Advance(TimeSpan.FromSeconds(2));
        player.Preview("low");
        check(player.Action == "low", "manager can preview a low clip without an account");
        player.Preview("blink");
        player.Advance(TimeSpan.FromMilliseconds(200));
        check(player.Action == "idle", "previewing another action clears the previous low preview state");
        var extendedActions = new Dictionary<string, CharacterAction>(animated.Actions)
        {
            ["look"] = Clip(false, ("second.png", 300)),
            ["hover"] = Clip(false, ("second.png", 500)),
            ["happy"] = Clip(false, ("second.png", 700))
        };
        var extended = animated with { Id = "extended-actions", Actions = extendedActions };
        var extendedPack = library.Import(Zip("extended", extended, new() { ["idle.png"] = imageBytes, ["second.png"] = imageBytes }));
        check(extendedPack.Actions.Count == 7, "a custom ZIP accepts all three optional action keys");
        player.Configure(extendedPack);
        player.TryAmbient("look");
        check(player.TryAmbient("hover"), "custom hover response still takes priority over a look clip");
        player.ReactToClick(1);
        check(player.Action == "happy" && !player.TryAmbient("hover"), "custom click response still takes priority over hover");
        player.ResetTransient();
        player.TryAmbient("hover");
        player.Low = true;
        check(player.Action == "low", "custom unfinished hover is cleared when quota becomes low");
        var extendedExport = Path.Combine(area, "extended-export.zip");
        library.Export(extendedPack, extendedExport);
        check(new CharacterLibrary(Path.Combine(area, "extended-roundtrip")).Import(extendedExport).Actions.Count == 7,
            "extended actions survive custom package export and reimport");
        var happyOnly = animated with { Id = "happy-only", Actions = new() {
            ["idle"] = Clip(true, ("idle.png", 100)), ["happy"] = Clip(false, ("second.png", 300)) } };
        player.Configure(library.Import(Zip("happy-only", happyOnly, new() { ["idle.png"] = imageBytes, ["second.png"] = imageBytes })));
        player.ReactToClick(0);
        check(player.Action == "happy", "a happy-only custom package responds even when the click choice is zero");
        foreach (var optional in new[] { "look", "hover", "happy" })
            Reject(() => library.Import(Zip("loop-" + optional, animated with { Id = "loop-" + optional, Actions = new() {
                ["idle"] = Clip(true, ("idle.png", 100)), [optional] = Clip(true, ("idle.png", 100)) } },
                new() { ["idle.png"] = imageBytes })), "optional " + optional + " clips cannot loop indefinitely");

        var legacyTianyi = tianyi with { Actions = tianyi.Actions.Where(pair => !pair.Key.StartsWith("sit-", StringComparison.Ordinal))
            .ToDictionary(pair => pair.Key, pair => pair.Value) };
        player.Configure(legacyTianyi);
        check(player.SitDown() && player.Action == "conjure", "sit request begins the package's magic sequence");
        player.Advance(TimeSpan.FromMilliseconds(600));
        check(player.Image == tianyi.Actions["conjure"].Frames[1].Image, "throne materialization follows the configured frame sequence");
        player.Advance(TimeSpan.FromSeconds(1));
        check(player.Action == "sit" && player.Resting, "conjure finishes in a persistent seated pose");
        var seated = player.Image;
        player.Blink();
        check(player.Image == seated && !player.TryAmbient("look") && !player.TryAmbient("hover"),
            "seated pose cannot be interrupted by standing blink or ambient clips");
        player.ReactToClick(1);
        check(player.Action == "stand" && !player.Resting, "click rises before responding rather than replacing the seated pose");
        player.ReactToClick(0);
        player.ReactToClick(1);
        player.Advance(TimeSpan.FromMilliseconds(1100));
        check(player.Action == "happy", "only the latest response is retained during a rise");
        player.Advance(TimeSpan.FromSeconds(2));
        check(player.Action == "idle" && !player.RestPose, "rise and response finish without a queue or residual throne");
        player.SitDown();
        player.Low = true;
        check(player.Action == "low" && !player.Resting && !player.SitDown(), "low quota cancels conjure and prevents another rest");
        player.Low = false;
        player.Preview("sit");
        player.Low = true;
        check(player.Action == "low" && !player.RestPose, "low quota also clears a fully seated pose");
        player.Low = false;
        player.Preview("conjure");
        player.Advance(TimeSpan.FromSeconds(2));
        check(player.Action == "sit", "manager conjure preview reaches the seated base without an account");
        player.Reset();
        check(player.Action == "idle" && !player.RestPose, "lifetime reset clears seated state and pending response");
        player.Configure(cat);
        check(!player.SitDown() && player.Action == "idle", "older cat packages safely ignore unsupported sitting");
        var sitOnly = animated with { Id = "sit-only", Actions = new() {
            ["idle"] = Clip(true, ("idle.png", 100)), ["sit"] = Clip(true, ("second.png", 100)) } };
        var sittingPack = library.Import(Zip("sit-only", sitOnly, new() { ["idle.png"] = imageBytes, ["second.png"] = imageBytes }));
        player.Configure(sittingPack);
        check(player.SitDown() && player.Action == "sit", "custom sit-only package works without transition clips");
        player.ReactToClick(0);
        check(player.Action == "idle", "click safely wakes a sit-only package without greeting or stand clips");
        var sitExport = Path.Combine(area, "sit-export.zip");
        library.Export(sittingPack, sitExport);
        check(new CharacterLibrary(Path.Combine(area, "sit-roundtrip")).Import(sitExport).Actions["sit"].Loop,
            "seated loop survives custom ZIP export and reimport");
        foreach (var transition in new[] { "conjure", "stand", "sit-blink", "sit-greeting", "sit-happy" })
            Reject(() => library.Import(Zip("orphan-" + transition, animated with { Id = "orphan-" + transition,
                Actions = new() { ["idle"] = Clip(true, ("idle.png", 100)), [transition] = Clip(false, ("idle.png", 100)) } },
                new() { ["idle.png"] = imageBytes })), "rest transition " + transition + " requires a seated base");
        Reject(() => library.Import(Zip("rest-no-sit", animated with { Id = "rest-no-sit", RestAfterMs = 30000 },
            new() { ["idle.png"] = imageBytes, ["second.png"] = imageBytes })), "automatic rest requires a seated clip");
        Reject(() => library.Import(Zip("rest-fast", sitOnly with { Id = "rest-fast", RestAfterMs = 1 },
            new() { ["idle.png"] = imageBytes, ["second.png"] = imageBytes })), "automatic rest interval rejects excessive frequency");
        Reject(() => library.Import(Zip("rest-zero", sitOnly with { Id = "rest-zero", RestDurationMs = 0 },
            new() { ["idle.png"] = imageBytes, ["second.png"] = imageBytes })), "automatic seated duration must be bounded and positive");
        Reject(() => library.Import(Zip("sit-finite", sitOnly with { Id = "sit-finite", Actions = new() {
            ["idle"] = Clip(true, ("idle.png", 100)), ["sit"] = Clip(false, ("second.png", 100)) } },
            new() { ["idle.png"] = imageBytes, ["second.png"] = imageBytes })), "sit must remain a looping base pose");
        foreach (var transition in new[] { "conjure", "stand", "sit-blink", "sit-greeting", "sit-happy" })
            Reject(() => library.Import(Zip("loop-" + transition, sitOnly with { Id = "loop-" + transition, Actions = new() {
                ["idle"] = Clip(true, ("idle.png", 100)), ["sit"] = Clip(true, ("second.png", 100)),
                [transition] = Clip(true, ("second.png", 100)) } }, new() { ["idle.png"] = imageBytes, ["second.png"] = imageBytes })),
                "rest transition " + transition + " cannot loop indefinitely");
        var largeEncoder = new PngBitmapEncoder();
        largeEncoder.Frames.Add(BitmapFrame.Create(BitmapSource.Create(2048, 2048, 96, 96,
            System.Windows.Media.PixelFormats.Bgra32, null, new byte[2048 * 2048 * 4], 2048 * 4)));
        using var largeBytes = new MemoryStream();
        largeEncoder.Save(largeBytes);
        var largeFiles = Enumerable.Range(0, 11).ToDictionary(i => "large-" + i + ".png", _ => largeBytes.ToArray());
        Reject(() => library.Import(Zip("ornament-pixels", animated with { Id = "ornament-pixels",
            TopSwing = new() { Ornament = new() { Image = "large-10.png" } }, Actions = new() {
                ["idle"] = Clip(true, Enumerable.Range(0, 10).Select(i => ("large-" + i + ".png", 100)).ToArray()),
                ["edge-top-idle"] = Clip(true, ("large-0.png", 100)) } }, largeFiles)),
            "ornament pixels share the same total decoded memory limit");
        Reject(() => library.Import(Zip("total-pixels", animated with { Id = "total-pixels", Actions = new() {
            ["idle"] = Clip(true, Enumerable.Range(0, 11).Select(i => ("large-" + i + ".png", 100)).ToArray()) } }, largeFiles)),
            "total decoded pixels remain bounded with the expanded rest package limit");

        Reject(() => library.Import(Zip("traversal", animated with { Id = "traversal" }, new() { ["../outside.png"] = imageBytes })),
            "ZIP traversal paths are rejected before installation");
        check(!File.Exists(Path.Combine(library.Root, "outside.png")), "malformed ZIP cannot create a file outside staging");
        Reject(() => library.Import(Zip("script", animated with { Id = "script" }, new() { ["run.exe"] = imageBytes })),
            "character packages cannot carry executable payloads");
        Reject(() => library.Import(Zip("builtin", animated with { Id = "cat" }, new() { ["idle.png"] = imageBytes, ["second.png"] = imageBytes })),
            "import cannot replace a built-in character");
        Reject(() => library.Import(Zip("missing", animated with { Id = "missing" }, new() { ["idle.png"] = imageBytes })),
            "missing animation frames reject the entire import");
        Reject(() => library.Import(Zip("broken-image", animated with { Id = "broken-image" }, new() {
            ["idle.png"] = Encoding.UTF8.GetBytes("not a png"), ["second.png"] = imageBytes })),
            "invalid image content rejects the entire import");
        Reject(() => library.Import(Zip("version", animated with { Id = "version", FormatVersion = 99 }, new() {
            ["idle.png"] = imageBytes, ["second.png"] = imageBytes })), "unsupported package versions are rejected");
        Reject(() => library.Import(Zip("loop", animated with { Id = "loop", Actions = new() {
            ["idle"] = Clip(true, ("idle.png", 100)), ["greeting"] = Clip(true, ("idle.png", 100)) } },
            new() { ["idle.png"] = imageBytes })), "interactive animations cannot loop indefinitely");
        Reject(() => library.Import(Zip("fast", animated with { Id = "fast", Actions = new() {
            ["idle"] = Clip(true, ("idle.png", 1)) } }, new() { ["idle.png"] = imageBytes })),
            "frame durations are bounded to avoid excessively fast animations");
        Reject(() => library.Import(Zip("oversized", animated with { Id = "oversized", DisplayHeight = 999 },
            new() { ["idle.png"] = imageBytes, ["second.png"] = imageBytes })), "package cannot overflow the character display area");
        var wrongCanvas = new PngBitmapEncoder();
        wrongCanvas.Frames.Add(BitmapFrame.Create(BitmapSource.Create(1, 1, 96, 96,
            System.Windows.Media.PixelFormats.Bgra32, null, new byte[] { 0, 0, 0, 0 }, 4)));
        using var canvasBytes = new MemoryStream();
        wrongCanvas.Save(canvasBytes);
        var cloudManifest = new CharacterManifest { Id = "cloud-custom", Name = "Cloud test",
            Cloud = new() { Image = "cloud.png" }, Actions = new() { ["idle"] = Clip(true, ("idle.png", 100)) } };
        var cloudPack = library.Import(Zip("cloud-custom", cloudManifest,
            new() { ["idle.png"] = imageBytes, ["cloud.png"] = canvasBytes.ToArray() }));
        check(cloudPack.CloudImage is { PixelWidth: 1, IsFrozen: true }, "cloud layers may use their own canvas without changing character frames");
        var ornamentPack = library.Import(Zip("ornament-custom", cloudManifest with { Id = "ornament-custom", Cloud = null,
            TopSwing = new() { Ornament = new() { Image = "ornament.png" } }, Actions = new() {
                ["idle"] = Clip(true, ("idle.png", 100)), ["edge-top-idle"] = Clip(true, ("idle.png", 100)) } },
            new() { ["idle.png"] = imageBytes, ["ornament.png"] = canvasBytes.ToArray() }));
        check(ornamentPack.SwingOrnamentImage is { PixelWidth: 1, IsFrozen: true },
            "ornament layer accepts its own independent canvas");
        var cloudExport = Path.Combine(area, "cloud-roundtrip.zip");
        library.Export(cloudPack, cloudExport);
        check(new CharacterLibrary(Path.Combine(area, "cloud-roundtrip")).Import(cloudExport).CloudImage is not null,
            "custom cloud configuration and its image survive ZIP export and import");
        Reject(() => library.Import(Zip("cloud-missing", cloudManifest with { Id = "cloud-missing" },
            new() { ["idle.png"] = imageBytes })), "missing cloud layer rejects the whole import");
        Reject(() => library.Import(Zip("cloud-path", cloudManifest with { Id = "cloud-path", Cloud = new() { Image = "../cloud.png" } },
            new() { ["idle.png"] = imageBytes })), "cloud paths obey the same traversal protection");
        Reject(() => library.Import(Zip("cloud-size", cloudManifest with { Id = "cloud-size", Cloud = new() { Image = "cloud.png", DisplayWidth = 999 } },
            new() { ["idle.png"] = imageBytes, ["cloud.png"] = imageBytes })), "cloud display sizes are bounded");
        Reject(() => library.Import(Zip("cloud-orphan", cloudManifest with { Id = "cloud-orphan", Cloud = null, Actions = new() {
            ["idle"] = Clip(true, ("idle.png", 100)), ["summon-cloud"] = Clip(false, ("idle.png", 100)) } },
            new() { ["idle.png"] = imageBytes })), "cloud summon actions require an optional cloud layer");
        Reject(() => library.Import(Zip("cloud-pixels", cloudManifest with { Id = "cloud-pixels", Cloud = new() { Image = "large-10.png" },
            Actions = new() { ["idle"] = Clip(true, Enumerable.Range(0, 10).Select(i => ("large-" + i + ".png", 100)).ToArray()) } }, largeFiles)),
            "cloud pixels count toward the same total decoded memory limit");
        Reject(() => library.Import(Zip("canvas", animated with { Id = "canvas" }, new() {
            ["idle.png"] = imageBytes, ["second.png"] = canvasBytes.ToArray() })), "inconsistent frame canvases are rejected");
        check(!Directory.EnumerateDirectories(library.Root, ".import-*").Any(), "all malformed imports leave no installed or staging residue");
        check(new Preferences(CharacterPackId: "../outside").Validated().CharacterPackId is null,
            "invalid saved package IDs cannot become file paths");
        check(!CharacterPackLoader.ValidId("cat\n") && !CharacterPackLoader.SafeFile("idle.png\n"),
            "portable IDs and frame paths reject trailing control characters");
        var tooWide = new PngBitmapEncoder();
        tooWide.Frames.Add(BitmapFrame.Create(BitmapSource.Create(2049, 1, 96, 96,
            System.Windows.Media.PixelFormats.Bgra32, null, new byte[2049 * 4], 2049 * 4)));
        using var tooWideBytes = new MemoryStream();
        tooWide.Save(tooWideBytes);
        Reject(() => library.Import(Zip("pixel-limit", animated with { Id = "pixel-limit", Actions = new() {
            ["idle"] = Clip(true, ("idle.png", 100)) } }, new() { ["idle.png"] = tooWideBytes.ToArray() })),
            "PNG pixel dimensions are bounded before a package is installed");

        string Zip(string name, CharacterManifest manifest, Dictionary<string, byte[]> resources)
        {
            var path = Path.Combine(area, name + ".zip");
            using var archive = ZipFile.Open(path, ZipArchiveMode.Create);
            using (var stream = archive.CreateEntry("character.json").Open())
                JsonSerializer.Serialize(stream, manifest, CharacterPackLoader.Json);
            foreach (var (file, content) in resources)
            { using var stream = archive.CreateEntry(file).Open(); stream.Write(content); }
            return path;
        }
        void Reject(Action action, string label)
        {
            var rejected = false;
            try { action(); }
            catch (Exception ex) when (CharacterLibrary.IsPackageError(ex)) { rejected = true; }
            check(rejected, label);
        }
    }
    private static CharacterAction Clip(bool loop, params (string Image, int Duration)[] frames) => new()
    { Loop = loop, Frames = frames.Select(frame => new CharacterActionFrame(frame.Image, frame.Duration)).ToList() };
}
