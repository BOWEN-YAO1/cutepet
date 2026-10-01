using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using CutePet.Codex;
using CutePet.Core;

namespace CutePet.Desktop;

// Runs the real WPF visual tree without showing a desktop window or changing user preferences.
internal static class DesktopVerification
{
    public static async Task<int> RunAsync(string directory, bool live)
    {
        Directory.CreateDirectory(directory);
        var checks = new List<string>();
        MainWindow? window = null;
        string? failure = null;
        try
        {
            var settingsDirectory = Path.Combine(directory, "isolated-settings");
            var store = new PreferencesStore(settingsDirectory);
            Check(store.Load() == new Preferences(), "missing settings use defaults");
            Check(store.Save(new Preferences(32, 48, 1.2, false)), "settings save");
            Check(store.Load() == new Preferences(32, 48, 1.2, false), "settings round trip");
            File.WriteAllText(Path.Combine(settingsDirectory, "settings.json"), "broken json");
            Check(store.Load() == new Preferences(), "corrupt settings recover");
            File.WriteAllText(Path.Combine(settingsDirectory, "settings.json"), "{\"Scale\":1.2,\"AlwaysOnTop\":false}");
            Check(store.Load().Details == DetailsMode.Hover && store.Load().Scale == 1.2,
                "existing settings default to hover details");
            Check(new Preferences(Details: (DetailsMode)99).Validated().Details == DetailsMode.Hover,
                "invalid details mode recovers");
            Check(new Preferences(double.MaxValue, double.NaN, double.PositiveInfinity).Validated() == new Preferences(),
                "invalid coordinates and scale recover");
            store.Save(new Preferences());
            window = new MainWindow(store, verification: true);
            Check(window.AllowsTransparency && window.WindowStyle == WindowStyle.None && !window.ShowInTaskbar,
                "transparent borderless desktop host");
            Check(!window.DetailsVisible && window.Width * window.Height < 348 * 440 / 2,
                "default compact host uses less than half the previous area");
            Check(window.Model.Windows.Single().RemainingText == "—", "unknown quota stays unknown");
            Render(window, directory, "unknown", 96);

            window.Model.Apply(Snapshot(72, 48), demo: true);
            Check(window.Model.Windows.Count == 2 && !window.Model.IsLow && !window.Model.IsStale,
                "two quota windows display");
            Check(window.Model.CompactWindows.Count() == 2 && !window.Model.ShowCharacterMessage,
                "normal compact display keeps quota and hides idle speech");
            Render(window, directory, "normal", 96);
            Render(window, directory, "normal-150dpi", 144);
            Render(window, directory, "normal-200dpi", 192);
            Render(window, directory, "details", 96, details: true);
            Render(window, directory, "details-150dpi", 144, details: true);

            window.PointerChanged(true);
            Check(!window.DetailsVisible, "hover waits before opening");
            window.PointerChanged(false);
            window.CompleteHoverOpen();
            Check(!window.DetailsVisible, "brief hover cancels pending open");
            window.PointerChanged(true);
            window.CompleteHoverOpen();
            Check(window.DetailsVisible && window.Height == MainWindow.BaseHeight,
                "hover opens details without resizing character host");
            window.PointerChanged(false);
            Check(window.DetailsVisible, "leaving starts a grace period");
            window.PointerChanged(true);
            window.CompleteHoverClose();
            Check(window.DetailsVisible, "entering details cancels pending close");
            window.BeginDetailsMenu();
            window.PointerChanged(false);
            window.CompleteHoverClose();
            Check(window.DetailsVisible, "details stay open during menu interaction");
            window.EndDetailsMenu();
            window.CompleteHoverClose();
            Check(!window.DetailsVisible, "details close after leaving both surfaces");
            window.SetDetailsMode(DetailsMode.Always);
            window.PointerChanged(false);
            window.CompleteHoverClose();
            Check(window.DetailsVisible && store.Load().Details == DetailsMode.Always,
                "pinned details survive pointer leave and persist");
            var restored = new MainWindow(store, verification: true);
            Check(restored.DetailsVisible && restored.Settings.Details == DetailsMode.Always,
                "pinned preference restores in a new host");
            await restored.StopAsync();
            restored.Close();
            window.SetDetailsMode(DetailsMode.Hidden);
            window.PointerChanged(true);
            window.CompleteHoverOpen();
            Check(!window.DetailsVisible && store.Load().Details == DetailsMode.Hidden,
                "hidden details ignore hover and persist");
            window.SetDetailsMode(DetailsMode.Hover);
            window.PointerChanged(false);
            window.CompleteHoverClose();
            window.Model.Apply(Snapshot(8, 0, expired: true), demo: true);
            Check(window.Model.IsLow && window.Model.Windows.Last().RemainingText == "0%", "low and exhausted quota");
            Check(window.Model.Windows.All(w => w.ResetText == "等待官方额度更新"), "expired reset does not invent restored quota");
            Render(window, directory, "low", 96);
            window.Model.Failure("读取超时 · 稍后重试", clear: false);
            Check(window.Model.IsStale && window.Model.HasData && window.Model.Windows.First().RemainingText == "8%",
                "transient failure marks retained quota stale");
            Check(window.Model.CompactStatus == "上次数据" && window.Model.ShowCharacterMessage,
                "compact display keeps stale-data warning visible");
            Render(window, directory, "stale", 96);
            window.Model.Failure("账号发生变化", clear: true);
            Check(!window.Model.HasData && window.Model.Windows.Single().RemainingText == "—",
                "account changes clear quota");
            window.SetScale(1.2);
            Check(Math.Abs(window.Width - MainWindow.BaseWidth * 1.2) < 0.1 && store.Load().Scale == 1.2,
                "scale applies and persists");
            window.ToggleTopmost();
            Check(!window.Topmost && !store.Load().AlwaysOnTop, "topmost toggle persists");
            window.SetScale(1);

            using (var first = new SingleInstance("CutePet.Verify." + Guid.NewGuid().ToString("N")))
            {
                // A second object with the same name is exercised below through a separate shared test name.
                Check(first.IsFirst, "single instance owner");
            }
            var name = "CutePet.Verify." + Guid.NewGuid().ToString("N");
            using (var first = new SingleInstance(name))
            using (var second = new SingleInstance(name))
            {
                Check(first.IsFirst && !second.IsFirst, "duplicate launch detected");
                var signal = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                first.Listen(() => signal.TrySetResult());
                second.RequestShow();
                Check(await Task.WhenAny(signal.Task, Task.Delay(3000)) == signal.Task, "duplicate launch signals existing host");
            }

            if (live)
            {
                await using var reader = await CodexQuotaReader.ConnectAsync();
                var snapshot = await reader.ReadAsync();
                window.Model.Apply(snapshot);
                Check(window.Model.HasData && !window.Model.IsStale, "live official quota reaches desktop model");
                Render(window, directory, "live", 96);
                File.WriteAllText(Path.Combine(directory, "live-snapshot.json"), JsonSerializer.Serialize(snapshot,
                    new JsonSerializerOptions { WriteIndented = true }));
            }
        }
        catch (Exception ex)
        {
            // The verification report is diagnostic; live server messages are already sanitized by the reader.
            failure = ex is QuotaException quota ? $"QuotaFailure: {quota.Failure}" : ex.ToString();
        }
        finally
        {
            if (window is not null) { await window.StopAsync(); window.Close(); }
        }
        File.WriteAllText(Path.Combine(directory, "verification.json"), JsonSerializer.Serialize(
            new { passed = failure is null, live, checks, failure }, new JsonSerializerOptions { WriteIndented = true }));
        return failure is null ? 0 : 1;

        void Check(bool condition, string label)
        {
            if (!condition) throw new InvalidOperationException(label);
            checks.Add(label);
        }
    }

    private static QuotaSnapshot Snapshot(double primary, double secondary, bool expired = false)
    {
        var now = DateTimeOffset.UtcNow;
        return new(now, "demo", true, new[] { new QuotaBucket("demo", "演示", "demo", null,
            new[] { new QuotaWindow("primary", 100 - primary, primary, 300, expired ? now.AddSeconds(-1) : now.AddHours(3)),
                new QuotaWindow("secondary", 100 - secondary, secondary, 10080, expired ? now.AddSeconds(-1) : now.AddDays(4)) }) });
    }

    private static void Render(MainWindow window, string directory, string name, double dpi, bool details = false)
    {
        var visual = details ? window.DetailsViewport : (FrameworkElement)window.Content;
        var width = details ? window.DetailsViewport.Width : window.Width;
        var height = details ? window.DetailsViewport.Height : window.Height;
        visual.Measure(new Size(width, height));
        visual.Arrange(new Rect(0, 0, width, height));
        visual.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(width * dpi / 96),
            (int)Math.Ceiling(height * dpi / 96), dpi, dpi, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine(directory, name + ".png"));
        encoder.Save(file);
    }
}
