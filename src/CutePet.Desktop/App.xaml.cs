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
        if(e.Args.Length==2&&e.Args[0]=="--verify-side-animation")
        {
            try{SideAnimationVerification.Run(e.Args[1]);Shutdown();}
            catch(Exception ex){System.IO.Directory.CreateDirectory(e.Args[1]);System.IO.File.WriteAllText(System.IO.Path.Combine(e.Args[1],"failure.txt"),ex.ToString());Shutdown(1);}
            return;
        }
        if(e.Args.Length==2&&e.Args[0]=="--verify-top-animation")
        {
            try{TopAnimationVerification.Run(e.Args[1]);Shutdown();}
            catch(Exception ex){System.IO.Directory.CreateDirectory(e.Args[1]);System.IO.File.WriteAllText(System.IO.Path.Combine(e.Args[1],"failure.txt"),ex.ToString());Shutdown(1);}
            return;
        }
        if(e.Args.Length==2&&e.Args[0]=="--verify-top-window")
        {
            var directory=e.Args[1];System.IO.Directory.CreateDirectory(directory);
            var host=new MainWindow(new PreferencesStore(System.IO.Path.Combine(directory,"isolated-settings")),verification:true);
            var checks=new System.Collections.Generic.List<string>();
            try
            {
                host.SetCharacter(PetCharacter.Tianyi);
                TopEdgeVerification.Run(host,directory,(passed,label)=>{if(!passed)throw new InvalidOperationException(label);checks.Add(label);});
                SwingVerification.Run(host,directory,(passed,label)=>{if(!passed)throw new InvalidOperationException(label);checks.Add(label);});
                System.IO.File.WriteAllText(System.IO.Path.Combine(directory,"top-window-verification.json"),System.Text.Json.JsonSerializer.Serialize(new{passed=true,checks=checks.Count}));
                await host.StopAsync();host.Close();Shutdown();
            }
            catch(Exception ex){System.IO.File.WriteAllText(System.IO.Path.Combine(directory,"failure.txt"),ex.ToString());await host.StopAsync();host.Close();Shutdown(1);}
            return;
        }
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
