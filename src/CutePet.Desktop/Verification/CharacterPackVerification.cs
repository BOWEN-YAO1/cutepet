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
        check(library.Packs.Count == 2 && cat.BuiltIn && library.Find("tianyi").Actions.Count == 6 && cat.Actions.Count == 4,
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
