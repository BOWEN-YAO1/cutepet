using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using CutePet.Core;

namespace CutePet.Desktop;

public partial class MainWindow : Window
{
    public const double BaseWidth = 280, BaseHeight = 252;
    public bool DetailsVisible { get; private set; }
    public PetViewModel Model { get; } = new();
    public Preferences Settings { get; private set; }
    private readonly PreferencesStore store;
    private readonly bool verification;
    private readonly CancellationTokenSource stop = new();
    private readonly DispatcherTimer countdown = new() { Interval = TimeSpan.FromSeconds(30) };
    private readonly DispatcherTimer blink = new() { Interval = TimeSpan.FromSeconds(4) };
    private readonly DispatcherTimer openDetails = new() { Interval = TimeSpan.FromMilliseconds(350) };
    private readonly DispatcherTimer closeDetails = new() { Interval = TimeSpan.FromMilliseconds(650) };
    private QuotaSession? session;
    private Task? syncTask;
    private Point? mouseStart;
    private bool dragged, loaded, exiting;
    private bool hovered;
    private int menusOpen;
    public event Action? ExitRequested;

    public MainWindow(PreferencesStore store, bool verification = false)
    {
        this.store = store;
        this.verification = verification;
        Settings = store.Load();
        InitializeComponent();
        DataContext = Model;
        DetailsViewport.DataContext = Model;
        SetScale(Settings.Scale, save: false);
        Topmost = Settings.AlwaysOnTop;
        countdown.Tick += (_, _) => Model.Tick();
        blink.Tick += (_, _) => Blink();
        openDetails.Tick += (_, _) => CompleteHoverOpen();
        closeDetails.Tick += (_, _) => CompleteHoverClose();
        Loaded += OnLoaded;
        IsVisibleChanged += (_, _) =>
        {
            Animate(IsVisible && !verification);
            if (!IsVisible) { hovered = false; StopDetailsTimers(); ShowDetails(false); }
            else ShowDetails(Settings.Details == DetailsMode.Always);
        };
        DpiChanged += (_, _) => Dispatcher.BeginInvoke(() =>
        {
            if (loaded)
            {
                var current = NativePlacement.Get(this);
                NativePlacement.Apply(this, current.Left, current.Top);
                SavePlacement();
                RepositionDetails();
            }
        });
        Scene.ContextMenu = BuildMenu();
        DetailsScene.ContextMenu = BuildMenu();
        ShowDetails(Settings.Details == DetailsMode.Always);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (loaded) return;
        NativePlacement.Apply(this, Settings.Left, Settings.Top);
        loaded = true;
        SavePlacement();
        countdown.Start();
        ShowDetails(Settings.Details == DetailsMode.Always);
        if (!verification) { Animate(true); StartSession(); }
    }

    private void StartSession()
    {
        session = new(Settings.CodexPath);
        session.Loading += () => Dispatch(Model.Loading);
        session.Updated += snapshot => Dispatch(() => Model.Apply(snapshot));
        session.Failed += (message, clear) => Dispatch(() => Model.Failure(message, clear));
        syncTask = session.RunAsync(stop.Token);
    }

    private void Dispatch(Action action)
    {
        if (!stop.IsCancellationRequested) Dispatcher.BeginInvoke(() => { if (!exiting) action(); });
    }

    public void RefreshQuota() => session?.Refresh();

    public void SetScale(double value, bool save = true)
    {
        Settings = (Settings with { Scale = value }).Validated();
        Width = BaseWidth * Settings.Scale;
        Height = BaseHeight * Settings.Scale;
        DetailsViewport.Width = 348 * Settings.Scale;
        DetailsViewport.Height = 256 * Settings.Scale;
        if (loaded)
        {
            UpdateLayout();
            var current = NativePlacement.Get(this);
            NativePlacement.Apply(this, current.Left, current.Top);
            RepositionDetails();
        }
        if (save) SavePlacement();
    }

    public void ToggleTopmost()
    {
        Topmost = !Topmost;
        Settings = Settings with { AlwaysOnTop = Topmost };
        NativePlacement.SetPopupTopmost(DetailsViewport, Topmost);
        SavePlacement();
    }

    public void RestorePet()
    {
        if (exiting) return;
        Show();
        WindowState = WindowState.Normal;
        if (loaded)
        {
            var current = NativePlacement.Get(this);
            NativePlacement.Apply(this, current.Left, current.Top);
        }
        Activate();
    }

    public void HidePet() { StopDetailsTimers(); ShowDetails(false); SavePlacement(); Hide(); }

    public void ResetPosition()
    {
        NativePlacement.Apply(this, null, null);
        RepositionDetails();
        SavePlacement();
    }

    public void SelectCodexPath()
    {
        var picker = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择官方 Codex 原生程序", Filter = "Codex 原生程序 (codex.exe)|codex.exe",
            CheckFileExists = true, Multiselect = false
        };
        if (picker.ShowDialog(this) != true) return;
        Settings = Settings with { CodexPath = picker.FileName };
        SavePlacement();
        Model.CharacterMessage = "已保存，下次启动生效";
    }

    private void SavePlacement()
    {
        if (loaded)
        {
            var point = NativePlacement.Get(this);
            Settings = Settings with { Left = point.Left, Top = point.Top };
        }
        if (!store.Save(Settings)) Model.CharacterMessage = "设置暂时无法保存";
    }

    private ContextMenu BuildMenu()
    {
        var menu = new ContextMenu();
        Add("刷新额度", RefreshQuota);
        var pin = Add("始终置顶", ToggleTopmost);
        pin.IsCheckable = true;
        pin.IsChecked = Topmost;
        var size = new MenuItem { Header = "桌宠大小" };
        foreach (var scale in new[] { 0.8, 1.0, 1.2, 1.4 })
        {
            var item = new MenuItem { Header = $"{scale * 100:0}%", IsCheckable = true,
                IsChecked = Math.Abs(Settings.Scale - scale) < 0.01 };
            item.Click += (_, _) => SetScale(scale);
            size.Items.Add(item);
        }
        menu.Items.Add(size);
        var details = new MenuItem { Header = "详情显示" };
        foreach (var (mode, label) in new[] { (DetailsMode.Hover, "悬停显示"), (DetailsMode.Always, "固定显示"), (DetailsMode.Hidden, "隐藏详情") })
        {
            var item = new MenuItem { Header = label, IsCheckable = true, Tag = mode };
            item.Click += (_, _) => SetDetailsMode(mode);
            details.Items.Add(item);
        }
        menu.Items.Add(details);
        Add("移回屏幕右下角", ResetPosition);
        Add("选择 Codex 程序路径…", SelectCodexPath);
        menu.Items.Add(new Separator());
        Add("隐藏到托盘", HidePet);
        Add("退出 CutePet", () => ExitRequested?.Invoke());
        menu.Opened += (_, _) =>
        {
            BeginDetailsMenu();
            pin.IsChecked = Topmost;
            var index = 0;
            foreach (MenuItem item in size.Items)
                item.IsChecked = Math.Abs(Settings.Scale - new[] { 0.8, 1.0, 1.2, 1.4 }[index++]) < 0.01;
            foreach (MenuItem item in details.Items) item.IsChecked = (DetailsMode)item.Tag == Settings.Details;
        };
        menu.Closed += (_, _) => EndDetailsMenu();
        return menu;
        MenuItem Add(string text, Action action)
        {
            var item = new MenuItem { Header = text };
            item.Click += (_, _) => action();
            menu.Items.Add(item);
            return item;
        }
    }

    private void OnMenu(object sender, RoutedEventArgs e)
    {
        var menu = BuildMenu();
        menu.PlacementTarget = MenuButton;
        menu.IsOpen = true;
    }
    private void OnHide(object sender, RoutedEventArgs e) => HidePet();
    private void OnRefresh(object sender, RoutedEventArgs e) => RefreshQuota();
    private void OnPinDetails(object sender, RoutedEventArgs e) =>
        SetDetailsMode(Settings.Details == DetailsMode.Always ? DetailsMode.Hover : DetailsMode.Always);
    private void OnCollapseDetails(object sender, RoutedEventArgs e)
    {
        SetDetailsMode(DetailsMode.Hover);
        hovered = false;
        ShowDetails(false);
    }

    public void SetDetailsMode(DetailsMode mode)
    {
        Settings = (Settings with { Details = mode }).Validated();
        StopDetailsTimers();
        ShowDetails(Settings.Details == DetailsMode.Always || Settings.Details == DetailsMode.Hover && hovered && !dragged);
        SavePlacement();
    }

    private void OnHoverEnter(object sender, MouseEventArgs e) => PointerChanged(true);
    private void OnHoverLeave(object sender, MouseEventArgs e) => PointerChanged(Scene.IsMouseOver || DetailsViewport.IsMouseOver);
    internal void PointerChanged(bool inside)
    {
        hovered = inside;
        StopDetailsTimers();
        if (Settings.Details != DetailsMode.Hover || dragged || menusOpen > 0 || exiting) return;
        if (inside && !DetailsVisible) openDetails.Start();
        else if (!inside && DetailsVisible) closeDetails.Start();
    }
    internal void CompleteHoverOpen()
    {
        openDetails.Stop();
        if (Settings.Details == DetailsMode.Hover && hovered && !dragged && menusOpen == 0 && !exiting) ShowDetails(true);
    }
    internal void CompleteHoverClose()
    {
        closeDetails.Stop();
        if (Settings.Details == DetailsMode.Hover && !hovered && menusOpen == 0) ShowDetails(false);
    }
    internal void BeginDetailsMenu() { menusOpen++; StopDetailsTimers(); }
    internal void EndDetailsMenu()
    {
        menusOpen = Math.Max(0, menusOpen - 1);
        PointerChanged(Scene.IsMouseOver || DetailsViewport.IsMouseOver);
    }
    private void StopDetailsTimers() { openDetails.Stop(); closeDetails.Stop(); }
    private void ShowDetails(bool visible)
    {
        DetailsVisible = visible;
        PinDetailsButton.Content = Settings.Details == DetailsMode.Always ? "取消固定" : "固定";
        DetailsPopup.IsOpen = visible && IsVisible && !verification;
    }
    private void OnDetailsOpened(object? sender, EventArgs e) => NativePlacement.SetPopupTopmost(DetailsViewport, Topmost);
    private void RepositionDetails()
    {
        if (!DetailsPopup.IsOpen) return;
        DetailsPopup.IsOpen = false;
        DetailsPopup.IsOpen = true;
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        // Let buttons and scroll bars keep their own mouse gestures.
        var target = e.OriginalSource as DependencyObject;
        while (target is not null && target != Scene)
        {
            if (target is System.Windows.Controls.Primitives.ButtonBase or System.Windows.Controls.Primitives.ScrollBar) return;
            target = VisualTreeHelper.GetParent(target);
        }
        mouseStart = e.GetPosition(this);
        dragged = false;
    }
    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (mouseStart is not Point start || e.LeftButton != MouseButtonState.Pressed || dragged) return;
        if ((e.GetPosition(this) - start).Length < 4) return;
        dragged = true;
        StopDetailsTimers();
        ShowDetails(false);
        try { DragMove(); } catch (InvalidOperationException) { }
        finally
        {
            mouseStart = null;
            var current = NativePlacement.Get(this);
            NativePlacement.Apply(this, current.Left, current.Top);
            SavePlacement();
            dragged = false;
            if (Settings.Details == DetailsMode.Always) ShowDetails(true);
            else PointerChanged(Scene.IsMouseOver);
        }
    }
    private async void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        var clicked = mouseStart is not null && !dragged && PetStage.IsMouseOver;
        mouseStart = null;
        if (!clicked) return;
        Model.CharacterMessage = "收到！我会看着的";
        Blink();
        try { await Task.Delay(1800, stop.Token); } catch (OperationCanceledException) { return; }
        Model.RestoreCharacterMessage();
    }

    private void Blink()
    {
        if (!IsVisible || verification) return;
        var animation = new DoubleAnimationUsingKeyFrames { Duration = TimeSpan.FromSeconds(0.2), FillBehavior = FillBehavior.Stop };
        animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(13, KeyTime.FromTimeSpan(TimeSpan.Zero)));
        animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(1, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.06))));
        animation.KeyFrames.Add(new DiscreteDoubleKeyFrame(13, KeyTime.FromTimeSpan(TimeSpan.FromSeconds(0.16))));
        LeftEye.BeginAnimation(HeightProperty, animation);
        RightEye.BeginAnimation(HeightProperty, animation);
    }

    private void Animate(bool active)
    {
        if (active)
        {
            blink.Start();
            Bob.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, -4, TimeSpan.FromSeconds(2.2))
            { AutoReverse = true, RepeatBehavior = RepeatBehavior.Forever, EasingFunction = new SineEase() });
        }
        else
        {
            blink.Stop();
            Bob.BeginAnimation(TranslateTransform.YProperty, null);
            LeftEye.BeginAnimation(HeightProperty, null);
            RightEye.BeginAnimation(HeightProperty, null);
        }
    }

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!exiting) { e.Cancel = true; ExitRequested?.Invoke(); }
        base.OnClosing(e);
    }

    public async Task StopAsync()
    {
        if (exiting) return;
        exiting = true;
        SavePlacement();
        countdown.Stop();
        StopDetailsTimers();
        ShowDetails(false);
        Animate(false);
        stop.Cancel();
        if (syncTask is not null) await syncTask;
        stop.Dispose();
    }
}
