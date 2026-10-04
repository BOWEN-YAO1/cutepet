using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CutePet.Core;

namespace CutePet.Desktop;

internal static class DialogueVerification
{
    internal static async Task RunAsync(string directory, Action<bool, string> check)
    {
        var area = Path.Combine(directory, "dialogue-tests");
        var store = new PreferencesStore(area);
        check(store.Load().SpeechFrequency == DialogueFrequency.Normal && store.Load().ProactiveSpeech,
            "legacy preferences enable normal speech without changing other defaults");
        check(new Preferences(SpeechFrequency: (DialogueFrequency)99).Validated().SpeechFrequency == DialogueFrequency.Normal,
            "invalid speech frequency recovers to normal");
        var engine = new CharacterDialogue(_ => 0);
        engine.Configure(null);
        check(engine.Speak("click") && engine.Visible, "legacy PNG characters use neutral contextual dialogue");
        var first = engine.Text;
        check(!engine.Speak("click") && engine.Text == first, "rapid clicking keeps the existing bubble instead of flickering");
        engine.Advance(TimeSpan.FromSeconds(9), false);
        check(!engine.Visible && engine.Speak("click") && engine.Text != first, "bubble expires and repeated clicks select a different line");
        engine.Proactive = false; engine.Advance(TimeSpan.FromMinutes(5), true);
        check(!engine.Speak("ambient", proactive: true) && engine.Speak("click"), "proactive off still allows deliberate interaction");
        check(engine.Speak("low", important: true) && engine.Important, "quota transition interrupts ordinary interaction");
        var important = engine.Text; engine.Advance(TimeSpan.FromSeconds(5), true);
        check(!engine.Speak("click") && engine.Text == important, "ordinary speech cannot replace a still visible quota warning");
        check(engine.Speak("offline", important: true), "a new important event can replace the earlier notification");
        engine.Advance(TimeSpan.FromSeconds(3), true);
        check(engine.Visible, "new notification owns a fresh lifetime without an old delayed callback");
        engine.Advance(TimeSpan.FromSeconds(6), true);
        check(!engine.Visible, "important bubbles also close automatically");
        foreach (var frequency in Enum.GetValues<DialogueFrequency>())
        {
            engine.Configure(null); engine.Proactive = true; engine.Frequency = frequency;
            engine.Advance(TimeSpan.FromSeconds(engine.ProactiveInterval - .1), true);
            check(!engine.Speak("ambient", proactive: true), $"{frequency} respects proactive cooldown");
            engine.Advance(TimeSpan.FromSeconds(.2), false);
            check(!engine.Speak("ambient", proactive: true), $"{frequency} pauses proactive clock during menus or dragging");
            engine.Advance(TimeSpan.FromSeconds(.2), true);
            check(engine.Speak("ambient", proactive: true), $"{frequency} resumes proactive speech at its own interval");
        }
        engine.Configure(new() { ["click"] = new[] { "一", "二", "三" } });
        string? prior = null;
        for (var i = 0; i < 12; i++)
        {
            engine.Advance(TimeSpan.FromSeconds(9), false); engine.Speak("click");
            check(engine.Text != prior, "dialogue history prevents adjacent repetitions " + i); prior = engine.Text;
        }
        engine.Configure(new() { ["click"] = new[] { "单句" } });
        check(!engine.Visible && engine.Speak("click") && engine.Text == "单句", "switching packs clears history and single-line groups remain usable");
        Reject(new() { ["unknown"] = new[] { "hi" } }, "unknown dialogue event");
        Reject(new() { ["click"] = null! }, "null group");
        Reject(new() { ["click"] = new string[] { null! } }, "null line");
        Reject(new() { ["click"] = Array.Empty<string>() }, "empty group");
        Reject(new() { ["click"] = new[] { "  " } }, "blank line");
        Reject(new() { ["click"] = new[] { "a\nb" } }, "control characters");
        Reject(new() { ["click"] = new[] { new string('a', 81) } }, "oversized line");
        Reject(new() { ["click"] = Enumerable.Repeat("hi", 13).ToArray() }, "oversized group");
        Reject(CharacterDialogue.Events.ToDictionary(key => key, _ => Enumerable.Repeat("hi", 12).ToArray()), "oversized total");

        var window = new MainWindow(store, verification: true);
        try
        {
            window.Show(); await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            window.SetProactiveSpeech(false);
            foreach (var (id, expected) in new[] { ("cat", 32), ("tianyi", 60) })
            {
                window.SetCharacterPackage(id);
                var pack = window.SelectedCharacter;
                check(pack.Manifest.Dialogue!.Values.Sum(lines => lines.Length) == expected && pack.Manifest.Dialogue.Count == 16,
                    $"{id} owns {expected} contextual lines in its character package");
                var zip = Path.Combine(area, id + "-dialogue.cutepet.zip");
                window.Characters.Export(pack, zip);
                using (var archive = ZipFile.Open(zip, ZipArchiveMode.Update))
                {
                    archive.GetEntry("character.json")!.Delete();
                    using var stream = archive.CreateEntry("character.json").Open();
                    JsonSerializer.Serialize(stream, pack.Manifest with { Id = "dialogue-" + id }, CharacterPackLoader.Json);
                }
                var imported = window.Characters.Import(zip);
                check(JsonSerializer.Serialize(imported.Manifest.Dialogue) == JsonSerializer.Serialize(pack.Manifest.Dialogue),
                    $"{id} export/import preserves all dialogue groups");
                window.PlayCharacterInteraction();
                check(window.Model.SpeechVisible && pack.Manifest.Dialogue["click"].Contains(window.Model.SpeechText),
                    $"{id} click uses selected character dialogue");
                window.Dialogue.Clear();
            }
            window.Dialogue.Configure(); window.Dialogue.Startup(8);
            check(window.SelectedCharacter.Manifest.Dialogue!["morning"].Contains(window.Model.SpeechText), "morning startup uses local-time greeting");
            window.Dialogue.Configure(); window.Dialogue.Startup(22);
            check(window.SelectedCharacter.Manifest.Dialogue!["evening"].Contains(window.Model.SpeechText), "evening startup uses local-time greeting");
            window.Dialogue.Configure();
            window.Model.Apply(Snapshot(72), demo: true);
            check(!window.Model.SpeechVisible, "healthy quota refresh does not generate unsolicited speech");
            window.Model.Apply(Snapshot(8), demo: true);
            check(window.SelectedCharacter.Manifest.Dialogue!["low"].Contains(window.Model.SpeechText), "low quota emits one important contextual warning");
            window.Dialogue.Advance(TimeSpan.FromSeconds(9));
            window.Model.Apply(Snapshot(8), demo: true);
            check(!window.Model.SpeechVisible, "repeated low quota refresh does not repeat its warning");
            window.Model.Failure("演示离线", clear: false);
            check(window.SelectedCharacter.Manifest.Dialogue!["offline"].Contains(window.Model.SpeechText), "connection loss emits once");
            window.Dialogue.Advance(TimeSpan.FromSeconds(9)); window.Model.Failure("演示离线", clear: false);
            check(!window.Model.SpeechVisible, "repeated failures remain quiet");
            window.Model.Apply(Snapshot(72), demo: true);
            check(window.SelectedCharacter.Manifest.Dialogue!["recovery"].Contains(window.Model.SpeechText), "fresh data after a failure emits a recovery line");
            window.Dialogue.Advance(TimeSpan.FromSeconds(9)); window.Model.Apply(Snapshot(72), demo: true);
            check(!window.Model.SpeechVisible, "subsequent fresh snapshots remain quiet");
            window.SetSpeechFrequency(DialogueFrequency.Lively);
            check(store.Load().SpeechFrequency == DialogueFrequency.Lively && !store.Load().ProactiveSpeech,
                "speech frequency and proactive toggle persist together");
            window.SummonCloud();
            check(window.CloudActive && window.SelectedCharacter.Manifest.Dialogue!["cloud"].Contains(window.Model.SpeechText), "manual cloud trip uses cloud dialogue");
            window.WakeCharacterImmediately(); window.Dialogue.Configure(); window.ToggleCharacterRest();
            check(window.SelectedCharacter.Manifest.Dialogue!["rest"].Contains(window.Model.SpeechText), "summoning throne uses rest dialogue");
            window.WakeCharacterImmediately();
            var bounds = new Rect(-1920, -1080, 1920, 1040); var size = new Size(230, 178);
            foreach (var (point, context, placement) in new[]
            {
                (new Point(-1918,-500), "edge-side", PlacementMode.Right),
                (new Point(-232,-500), "edge-side", PlacementMode.Left),
                (new Point(-900,-1078), "edge-top", PlacementMode.Bottom),
                (new Point(-900,-220), "edge-bottom", PlacementMode.Top)
            })
            {
                window.WakeCharacterImmediately(); window.Dialogue.Configure();
                check(window.CompletePetDrag(bounds, size, point, 1), context + " attaches on a negative-coordinate monitor");
                check(window.SelectedCharacter.Manifest.Dialogue![context].Contains(window.Model.SpeechText)
                    && window.SpeechPopup.Placement == placement && ReferenceEquals(window.SpeechPopup.PlacementTarget, window.PetStage),
                    context + " bubble is placed inward and anchored to pet rather than quota");
            }
            check(!window.SpeechPopup.IsOpen, "verification never opens a native speech popup");
            var quotaPosition = window.QuotaHost.Position;
            window.WakeCharacterImmediately(); window.Dialogue.Configure(); window.Speak("click");
            await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
            check(((TextBlock)window.SpeechBubble.Child).Text == window.Model.SpeechText && window.QuotaHost.Position == quotaPosition,
                "actual speech binding updates without moving quota");
            Render(window, directory);
            window.SetCharacter(PetCharacter.Cat);
            check(!window.Model.SpeechVisible, "switching character removes the previous character's bubble");
            window.Speak("click"); window.HidePet();
            check(!window.Model.SpeechVisible && !window.SpeechPopup.IsOpen, "hiding pet clears text and closes its popup");
            window.Speak("low");
            check(!window.Model.SpeechVisible, "hidden pet does not queue notifications for later display");
        }
        finally { await window.StopAsync(); window.Close(); }

        void Reject(Dictionary<string, string[]> invalid, string reason)
        {
            var rejected = false;
            try
            {
                var manifest = new CharacterManifest { Id = "test", Name = "测试", Dialogue = invalid };
                using var stream = new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(manifest, CharacterPackLoader.Json));
                CharacterPackLoader.Load(stream, _ => throw new Exception("must reject before loading artwork"), false);
            }
            catch (InvalidDataException) { rejected = true; }
            check(rejected, "package loader rejects " + reason);
        }
    }
    private static QuotaSnapshot Snapshot(double remaining) => new(DateTimeOffset.UtcNow, "demo", true,
        new[] { new QuotaBucket("demo", "演示", "demo", null, new[] { new CutePet.Core.QuotaWindow("primary", 100-remaining, remaining, 300, DateTimeOffset.UtcNow.AddHours(3)) }) });
    private static void Render(MainWindow window, string directory)
    {
        var bubble = window.SpeechBubble;
        bubble.Measure(new Size(190, 200)); bubble.Arrange(new Rect(bubble.DesiredSize)); bubble.UpdateLayout();
        var speech = WindowPreview.Surface(bubble, bubble.ActualWidth, bubble.ActualHeight, 144);
        var pet = WindowPreview.Capture(window, 144);
        var drawing = new DrawingVisual();
        using (var dc = drawing.RenderOpen())
        {
            dc.DrawRoundedRectangle(new SolidColorBrush(Color.FromRgb(245,237,241)), null, new Rect(0,0,300,280), 16,16);
            dc.DrawImage(speech, new Rect((300-bubble.ActualWidth)/2,18,bubble.ActualWidth,bubble.ActualHeight));
            dc.DrawImage(pet, new Rect(35,90,230,178));
        }
        var bitmap = new RenderTargetBitmap(450,420,144,144,PixelFormats.Pbgra32); bitmap.Render(drawing);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine(directory,"dialogue-preview.png")); encoder.Save(file);
    }
}
