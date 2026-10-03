using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CutePet.Desktop;

public partial class ControlCenterWindow : Window
{
    private readonly MainWindow host;
    internal CenterViewModel Model { get; }
    private readonly Dictionary<string, UserControl> pages = new();
    private readonly DispatcherTimer statusClock = new() { Interval = TimeSpan.FromSeconds(1) };
    internal string SelectedPage { get; private set; } = "overview";
    internal bool IsCharactersPage => IsVisible && SelectedPage == "characters";
    internal bool Released { get; private set; }
    internal ControlCenterWindow(MainWindow host, bool verification)
    {
        this.host = host;
        Model = new CenterViewModel(host, () => host.SelectCodexPath(this));
        InitializeComponent(); Icon = AppIcon.WindowIcon; DataContext = Model;
        if (verification) { Opacity = 0; ShowInTaskbar = false; WindowStartupLocation = WindowStartupLocation.Manual; Left = Top = -10000; }
        statusClock.Tick += (_, _) => Model.Refresh();
        IsVisibleChanged += (_, _) => { if (IsVisible && !verification) statusClock.Start(); else statusClock.Stop(); };
        Activated += (_, _) => { host.RefreshStartupState(); Model.Refresh(); };
        if (!verification) Loaded += (_, _) => FitWorkArea();
        Closed += (_, _) =>
        {
            statusClock.Stop(); Model.Dispose();
            foreach (var page in pages.Values) if (page is IDisposable disposable) disposable.Dispose();
            Released = true;
        };
        Navigate("overview");
    }
    internal void Navigate(string page)
    {
        if (page is not ("overview" or "characters" or "activity" or "settings")) page = "overview";
        SelectedPage = page;
        if (!pages.TryGetValue(page, out var view))
        {
            view = page switch { "characters" => new CharactersPage(host), "activity" => new ActivityPage(),
                "settings" => new SettingsPage(), _ => new OverviewPage() };
            pages.Add(page, view);
        }
        PageHost.Content = view;
        (page switch { "characters" => CharactersTab, "activity" => ActivityTab, "settings" => SettingsTab, _ => OverviewTab }).IsChecked = true;
        Model.Refresh();
    }
    private void OnNavigate(object sender, RoutedEventArgs e)
    { if (sender is RadioButton { Tag: string page } && PageHost is not null && SelectedPage != page) Navigate(page); }
    private void FitWorkArea()
    {
        var handle = new System.Windows.Interop.WindowInteropHelper(this).Handle;
        var area = System.Windows.Forms.Screen.FromHandle(handle).WorkingArea;
        var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(this);
        var availableWidth = area.Width / dpi.DpiScaleX;
        var availableHeight = area.Height / dpi.DpiScaleY;
        MinWidth = Math.Min(MinWidth, availableWidth); MinHeight = Math.Min(MinHeight, availableHeight);
        Width = Math.Min(Width, availableWidth); Height = Math.Min(Height, availableHeight);
        UpdateLayout();
        var size = NativePlacement.RoamingBounds(this).Size;
        NativePlacement.Apply(this, area.Left + (area.Width - size.Width) / 2, area.Top + (area.Height - size.Height) / 2);
    }
}
