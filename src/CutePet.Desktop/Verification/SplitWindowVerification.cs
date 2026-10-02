using System;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.Linq;

namespace CutePet.Desktop;

internal static class SplitWindowVerification
{
    [DllImport("user32.dll")] private static extern bool IsWindow(IntPtr handle);
    internal static async Task RunAsync(MainWindow window, string directory, Action<bool, string> check)
    {
        var card = window.QuotaHost;
        var petHandle = new WindowInteropHelper(window).EnsureHandle();
        var cardHandle = new WindowInteropHelper(card).EnsureHandle();
        check(petHandle != IntPtr.Zero && cardHandle != IntPtr.Zero && petHandle != cardHandle,
            "pet and quota are two real independent hidden native windows");
        check(card.AllowsTransparency && !card.ShowInTaskbar && !card.ShowActivated && card.WindowStyle == WindowStyle.None,
            "quota window is transparent, borderless and does not steal activation");
        check(ReferenceEquals(card.DataContext, window.Model) && ReferenceEquals(card.DetailsViewport.DataContext, window.Model),
            "both quota surfaces share the pet's single live model");
        check(card.DetailsPopup.PlacementTarget == card.QuotaScene,
            "details popup is anchored to the quota window instead of the moving pet");
        foreach (var dock in Enum.GetValues<QuotaDock>())
        {
            var old = new Preferences(100, 200, CharacterScale: 1.4, QuotaScale: 1.6, QuotaPosition: dock);
            var layout = DockLayout.For(dock, 1.4, 1.6);
            var migrated = SplitWindowLayout.Migrate(old, 1.5, 2);
            check(migrated.Left == 100 + layout.Pet.X * 1.5 && migrated.Top == 200 + layout.Pet.Y * 2
                && migrated.QuotaLeft == 100 + layout.Quota.X * 1.5 && migrated.QuotaTop == 200 + layout.Quota.Y * 2,
                "legacy " + dock + " coordinates preserve both visual positions across DPI migration");
            check(SplitWindowLayout.Migrate(migrated, 2, 2) == migrated, "coordinate migration runs only once for " + dock);
        }
        var explicitCard = new Preferences(10, 20, QuotaLeft: -500, QuotaTop: 300);
        check(SplitWindowLayout.Migrate(explicitCard, 1, 1).QuotaLeft == -500,
            "migration preserves explicit independent quota coordinates including negative monitors");
        check(new Preferences(QuotaLeft: double.NaN, QuotaTop: double.PositiveInfinity).Validated().QuotaLeft is null
            && new Preferences(QuotaLeft: 1_000_000, QuotaTop: -1_000_000).Validated().QuotaTop is null,
            "independent quota coordinates reject invalid and extreme values");
        window.SetDetailsMode(DetailsMode.Hidden);
        if (window.Settings.PositionLocked) window.TogglePositionLock();
        window.SetCharacter(PetCharacter.Tianyi);
        window.WakeCharacterImmediately();
        card.MoveTo(new(800, 300));
        window.SavePlacement();
        var quotaPosition = card.Position;
        var originalQuotaPixels = QuotaPixels(card);
        window.SummonCloud();
        window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(1200));
        var petX = window.CloudRequestedX;
        window.AdvanceCharacterAnimation(TimeSpan.FromSeconds(4));
        check(window.CloudRequestedX != petX && card.Position == quotaPosition,
            "cloud movement changes only the pet destination and leaves quota stationary");
        check(originalQuotaPixels.SequenceEqual(QuotaPixels(card)),
            "the actual quota surface pixels remain unchanged while the pet moves");
        window.AdvanceCharacterAnimation(TimeSpan.FromSeconds(5));
        check(card.Position == quotaPosition && window.Settings.QuotaLeft == quotaPosition.X,
            "cloud completion saves the independent quota without moving it");
        var quotaSize = new Size(card.Width, card.Height);
        window.SetCharacterScale(1.4);
        check(card.Position == quotaPosition && new Size(card.Width, card.Height) == quotaSize,
            "character scaling preserves the independent quota size and position");
        var petSize = new Size(window.Width, window.Height);
        window.SetQuotaScale(1.2);
        check(new Size(window.Width, window.Height) == petSize && card.Position == quotaPosition,
            "quota scaling preserves the pet size and both window positions");
        var petPosition = NativePlacement.Get(window);
        var quotaDpi = System.Windows.Media.VisualTreeHelper.GetDpi(card);
        window.BeginQuotaDrag(new Point(16, 16));
        window.UpdateQuotaDrag(new Point(1100 + 16 * quotaDpi.DpiScaleX, 700 + 16 * quotaDpi.DpiScaleY));
        window.EndQuotaDrag(false);
        check(NativePlacement.Get(window) == petPosition && card.Position == new Point(1100, 700),
            "free quota drag leaves the real pet window untouched");
        var before = card.Position;
        window.BeginQuotaDrag(new Point(16, 16));
        window.UpdateQuotaDrag(new Point(1216, 816));
        window.TogglePositionLock();
        check(!card.Dragging && card.Position == before, "locking during quota drag restores its original independent position");
        window.TogglePositionLock();
        window.ToggleTopmost();
        check(window.Topmost == card.Topmost, "topmost preference applies to both windows");
        window.ToggleTopmost();
        window.HidePet();
        check(!window.IsVisible && !card.IsVisible && !window.DetailsPopup.IsOpen,
            "hide closes both surfaces and their details");
        window.SetCharacterScale(1);
        window.SetQuotaScale(1);
        window.SetQuotaPosition(QuotaDock.Left);
        RecordSplitPreview(window, directory);
        var store = new PreferencesStore(Path.Combine(directory, "split-lifecycle-settings"));
        var oldPreferences = new Preferences(100, 150, AutoCloud: false, QuotaPosition: QuotaDock.Left);
        store.Save(oldPreferences);
        var lifecycle = new MainWindow(store, verification: true);
        IntPtr lifecycleQuota = IntPtr.Zero;
        try
        {
            lifecycle.RestorePet();
            lifecycleQuota = new WindowInteropHelper(lifecycle.QuotaHost).Handle;
            check(lifecycle.IsVisible && lifecycle.QuotaHost.IsVisible && lifecycle.Opacity == 0 && lifecycle.QuotaHost.Opacity == 0,
                "restoring shows both real windows in isolated invisible offscreen verification");
            var oldLayout = DockLayout.For(QuotaDock.Left);
            var lifecycleDpi = System.Windows.Media.VisualTreeHelper.GetDpi(lifecycle);
            check(lifecycle.Settings.IndependentWindows && store.Load().IndependentWindows
                && lifecycle.QuotaHost.Position.X == 100 + oldLayout.Quota.X * lifecycleDpi.DpiScaleX,
                "actual loaded path migrates and saves the independent quota position");
            lifecycle.HidePet();
            check(!lifecycle.IsVisible && !lifecycle.QuotaHost.IsVisible, "hide hides both real native surfaces");
            lifecycle.RestorePet();
            check(lifecycle.IsVisible && lifecycle.QuotaHost.IsVisible, "restore reopens both surfaces after hiding");
        }
        finally { await lifecycle.StopAsync(); lifecycle.Close(); }
        check(lifecycleQuota != IntPtr.Zero && !IsWindow(lifecycleQuota), "application exit destroys the quota window without leaving an orphan");
    }
    private static void RecordSplitPreview(MainWindow window, string directory)
    {
        var encoder = new System.Windows.Media.Imaging.GifBitmapEncoder();
        var delays = new System.Collections.Generic.List<int>();
        window.SummonCloud();
        window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(1200));
        var start = window.CloudRequestedX;
        for (var i = 0; i <= 40; i++)
        {
            var bitmap = WindowPreview.Capture(window, 96, window.CloudRequestedX - start, 600, 220);
            encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
            delays.Add(20);
            window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(200));
        }
        using var bytes = new MemoryStream();
        encoder.Save(bytes);
        File.WriteAllBytes(Path.Combine(directory, "independent-motion.gif"), ThroneMotionVerification.WithAnimationMetadata(bytes.ToArray(), delays));
        window.WakeCharacterImmediately();
    }
    private static byte[] QuotaPixels(QuotaWindow card)
    {
        var bitmap = WindowPreview.Surface((FrameworkElement)card.Content, card.Width, card.Height, 96);
        var pixels = new byte[bitmap.PixelWidth * bitmap.PixelHeight * 4];
        bitmap.CopyPixels(pixels, bitmap.PixelWidth * 4, 0);
        return pixels;
    }
}
