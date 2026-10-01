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
            Check(new Preferences(double.MaxValue, double.NaN, double.PositiveInfinity).Validated() == new Preferences(),
                "invalid coordinates and scale recover");
            store.Save(new Preferences());
            window = new MainWindow(store, verification: true);
            Check(window.AllowsTransparency && window.WindowStyle == WindowStyle.None && !window.ShowInTaskbar,
                "transparent borderless desktop host");
            Check(window.Model.Windows.Single().RemainingText == "—", "unknown quota stays unknown");
            Render(window, directory, "unknown", 96);

            window.Model.Apply(Snapshot(72, 48), demo: true);
            Check(window.Model.Windows.Count == 2 && !window.Model.IsLow && !window.Model.IsStale,
                "two quota windows display");
            Render(window, directory, "normal", 96);
            Render(window, directory, "normal-150dpi", 144);
            Render(window, directory, "normal-200dpi", 192);
            window.Model.Apply(Snapshot(8, 0, expired: true), demo: true);
            Check(window.Model.IsLow && window.Model.Windows.Last().RemainingText == "0%", "low and exhausted quota");
            Check(window.Model.Windows.All(w => w.ResetText == "等待官方额度更新"), "expired reset does not invent restored quota");
            Render(window, directory, "low", 96);
            window.Model.Failure("读取超时 · 稍后重试", clear: false);
            Check(window.Model.IsStale && window.Model.HasData && window.Model.Windows.First().RemainingText == "8%",
                "transient failure marks retained quota stale");
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

    private static void Render(MainWindow window, string directory, string name, double dpi)
    {
        var visual = (FrameworkElement)window.Content;
        visual.Measure(new Size(window.Width, window.Height));
        visual.Arrange(new Rect(0, 0, window.Width, window.Height));
        visual.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(window.Width * dpi / 96),
            (int)Math.Ceiling(window.Height * dpi / 96), dpi, dpi, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine(directory, name + ".png"));
        encoder.Save(file);
    }
}
