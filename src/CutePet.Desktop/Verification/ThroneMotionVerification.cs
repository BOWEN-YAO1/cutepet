using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CutePet.Desktop;

internal static class ThroneMotionVerification
{
    internal static void Run(MainWindow window, string directory, Action<bool, string> check)
    {
        var pack = window.Characters.Find("tianyi");
        var player = new CharacterAnimation();
        var lowering = pack.Actions["conjure"];
        check(lowering.Frames.Count == 8 && pack.Actions["stand"].Frames.Count == 9
            && lowering.Duration == 1500 && pack.Actions["stand"].Duration == 1000,
            "throne uses more intermediate poses while preserving total action durations");
        double before = 0;
        for (var i = 0; i < lowering.Frames.Count; i++)
        {
            player.Configure(pack);
            player.SitDown();
            player.Advance(TimeSpan.FromMilliseconds(before + lowering.Frames[i].DurationMs * 0.35));
            var current = player.Image;
            player.StandUp();
            check(player.Action == "stand" && !player.Resting && ReferenceEquals(current, player.Image),
                "reversing throne stage " + i + " retains its current pose");
            player.Advance(TimeSpan.FromSeconds(2));
            check(player.Action == "idle" && !player.RestPose, "reversed throne stage " + i + " completes without residual rest");
            before += lowering.Frames[i].DurationMs;
        }
        player.Configure(pack);
        player.Preview("sit");
        var seated = player.Image;
        player.StandUp();
        check(ReferenceEquals(seated, player.Image), "a seated rise starts from the same seated image");

        var cat = window.Characters.Find("cat");
        var idle = cat.Idle.Frames[0].Image;
        var closed = cat.Actions["blink"].Frames[0].Image;
        var wave = cat.Actions["greeting"].Frames[0].Image;
        var clips = new Dictionary<string, LoadedAction>
        {
            ["idle"] = new(true, new[] { new LoadedFrame(idle, 100) }),
            ["sit"] = new(true, new[] { new LoadedFrame(closed, 100) }),
            ["conjure"] = new(false, new[] { new LoadedFrame(idle, 100), new LoadedFrame(closed, 300) }),
            ["stand"] = new(false, new[] { new LoadedFrame(closed, 500), new LoadedFrame(idle, 200) }),
            ["greeting"] = new(false, new[] { new LoadedFrame(wave, 200) }),
            ["happy"] = new(false, new[] { new LoadedFrame(closed, 200) })
        };
        var custom = cat with { Actions = clips };
        player.Configure(custom);
        player.SitDown();
        player.Advance(TimeSpan.FromMilliseconds(175));
        player.StandUp();
        player.Advance(TimeSpan.FromMilliseconds(324));
        check(player.Action == "stand", "reverse matching accounts for different forward and backward frame durations");
        player.Advance(TimeSpan.FromMilliseconds(1));
        check(player.Action == "idle", "matched reversal finishes at the proportional remaining time");

        var unmatched = custom with { Actions = new Dictionary<string, LoadedAction>(clips)
        { ["stand"] = new(false, new[] { new LoadedFrame(wave, 300) }) } };
        player.Configure(unmatched);
        player.SitDown();
        player.Advance(TimeSpan.FromMilliseconds(175));
        var midway = player.Image;
        player.ReactToClick(0);
        player.ReactToClick(1);
        check(player.Action == "conjure" && ReferenceEquals(midway, player.Image), "unmatched custom transitions finish lowering before rising");
        player.Advance(TimeSpan.FromMilliseconds(225));
        check(player.Action == "stand", "deferred custom rise begins at the configured sitting boundary");
        player.Advance(TimeSpan.FromMilliseconds(300));
        check(player.Action == "happy", "only the latest reply survives a deferred rise");
        player.Configure(unmatched);
        player.SitDown();
        player.Advance(TimeSpan.FromMilliseconds(175));
        player.Greet();
        player.Reset();
        player.Advance(TimeSpan.FromSeconds(1));
        check(player.Action == "idle" && !player.RestPose, "reset cancels a deferred rise and its reply");
        player.Configure(unmatched);
        player.SitDown();
        player.Advance(TimeSpan.FromMilliseconds(175));
        player.Greet();
        player.Low = true;
        player.Advance(TimeSpan.FromSeconds(1));
        check(player.Action == "idle" && !player.RestPose, "low quota cancels deferred custom rest without a delayed rise");
        var noRise = unmatched with { Actions = new Dictionary<string, LoadedAction>(clips) };
        ((Dictionary<string, LoadedAction>)noRise.Actions).Remove("stand");
        player.Configure(noRise);
        player.SitDown();
        player.Greet();
        player.Advance(TimeSpan.FromMilliseconds(400));
        check(player.Action == "greeting" && !player.RestPose, "a legacy conjure-only pack finishes before replying without a rise clip");

        window.SetCharacter(PetCharacter.Tianyi);
        window.SetDetailsMode(DetailsMode.Hidden);
        window.WakeCharacterImmediately();
        window.ToggleCharacterRest();
        window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(830));
        var hostPose = window.CharacterArt.Source;
        window.PlayCharacterInteraction();
        check(window.CurrentCharacterFrame == CharacterFrame.Rise && window.CharacterArt.Source == hostPose,
            "host click reverses the partially lowered pose without an image jump");
        window.WakeCharacterImmediately();
        Record(window, directory);
        var preview = new GifBitmapDecoder(new Uri(Path.Combine(directory, "throne-motion.gif")),
            BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        check(preview.Frames.Count == 19, "animated preview records the actual host's complete rest cycle");
        check((ushort)((BitmapMetadata)preview.Frames[0].Metadata).GetQuery("/grctlext/Delay") == 40,
            "animated preview preserves the intended frame delays");
    }

    private static void Record(MainWindow window, string directory)
    {
        var encoder = new GifBitmapEncoder();
        var delays = new List<int>();
        var oldBackground = window.Scene.Background;
        window.Scene.Background = new SolidColorBrush(Color.FromRgb(239, 245, 244));
        try
        {
            Capture(400);
            window.ToggleCharacterRest();
            foreach (var frame in window.SelectedCharacter.Actions["conjure"].Frames)
            { Capture(frame.DurationMs); window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(frame.DurationMs)); }
            Capture(1500);
            window.ToggleCharacterRest();
            foreach (var frame in window.SelectedCharacter.Actions["stand"].Frames)
            { Capture(frame.DurationMs); window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(frame.DurationMs)); }
            using var encoded = new MemoryStream();
            encoder.Save(encoded);
            // WIC's GIF encoder omits per-frame delays. Write animation metadata without changing pixels.
            File.WriteAllBytes(Path.Combine(directory, "throne-motion.gif"), WithAnimationMetadata(encoded.ToArray(), delays));
        }
        finally { window.Scene.Background = oldBackground; window.WakeCharacterImmediately(); }
        void Capture(int duration)
        {
            var visual = (FrameworkElement)window.Content;
            visual.Measure(new Size(window.Width, window.Height));
            visual.Arrange(new Rect(0, 0, window.Width, window.Height));
            visual.UpdateLayout();
            var bitmap = new RenderTargetBitmap((int)Math.Ceiling(window.Width * 1.5),
                (int)Math.Ceiling(window.Height * 1.5), 144, 144, PixelFormats.Pbgra32);
            bitmap.Render(visual);
            var metadata = new BitmapMetadata("gif");
            metadata.SetQuery("/grctlext/Delay", (ushort)Math.Max(4, duration / 10));
            metadata.SetQuery("/grctlext/Disposal", (byte)2);
            encoder.Frames.Add(BitmapFrame.Create(bitmap, null, metadata, null));
            delays.Add(Math.Max(4, duration / 10));
        }
    }

    private static byte[] WithAnimationMetadata(byte[] data, IReadOnlyList<int> delays)
    {
        var offset = 13 + ((data[10] & 128) == 0 ? 0 : 3 * (1 << ((data[10] & 7) + 1)));
        using var output = new MemoryStream();
        output.Write(data, 0, offset);
        output.Write(new byte[] { 0x21, 0xFF, 11, 78, 69, 84, 83, 67, 65, 80, 69, 50, 46, 48, 3, 1, 0, 0, 0 });
        var frame = 0;
        var hasControl = false;
        while (offset < data.Length)
        {
            var start = offset;
            if (data[offset] == 0x3B) { output.WriteByte(0x3B); break; }
            if (data[offset] == 0x21)
            {
                if (data[offset + 1] == 0xF9)
                {
                    var control = data.AsSpan(offset, 8).ToArray();
                    control[3] = (byte)((control[3] & ~28) | 8);
                    control[4] = (byte)(delays[frame] & 255);
                    control[5] = (byte)(delays[frame] >> 8);
                    output.Write(control);
                    offset += 8;
                    hasControl = true;
                    continue;
                }
                offset += 2;
                SkipBlocks();
            }
            else if (data[offset] == 0x2C)
            {
                if (!hasControl)
                    output.Write(new byte[] { 0x21, 0xF9, 4, 8, (byte)(delays[frame] & 255), (byte)(delays[frame] >> 8), 0, 0 });
                var packed = data[offset + 9];
                offset += 10 + ((packed & 128) == 0 ? 0 : 3 * (1 << ((packed & 7) + 1)));
                offset++; // LZW minimum code size.
                SkipBlocks();
                frame++;
                hasControl = false;
            }
            else throw new InvalidDataException("Unexpected GIF preview block.");
            output.Write(data, start, offset - start);
        }
        if (frame != delays.Count) throw new InvalidDataException("GIF preview frame count changed.");
        return output.ToArray();
        void SkipBlocks()
        { while (data[offset] != 0) offset += data[offset] + 1; offset++; }
    }
}
