using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using CutePet.Core;

namespace CutePet.Desktop;

internal static class ControlCenterVerification
{
    internal static async Task RunAsync(MainWindow host, string directory, Action<bool, string> check)
    {
        var trace = new BindingErrors();
        PresentationTraceSources.DataBindingSource.Listeners.Add(trace);
        try
        {
            check(App.ShouldOpenControlCenter(Array.Empty<string>()), "manual application startup opens its main interface");
            check(!App.ShouldOpenControlCenter(new[] {"--autostart"}), "Windows login startup keeps the main interface closed");
            host.WakeCharacterImmediately(); host.SetCharacter(PetCharacter.Tianyi);
            host.SetDetailsMode(DetailsMode.Hidden); host.PointerChanged(false); host.CharacterPointerChanged(false);
            if (host.Settings.PositionLocked) host.TogglePositionLock();
            host.Model.Apply(Snapshot(), demo: true);
            host.OpenControlCenter();
            var center = host.ControlCenter!;
            await Settle();
            check(ReferenceEquals(center.Model.Quota, host.Model) && center.SelectedPage == "overview",
                "control center reuses the desktop's quota model and opens overview");
            check(!center.AllowsTransparency && center.Icon == AppIcon.WindowIcon && !center.Topmost,
                "control center is an ordinary independently focused window with the application icon");
            var overview = (OverviewPage)center.PageHost.Content;
            check(overview.QuotaRows.Items.Count == 2 && overview.QuotaStatus.Text == host.Model.StatusText
                && ReferenceEquals(overview.PetPreview.Source, host.SelectedCharacter.Idle.Frames[0].Image),
                "overview bindings show live quota rows, status and current character artwork");
            Render(center, directory, "app-overview");
            host.OpenControlCenter();
            check(ReferenceEquals(center, host.ControlCenter), "repeated main-interface requests reuse one control center");

            center.Navigate("settings"); await Settle();
            var settings = (SettingsPage)center.PageHost.Content;
            settings.LockSwitch.IsChecked = true; await Settle();
            check(host.Settings.PositionLocked && !center.Model.RoamCommand.CanExecute(null),
                "setting switch updates the real position lock and disables cloud commands");
            host.TogglePositionLock(); await Settle();
            check(settings.LockSwitch.IsChecked == false, "external desktop settings changes flow back into the control center");
            var quotaScale = host.Settings.EffectiveQuotaScale;
            settings.CharacterSize.SelectedValue = 1.4; await Settle();
            check(host.Settings.EffectiveCharacterScale == 1.4 && host.Settings.EffectiveQuotaScale == quotaScale,
                "character size selection saves independently of quota size");
            settings.QuotaSize.SelectedValue = 1.2; await Settle();
            check(host.Settings.EffectiveQuotaScale == 1.2 && host.Settings.EffectiveCharacterScale == 1.4,
                "quota size selection saves independently of character size");
            settings.DetailsChoice.SelectedValue = DetailsMode.Hover; await Settle();
            check(host.Settings.Details == DetailsMode.Hover, "details selector delegates to the existing hover policy");
            settings.StartupSwitch.IsChecked = true; await Settle();
            check(host.Settings.StartWithWindows, "startup switch delegates to the isolated memory registration in verification");
            settings.StartupSwitch.IsChecked = false; await Settle();
            check(!host.Settings.StartWithWindows, "startup switch reflects the actual disabled registration result");
            var topmost = host.Topmost;
            settings.TopmostSwitch.IsChecked = !topmost; await Settle();
            check(host.Topmost == !topmost && host.QuotaHost.Topmost == !topmost && !center.Topmost,
                "topmost setting changes desktop surfaces without pinning the application interface");
            settings.TopmostSwitch.IsChecked = topmost;
            var point = host.QuotaHost.Position;
            center.Model.DockCommand.Execute(QuotaDock.Right); await Settle();
            check(host.Settings.QuotaPosition == QuotaDock.Right && host.QuotaHost.Position != point,
                "explicit quota-position command uses existing independent placement");
            Render(center, directory, "app-settings");

            center.Navigate("characters"); await Settle();
            var characters = (CharactersPage)center.PageHost.Content;
            check(characters.CharacterList.Items.Count == host.Characters.Packs.Count && center.IsCharactersPage,
                "characters page embeds the shared pack library and pauses desktop cloud travel while visible");
            characters.CharacterList.SelectedItem = host.Characters.Find("cat");
            characters.UseButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
            await Settle();
            check(host.SelectedCharacter.Id == "cat" && !center.Model.CanCloud && !characters.RemoveButton.IsEnabled,
                "embedded character selection applies the real built-in pack and protects it from removal");
            center.Navigate("activity"); await Settle();
            var activity = (ActivityPage)center.PageHost.Content;
            check(!activity.RoamButton.IsEnabled && !activity.AutoCloudSwitch.IsEnabled && !activity.RestButton.IsEnabled,
                "unsupported cat actions are visibly disabled in the activity page");
            host.SetCharacter(PetCharacter.Tianyi); await Settle();
            check(activity.RoamButton.IsEnabled && activity.AutoCloudSwitch.IsEnabled && activity.RestButton.IsEnabled,
                "activity capabilities update after an external character change");
            activity.AutoCloudSwitch.IsChecked = true; activity.AutoCloudSwitch.IsChecked = false; await Settle();
            check(!host.Settings.AutoCloud, "activity switch controls the existing persistent automatic-cloud preference");
            center.Model.RoamCommand.Execute(null); center.Model.Refresh(); await Settle();
            check(host.CloudActive && !activity.RoamButton.IsEnabled && center.Model.ActivityText == "正在乘云游动",
                "activity command launches the existing route and reports running state");
            center.Model.RecallCommand.Execute(null); await Settle();
            check(!host.CloudActive && host.LastRecallPosition is not null,
                "activity recall cancels cloud motion using the existing recall operation");
            center.Model.RestCommand.Execute(null); await Settle();
            check(host.CharacterResting && center.Model.RestAction == "起身收起王座",
                "rest action updates its own label to the real standing command");
            center.Model.RestCommand.Execute(null); host.WakeCharacterImmediately(); center.Model.Refresh(); await Settle();
            check(!host.CharacterResting, "second rest command safely resumes the existing standing sequence");
            Render(center, directory, "app-activity");
            center.Navigate("characters"); await Settle();
            characters.CharacterList.SelectedItem = host.Characters.Find("tianyi"); await Settle();
            Render(center, directory, "app-characters");
            center.Navigate("overview"); await Settle();
            host.Model.Loading(); await Settle();
            check(!overview.RefreshButton.IsEnabled, "loading disables refresh through command state notification");
            Render(center, directory, "app-loading");
            host.Model.Apply(Snapshot(), demo: true);
            host.Model.Failure("演示连接失败，请稍后刷新", clear: false); await Settle();
            check(center.Model.DataNote.Contains("上次") && overview.QuotaStatus.Text.Contains("上次")
                && overview.RefreshButton.IsEnabled, "sync failure keeps history visibly labelled and permits retry");
            Render(center, directory, "app-stale");
            host.Model.Failure("演示：未找到 Codex，请在设置中选择程序路径", clear: true); await Settle();
            check(host.Model.Windows.Single().RemainingText == "—" && overview.QuotaRows.Items.Count == 1,
                "unknown quota remains unknown in the main application");
            Render(center, directory, "app-unknown");
            host.Model.Apply(Snapshot(), demo: true);
            center.Navigate("settings"); await Settle();
            center.Width = center.MinWidth; center.Height = center.MinHeight; await Settle();
            Render(center, directory, "app-small");
            check(((FrameworkElement)center.Content).ActualWidth > 700 && !trace.Errors.Any(),
                "all control-center pages render at the minimum size without binding errors");
            var visible = host.IsVisible;
            center.Close(); await Settle();
            check(host.ControlCenter is null && center.Released && host.IsVisible == visible,
                "closing the application interface releases timers and subscriptions while desktop pet keeps running");
            host.OpenControlCenter("activity"); await Settle();
            check(host.ControlCenter != center && host.ControlCenter!.SelectedPage == "activity",
                "closed interface can be reopened directly on a requested feature page");
            host.HidePet(); host.OpenControlCenter(); await Settle();
            check(!host.IsVisible && host.ControlCenter!.IsVisible,
                "opening the main interface works while the independent pet remains hidden");
            host.ControlCenter!.Model.VisibilityCommand.Execute(null); await Settle();
            check(host.IsVisible && host.ControlCenter.Model.VisibilityText == "桌宠正在显示",
                "main-interface show command restores the hidden pet and its status");
            host.ControlCenter.Model.VisibilityCommand.Execute(null); await Settle();
            check(!host.IsVisible && host.ControlCenter.IsVisible,
                "main-interface hide command leaves its own independent window open");
            host.RestorePet();
            host.ControlCenter!.Close();
            var decoder = new IconBitmapDecoder(new Uri("pack://application:,,,/Assets/cutepet.ico"), BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
            check(decoder.Frames.Select(frame => frame.PixelWidth).Order().SequenceEqual(new[] {16,20,24,32,40,48,64,128,256}),
                "application icon provides nine native resolutions for Explorer and high-DPI windows");
            using var trayIcon = AppIcon.CreateTrayIcon();
            check(trayIcon.Width == 32 && trayIcon.Height == 32, "tray uses the same packaged icon without creating a live notification icon");
            host.SetCharacterScale(1); host.SetQuotaScale(1); host.SetDetailsMode(DetailsMode.Hidden);
        }
        finally { PresentationTraceSources.DataBindingSource.Listeners.Remove(trace); host.ControlCenter?.Close(); }
    }
    private static async Task Settle() => await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);
    private static QuotaSnapshot Snapshot()
    {
        var now = DateTimeOffset.UtcNow;
        return new(now,"demo",true,new[] {new QuotaBucket("demo","演示","demo",null,new[] {
            new CutePet.Core.QuotaWindow("primary",28,72,300,now.AddHours(3)),
            new CutePet.Core.QuotaWindow("secondary",52,48,10080,now.AddDays(4))})});
    }
    private static void Render(ControlCenterWindow window, string directory, string name)
    {
        var content = (FrameworkElement)window.Content;
        var bitmap = WindowPreview.Surface(content, content.ActualWidth, content.ActualHeight, 144);
        var encoder = new PngBitmapEncoder(); encoder.Frames.Add(BitmapFrame.Create(bitmap));
        using var file = File.Create(Path.Combine(directory,name+".png")); encoder.Save(file);
    }
    private sealed class BindingErrors : TraceListener
    {
        internal System.Collections.Generic.List<string> Errors { get; } = new();
        public override void Write(string? message) { if (message?.Contains("Error:") == true) Errors.Add(message); }
        public override void WriteLine(string? message) => Write(message);
    }
}
