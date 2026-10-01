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
            var legacy = new MainWindow(store, verification: true);
            Check(legacy.Settings.EffectiveCharacterScale == 1.2 && legacy.Settings.EffectiveQuotaScale == 1.2
                && Math.Abs(legacy.PetStage.Width - 230 * 1.2) < 0.01
                && Math.Abs(legacy.QuotaCard.Width - 116 * 1.2) < 0.01,
                "legacy global scale migrates to both independent sizes");
            await legacy.StopAsync();
            legacy.Close();
            var invalidSizes = new Preferences(CharacterScale: double.NaN, QuotaScale: 99).Validated();
            Check(invalidSizes.EffectiveCharacterScale == 1 && invalidSizes.EffectiveQuotaScale == 2,
                "invalid independent sizes recover and clamp");
            Check(new Preferences(Details: (DetailsMode)99).Validated().Details == DetailsMode.Hover,
                "invalid details mode recovers");
            Check(store.Load().QuotaPosition == QuotaDock.Left
                && new Preferences(QuotaPosition: (QuotaDock)99).Validated().QuotaPosition == QuotaDock.Left,
                "legacy and invalid quota positions use left docking");
            Check(store.Load().Character == PetCharacter.Cat
                && new Preferences(Character: (PetCharacter)99).Validated().Character == PetCharacter.Cat,
                "legacy and invalid character preferences retain the cat");
            Check(new Preferences(double.MaxValue, double.NaN, double.PositiveInfinity).Validated() == new Preferences(),
                "invalid coordinates and scale recover");
            store.Save(new Preferences());
            window = new MainWindow(store, verification: true);
            CharacterPackVerification.Run(directory, Check);
            Check(window.AllowsTransparency && window.WindowStyle == WindowStyle.None && !window.ShowInTaskbar,
                "transparent borderless desktop host");
            Check(!window.DetailsVisible && window.Width * window.Height < 348 * 440 / 2,
                "default compact host uses less than half the previous area");
            Check(window.Model.Windows.Single().RemainingText == "—", "unknown quota stays unknown");
            Check(!window.Settings.PositionLocked && !window.Settings.StartWithWindows && window.CanDrag,
                "new interaction settings default to unlocked and no startup");
            window.TogglePositionLock();
            var lockedSize = new Size(window.Width, window.Height);
            window.BeginQuotaDrag(new Point(16, 16));
            Check(!window.CanDrag && store.Load().PositionLocked
                && window.DockTarget.Visibility == Visibility.Collapsed && new Size(window.Width, window.Height) == lockedSize,
                "locked position blocks quota and character drag without changing layout");
            window.SetDetailsMode(DetailsMode.Hover);
            window.PointerChanged(true);
            window.CompleteHoverOpen();
            Check(window.DetailsVisible, "locked position keeps hover details available");
            var lockRestored = new MainWindow(store, verification: true);
            Check(lockRestored.Settings.PositionLocked && !lockRestored.CanDrag, "position lock restores after restart");
            await lockRestored.StopAsync();
            lockRestored.Close();
            window.TogglePositionLock();
            window.BeginQuotaDrag(new Point(16, 16));
            Check(window.DockTarget.Visibility == Visibility.Visible && window.CanDrag, "unlock restores dragging");
            window.TogglePositionLock();
            Check(window.DockTarget.Visibility == Visibility.Collapsed && new Size(window.Width, window.Height) == lockedSize,
                "locking during a quota drag cancels the temporary workspace");
            window.TogglePositionLock();
            window.PointerChanged(false);
            window.CompleteHoverClose();

            var startupMemory = new MemoryStartupStore();
            var startupService = new StartupRegistration(startupMemory, @"C:\CutePet test folder\CutePet.exe", _ => true);
            var startupPreferences = new PreferencesStore(Path.Combine(directory, "startup-settings"));
            var startupWindow = new MainWindow(startupPreferences, verification: true, startup: startupService);
            Check(!startupWindow.Settings.StartWithWindows && startupMemory.Writes == 0,
                "startup remains off until explicitly selected");
            startupWindow.ToggleStartup();
            Check(startupWindow.Settings.StartWithWindows && startupPreferences.Load().StartWithWindows
                && startupMemory.Command == "\"C:\\CutePet test folder\\CutePet.exe\" --autostart",
                "startup switch writes a quoted executable command and records success");
            var startupRestored = new MainWindow(startupPreferences, verification: true, startup: startupService);
            Check(startupRestored.Settings.StartWithWindows, "startup state reads the actual registration on restart");
            await startupRestored.StopAsync();
            startupRestored.Close();
            startupMemory.RejectWrites = true;
            startupWindow.ToggleStartup();
            Check(startupWindow.Settings.StartWithWindows && startupMemory.Command is not null
                && startupWindow.Model.CharacterMessage == "无法修改开机启动项",
                "failed startup removal retains enabled state and reports failure");
            startupMemory.RejectWrites = false;
            startupWindow.ToggleStartup();
            Check(!startupWindow.Settings.StartWithWindows && startupMemory.Command is null
                && !startupPreferences.Load().StartWithWindows, "startup switch removes its own entry");
            startupMemory.RejectWrites = true;
            startupWindow.ToggleStartup();
            Check(!startupWindow.Settings.StartWithWindows && !startupPreferences.Load().StartWithWindows,
                "failed startup enable never reports or persists success");
            startupMemory.RejectWrites = false;
            // External removal is respected: opening a menu must not recreate an entry.
            startupMemory.Command = StartupRegistration.CommandFor(@"C:\previous CutePet\CutePet.exe");
            startupWindow.RefreshStartupState();
            startupMemory.Command = null;
            var writesBeforeRefresh = startupMemory.Writes;
            startupWindow.RefreshStartupState();
            Check(!startupWindow.Settings.StartWithWindows && startupMemory.Writes == writesBeforeRefresh,
                "external startup removal is respected without writing a new entry");
            var invalidCommandRejected = false;
            try { StartupRegistration.CommandFor("C:\\bad\"name\\CutePet.exe"); }
            catch (ArgumentException) { invalidCommandRejected = true; }
            Check(invalidCommandRejected, "startup command rejects embedded quotation marks");
            var missingExecutable = new StartupRegistration(startupMemory, @"C:\missing CutePet\CutePet.exe", _ => false);
            Check(missingExecutable.SetEnabled(true).Error is not null && startupMemory.Command is null,
                "missing executable never creates a broken startup entry");
            var longCommandRejected = false;
            try { StartupRegistration.CommandFor("C:\\" + new string('a', 260) + "\\CutePet.exe"); }
            catch (ArgumentException) { longCommandRejected = true; }
            Check(longCommandRejected, "startup command respects the documented 260-character limit");
            await startupWindow.StopAsync();
            startupWindow.Close();
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
            Check(window.DetailsVisible && window.Height == window.LayoutSize.Height,
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

            foreach (var dock in Enum.GetValues<QuotaDock>())
            {
                window.SetQuotaPosition(dock);
                var layout = DockLayout.For(dock);
                var frame = new Rect(new Point(), layout.Size);
                Check(store.Load().QuotaPosition == dock && frame.Contains(layout.Pet) && frame.Contains(layout.Quota),
                    $"{dock} docking fits its host and persists");
                Render(window, directory, "dock-" + dock.ToString().ToLowerInvariant(), 144);
                window.BeginQuotaDrag(new Point(16, 16));
                var workspace = DockLayout.DragWorkspace(dock);
                var target = DockLayout.Target(dock, workspace.Pet.TopLeft);
                // Position the grabbed card's center on the actual drop target center.
                window.UpdateQuotaDrag(new Point(target.X + target.Width / 2 - window.QuotaCard.Width / 2 + 16,
                    target.Y + target.Height / 2 - window.QuotaCard.Height / 2 + 16));
                window.EndQuotaDrag(cancel: false);
                Check(window.Settings.QuotaPosition == dock && window.Width == layout.Size.Width,
                    $"{dock} drop returns to a compact saved layout");
            }
            window.SetQuotaPosition(QuotaDock.Bottom);
            window.BeginQuotaDrag(new Point(16, 16));
            var dragWorkspace = DockLayout.DragWorkspace(QuotaDock.Bottom);
            var leftTarget = DockLayout.Target(QuotaDock.Left, dragWorkspace.Pet.TopLeft);
            window.UpdateQuotaDrag(new Point(leftTarget.X + leftTarget.Width / 2 - window.QuotaCard.Width / 2 + 16,
                leftTarget.Y + leftTarget.Height / 2 - window.QuotaCard.Height / 2 + 16));
            Render(window, directory, "dock-drag-preview", 144);
            window.PointerChanged(true);
            window.CompleteHoverOpen();
            Check(!window.DetailsVisible && store.Load().QuotaPosition == QuotaDock.Bottom,
                "dragging suspends details and does not save a preview");
            window.EndQuotaDrag(cancel: true);
            Check(window.Settings.QuotaPosition == QuotaDock.Bottom && window.LayoutSize == DockLayout.For(QuotaDock.Bottom).Size,
                "cancelled drag restores the previous docking layout");
            window.BeginQuotaDrag(new Point(16, 16));
            window.UpdateQuotaDrag(new Point(leftTarget.X + leftTarget.Width / 2 - window.QuotaCard.Width / 2 + 16,
                leftTarget.Y + leftTarget.Height / 2 - window.QuotaCard.Height / 2 + 16));
            window.EndQuotaDrag(cancel: false);
            Check(window.Settings.QuotaPosition == QuotaDock.Left && store.Load().QuotaPosition == QuotaDock.Left,
                "dragging from bottom to left commits the selected side");
            var dockRestored = new MainWindow(store, verification: true);
            Check(dockRestored.Settings.QuotaPosition == QuotaDock.Left && dockRestored.Width == window.Width,
                "docking preference restores in a new host");
            await dockRestored.StopAsync();
            dockRestored.Close();
            var previousSettings = window.Settings;
            var previousWidth = window.Width;
            var previousRemaining = window.Model.Windows.First().RemainingText;
            window.SetCharacter(PetCharacter.Tianyi);
            Check(window.SelectedCharacter.Id == "tianyi" && window.CharacterArt.Visibility == Visibility.Visible
                && window.CharacterArt.Source == window.SelectedCharacter.Idle.Frames[0].Image
                && window.SelectedCharacter.Idle.Frames[0].Image.IsFrozen,
                "character selection displays the cached embedded Tianyi sprite");
            Check(window.Settings == previousSettings with { Character = PetCharacter.Tianyi, CharacterPackId = "tianyi" }
                && window.Width == previousWidth && window.Model.Windows.First().RemainingText == previousRemaining,
                "character switching preserves placement, display preferences and quota");
            foreach (var (frame, clip) in window.SelectedCharacter.Actions)
            {
                var image = clip.Frames[0].Image;
                var frameBitmap = new FormatConvertedBitmap(image, PixelFormats.Bgra32, null, 0);
                var framePixels = new byte[image.PixelWidth * image.PixelHeight * 4];
                frameBitmap.CopyPixels(framePixels, image.PixelWidth * 4, 0);
                var transparentFrame = false;
                var visibleFrame = false;
                for (var pixel = 3; pixel < framePixels.Length; pixel += 4)
                { transparentFrame |= framePixels[pixel] == 0; visibleFrame |= framePixels[pixel] == 255; }
                Check(image.IsFrozen && image.PixelWidth == window.SelectedCharacter.Idle.Frames[0].Image.PixelWidth
                    && image.PixelHeight == window.SelectedCharacter.Idle.Frames[0].Image.PixelHeight && transparentFrame && visibleFrame,
                    $"{frame} animation frame is cached, transparent and keeps the original canvas size");
            }
            window.StartCharacterBlink();
            Check(window.CurrentCharacterFrame == CharacterFrame.Closed
                && window.CharacterArt.Source == window.SelectedCharacter.Actions["blink"].Frames[0].Image,
                "Tianyi blink selects the closed-eye frame");
            Render(window, directory, "animation-blink", 144);
            window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(200));
            Check(window.CurrentCharacterFrame == CharacterFrame.Idle, "blink finishes and restores idle");
            window.PlayGreeting();
            Check(window.CurrentCharacterFrame == CharacterFrame.Wave, "click greeting starts the wave frame");
            Render(window, directory, "animation-wave", 144);
            window.AdvanceCharacterAnimation(TimeSpan.FromMilliseconds(180));
            Check(window.CurrentCharacterFrame == CharacterFrame.Idle, "greeting alternates the raised and lowered arm poses");
            window.AdvanceCharacterAnimation(TimeSpan.FromSeconds(2));
            Check(window.CurrentCharacterFrame == CharacterFrame.Idle, "greeting stops without queuing interactions");
            window.PlayGreeting();
            window.StartAnimationClock();
            await Task.Delay(1350);
            Check(window.CurrentCharacterFrame == CharacterFrame.Idle,
                "actual WPF frame timer finishes a greeting without manual stepping");
            window.HidePet();
            window.PlayGreeting();
            await Task.Delay(250);
            Check(window.CurrentCharacterFrame == CharacterFrame.Wave,
                "hidden host stops the actual WPF frame timer");
            window.HidePet();
            window.Model.Apply(Snapshot(8, 48), demo: true);
            Check(window.CurrentCharacterFrame == CharacterFrame.Low && window.CharacterArt.Source == window.SelectedCharacter.Actions["low"].Frames[0].Image,
                "valid low quota selects the tired expression");
            Render(window, directory, "animation-low", 144);
            window.PlayGreeting();
            window.AdvanceCharacterAnimation(TimeSpan.FromSeconds(2));
            Check(window.CurrentCharacterFrame == CharacterFrame.Low, "greeting returns to the current low-quota state");
            window.Model.Failure("读取超时", clear: false);
            Check(window.CurrentCharacterFrame == CharacterFrame.Idle, "stale quota does not drive the tired expression");
            window.Model.Apply(Snapshot(72, 48), demo: true);
            window.PlayGreeting();
            window.HidePet();
            Check(window.CurrentCharacterFrame == CharacterFrame.Idle, "hiding clears transient animation frames");
            window.PlayGreeting();
            window.SetCharacter(PetCharacter.Cat);
            Check(window.CurrentCharacterFrame == CharacterFrame.Idle && window.SelectedCharacter.Id == "cat",
                "character switch clears an unfinished greeting");
            window.Model.Apply(Snapshot(8, 48), demo: true);
            Check(window.CharacterArt.Source == window.SelectedCharacter.Actions["low"].Frames[0].Image, "cat also has a low-quota eye expression");
            window.Model.Apply(Snapshot(72, 48), demo: true);
            Check(window.CharacterArt.Source == window.SelectedCharacter.Idle.Frames[0].Image, "cat returns to its original eyes on quota recovery");
            window.SetCharacter(PetCharacter.Tianyi);
            var sprite = new FormatConvertedBitmap(window.SelectedCharacter.Idle.Frames[0].Image, PixelFormats.Bgra32, null, 0);
            var pixels = new byte[sprite.PixelWidth * sprite.PixelHeight * 4];
            sprite.CopyPixels(pixels, sprite.PixelWidth * 4, 0);
            var transparent = false;
            var opaque = false;
            for (var i = 3; i < pixels.Length; i += 4)
            {
                transparent |= pixels[i] == 0;
                opaque |= pixels[i] == 255;
            }
            Check(transparent && opaque, "Tianyi sprite contains transparent background and visible artwork");
            var characterRestored = new MainWindow(store, verification: true);
            Check(characterRestored.Settings.Character == PetCharacter.Tianyi
                && characterRestored.SelectedCharacter.Id == "tianyi",
                "selected character restores in a new host");
            await characterRestored.StopAsync();
            characterRestored.Close();
            foreach (var dock in Enum.GetValues<QuotaDock>())
            {
                window.SetQuotaPosition(dock);
                Render(window, directory, "tianyi-" + dock.ToString().ToLowerInvariant(), 144);
            }
            window.SetQuotaPosition(QuotaDock.Left);
            window.SetCharacterScale(1.4);
            Render(window, directory, "tianyi-140-percent", 192);
            window.SetCharacterScale(1);
            window.SetCharacter(PetCharacter.Cat);
            Check(window.SelectedCharacter.Id == "cat" && window.CharacterArt.Visibility == Visibility.Visible
                && store.Load().Character == PetCharacter.Cat && window.Width == previousWidth,
                "switching back restores the original cat and persists selection");
            window.Model.Apply(Snapshot(8, 0, expired: true), demo: true);
            Check(window.Model.IsLow && window.Model.Windows.Last().RemainingText == "0%", "low and exhausted quota");
            Check(window.Model.Windows.All(w => w.ResetText == "等待官方额度更新"), "expired reset does not invent restored quota");
            Render(window, directory, "low", 96);
            window.Model.Failure("读取超时 · 稍后重试", clear: false);
            Check(window.Model.IsStale && window.Model.HasData && window.Model.Windows.First().RemainingText == "8%",
                "transient failure marks retained quota stale");
            Check(window.Model.CompactWindows.All(row => row.RemainingText == "—") && window.Model.ShowCharacterMessage,
                "minimal compact display does not present stale percentages as current");
            Render(window, directory, "stale", 96);
            window.Model.Apply(Snapshot(100, 99.9), demo: true);
            Check(window.Model.CompactWindows.First().RemainingText == "100%" && !window.Model.IsStale,
                "fresh sync restores minimal percentages after stale-data suppression");
            Render(window, directory, "full-and-decimal", 144);
            window.Model.Failure("账号发生变化", clear: true);
            Check(!window.Model.HasData && window.Model.Windows.Single().RemainingText == "—",
                "account changes clear quota");
            window.SetCharacterScale(1.2);
            Check(Math.Abs(window.PetStage.Width - 230 * 1.2) < 0.1
                && window.QuotaCard.Width == 116 && store.Load().EffectiveCharacterScale == 1.2
                && store.Load().EffectiveQuotaScale == 1,
                "character size changes without resizing quota and persists");
            window.ToggleTopmost();
            Check(!window.Topmost && !store.Load().AlwaysOnTop, "topmost toggle persists");
            window.SetCharacterScale(1);
            window.Model.Apply(Snapshot(100, 99.9), demo: true);
            window.SetCharacter(PetCharacter.Tianyi);
            window.SetQuotaScale(1.6);
            Check(window.PetStage.Width == 230 && Math.Abs(window.QuotaCard.Width - 116 * 1.6) < 0.01
                && store.Load().EffectiveCharacterScale == 1 && store.Load().EffectiveQuotaScale == 1.6,
                "quota size changes without resizing character and persists");
            var sizeRestored = new MainWindow(store, verification: true);
            Check(sizeRestored.Settings.EffectiveCharacterScale == 1 && sizeRestored.Settings.EffectiveQuotaScale == 1.6
                && sizeRestored.Settings.Character == PetCharacter.Tianyi
                && Math.Abs(sizeRestored.QuotaCard.Width - window.QuotaCard.Width) < 0.01,
                "independent sizes and character restore together");
            await sizeRestored.StopAsync();
            sizeRestored.Close();
            var detailSize = new Size(window.DetailsViewport.Width, window.DetailsViewport.Height);
            foreach (var (characterScale, quotaScale) in new[] { (2.0, 0.8), (0.8, 2.0), (1.4, 1.6) })
            {
                window.SetCharacterScale(characterScale);
                window.SetQuotaScale(quotaScale);
                foreach (var dock in Enum.GetValues<QuotaDock>())
                {
                    window.SetQuotaPosition(dock);
                    var layout = DockLayout.For(dock, characterScale, quotaScale);
                    var frame = new Rect(new Point(), layout.Size);
                    var workspace = DockLayout.DragWorkspace(dock, characterScale, quotaScale);
                    var dragFrame = new Rect(new Point(), workspace.Size);
                    Check(frame.Contains(layout.Pet) && frame.Contains(layout.Quota)
                        && Enum.GetValues<QuotaDock>().All(candidate => dragFrame.Contains(
                            DockLayout.Target(candidate, workspace.Pet.TopLeft, characterScale, quotaScale))),
                        $"independent sizes {characterScale}/{quotaScale} fit {dock} and its drag targets");
                    Render(window, directory, $"sizes-{characterScale:0.0}-{quotaScale:0.0}-{dock}", 144);
                    window.BeginQuotaDrag(new Point(16, 16));
                    var targetDock = dock == QuotaDock.Left ? QuotaDock.Top : QuotaDock.Left;
                    var target = DockLayout.Target(targetDock, workspace.Pet.TopLeft, characterScale, quotaScale);
                    window.UpdateQuotaDrag(new Point(target.X + target.Width / 2 - window.QuotaCard.Width / 2 + 16,
                        target.Y + target.Height / 2 - window.QuotaCard.Height / 2 + 16));
                    window.EndQuotaDrag(cancel: false);
                    Check(window.Settings.QuotaPosition == targetDock
                        && window.Settings.EffectiveCharacterScale == characterScale && window.Settings.EffectiveQuotaScale == quotaScale,
                        $"scaled drag {characterScale}/{quotaScale} from {dock} keeps independent sizes");
                }
            }
            Check(new Size(window.DetailsViewport.Width, window.DetailsViewport.Height) == detailSize,
                "independent sizes keep detail panel readable at its existing size");
            window.SetCharacterScale(1);
            window.SetQuotaScale(1);
            window.SetQuotaPosition(QuotaDock.Left);

            var preserved = window.Settings;
            var custom = window.ImportCharacter(Path.Combine(directory, "character-pack-tests", "animated.zip"));
            Check(window.SelectedCharacter.Id == custom.Id && store.Load().CharacterPackId == custom.Id
                && window.Settings.EffectiveCharacterScale == preserved.EffectiveCharacterScale
                && window.Settings.EffectiveQuotaScale == preserved.EffectiveQuotaScale
                && window.Settings.QuotaPosition == preserved.QuotaPosition && window.Settings.PositionLocked == preserved.PositionLocked,
                "import immediately selects a custom package without disturbing layout or interaction settings");
            Render(window, directory, "custom-character", 144);
            var customRestored = new MainWindow(store, verification: true);
            Check(customRestored.SelectedCharacter.Id == custom.Id && customRestored.CharacterArt.Source.IsFrozen,
                "custom character resources and selection survive a new host");
            await customRestored.StopAsync();
            customRestored.Close();
            var manager = new CharacterManagerWindow(window);
            Check(manager.CharacterList.Items.Count == 3 && manager.RemoveButton.IsEnabled && manager.PreviewButton.IsEnabled,
                "character manager lists both built-ins and the selected custom animated character");
            manager.PreviewButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            Check(manager.Preview.Source == window.SelectedCharacter.Actions["greeting"].Frames[0].Image,
                "manager greeting preview uses the selected package's real frames");
            RenderManager(manager, directory);
            manager.CharacterList.SelectedItem = window.Characters.Find("cat");
            Check(!manager.RemoveButton.IsEnabled && manager.Preview.Source == window.Characters.Find("cat").Idle.Frames[0].Image,
                "manager updates previews and prevents removal of a built-in character");
            manager.UseButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            Check(window.SelectedCharacter.Id == "cat" && store.Load().CharacterPackId == "cat",
                "manager use button applies and persists the chosen character");
            manager.Close();
            custom = window.Characters.Find(custom.Id);
            window.SetCharacterPackage(custom.Id);
            window.RemoveCharacter(custom);
            Check(window.SelectedCharacter.Id == "cat" && store.Load().CharacterPackId == "cat",
                "removing the active custom character safely selects and persists the built-in fallback");

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

    private static void RenderManager(CharacterManagerWindow window, string directory)
    {
        var visual = (FrameworkElement)window.Content;
        visual.Measure(new Size(676, 456));
        visual.Arrange(new Rect(0, 0, 676, 456));
        visual.UpdateLayout();
        var bitmap = new RenderTargetBitmap(1014, 684, 144, 144, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        var encoder = new PngBitmapEncoder();
        encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine(directory, "character-manager.png"));
        encoder.Save(file);
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
