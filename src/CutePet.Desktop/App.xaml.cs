using System;
using System.Windows;

namespace CutePet.Desktop;

public partial class App : Application
{
    private SingleInstance? instance;
    private TrayController? tray;
    private bool shuttingDown;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        CenterColors.Apply(Resources, new Preferences());
        if (e.Args.Length == 2 && e.Args[0] == "--benchmark-animation")
        {
            AnimationPerformanceVerification.Run(e.Args[1]);
            Shutdown();
            return;
        }
        if (e.Args.Length == 2 && e.Args[0] is "--verify" or "--verify-live")
        {
            var result = await DesktopVerification.RunAsync(e.Args[1], e.Args[0] == "--verify-live");
            Shutdown(result);
            return;
        }
        instance = new SingleInstance("CutePet.Desktop.v1");
        if (!instance.IsFirst) { if (ShouldOpenControlCenter(e.Args)) instance.RequestShow(); Shutdown(); return; }
        var window = new MainWindow(new PreferencesStore());
        MainWindow = window;
        window.ExitRequested += ExitPet;
        tray = new(window, ExitPet);
        instance.Listen(() => Dispatcher.BeginInvoke(() => { window.RestorePet(); window.OpenControlCenter(); }));
        window.Show();
        if (ShouldOpenControlCenter(e.Args)) window.OpenControlCenter();
    }
    internal static bool ShouldOpenControlCenter(string[] args) => Array.IndexOf(args, "--autostart") < 0;

    private async void ExitPet()
    {
        if (shuttingDown) return;
        shuttingDown = true;
        if (MainWindow is MainWindow window) { await window.StopAsync(); window.Close(); }
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        tray?.Dispose();
        instance?.Dispose();
        base.OnExit(e);
    }
}
