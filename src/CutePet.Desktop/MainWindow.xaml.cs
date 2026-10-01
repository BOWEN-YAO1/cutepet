using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using CutePet.Core;

namespace CutePet.Desktop;

public partial class MainWindow : Window
{
    public Size LayoutSize => DockLayout.For(Settings.QuotaPosition).Size;
    public static readonly DependencyProperty CompactColumnsProperty = DependencyProperty.Register(
        nameof(CompactColumns), typeof(int), typeof(MainWindow), new PropertyMetadata(1));
    public int CompactColumns { get => (int)GetValue(CompactColumnsProperty); private set => SetValue(CompactColumnsProperty, value); }
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
    private bool quotaGesture, quotaDragging;
    private Point quotaGrabOffset;
    private QuotaDock originalDock, draftDock;
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
        ApplyCharacter();
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
                if (!quotaDragging) { NativePlacement.Apply(this, current.Left, current.Top); SavePlacement(); }
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

    public void SetCharacter(PetCharacter character)
    {
        if (quotaDragging) EndQuotaDrag(cancel: true);
        Settings = (Settings with { Character = character }).Validated();
        ApplyCharacter();
        SavePlacement();
    }

    private void ApplyCharacter()
    {
        var tianyi = Settings.Character == PetCharacter.Tianyi;
        CatArt.Visibility = tianyi ? Visibility.Collapsed : Visibility.Visible;
        TianyiArt.Visibility = tianyi ? Visibility.Visible : Visibility.Collapsed;
        if (tianyi) TianyiArt.Source = CharacterCatalog.Tianyi;
        LeftEye.BeginAnimation(HeightProperty, null);
        RightEye.BeginAnimation(HeightProperty, null);
        PetStage.ToolTip = CharacterCatalog.Label(Settings.Character);
        AutomationProperties.SetName(PetStage, CharacterCatalog.Label(Settings.Character));
    }

    public void SetScale(double value, bool save = true)
    {
        if (quotaDragging) EndQuotaDrag(cancel: true);
        var anchor = PetScreenOrigin();
        Settings = (Settings with { Scale = value }).Validated();
        DetailsViewport.Width = 348 * Settings.Scale;
        DetailsViewport.Height = 256 * Settings.Scale;
        ApplyDockLayout(DockLayout.For(Settings.QuotaPosition), anchor, constrain: true);
        if (save) SavePlacement();
    }

    public void SetQuotaPosition(QuotaDock dock)
    {
        if (quotaDragging) EndQuotaDrag(cancel: true);
        var anchor = PetScreenOrigin();
        Settings = (Settings with { QuotaPosition = dock }).Validated();
        ApplyDockLayout(DockLayout.For(Settings.QuotaPosition), anchor, constrain: true);
        SavePlacement();
    }

    private Point? PetScreenOrigin() => PresentationSource.FromVisual(PetStage) is not null
        ? PetStage.PointToScreen(new Point()) : null;

    private void ApplyDockLayout(DockLayout layout, Point? anchor, bool constrain)
    {
        Scene.Width = layout.Size.Width;
        Scene.Height = layout.Size.Height;
        Width = layout.Size.Width * Settings.Scale;
        Height = layout.Size.Height * Settings.Scale;
        Place(PetStage, layout.Pet);
        Place(QuotaCard, layout.Quota);
        CompactColumns = layout.Columns;
        SpeechBubble.Margin = new Thickness(Settings.QuotaPosition == QuotaDock.Right ? 0 : -26, 4, 0, 0);
        DetailsPopup.Placement = Settings.QuotaPosition switch
        { QuotaDock.Left => PlacementMode.Left, QuotaDock.Right => PlacementMode.Right, _ => PlacementMode.Top };
        UpdateLayout();
        if (anchor is Point physical)
        {
            var dpi = VisualTreeHelper.GetDpi(this);
            var left = physical.X - layout.Pet.X * Settings.Scale * dpi.DpiScaleX;
            var top = physical.Y - layout.Pet.Y * Settings.Scale * dpi.DpiScaleY;
            if (constrain) NativePlacement.Apply(this, left, top);
            else NativePlacement.MoveUnclamped(this, left, top);
        }
        RepositionDetails();
    }
    private static void Place(FrameworkElement element, Rect bounds)
    {
        Canvas.SetLeft(element, bounds.X);
        Canvas.SetTop(element, bounds.Y);
        element.Width = bounds.Width;
        element.Height = bounds.Height;
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

    public void HidePet() { if (quotaDragging) EndQuotaDrag(cancel: true); StopDetailsTimers(); ShowDetails(false); SavePlacement(); Hide(); }

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
        if (quotaDragging) return; // Never persist the temporary drag workspace.
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
        var characters = new MenuItem { Header = "角色选择" };
        foreach (var character in Enum.GetValues<PetCharacter>())
        {
            var item = new MenuItem { Header = CharacterCatalog.Label(character), IsCheckable = true, Tag = character };
            item.Click += (_, _) => SetCharacter(character);
            characters.Items.Add(item);
        }
        menu.Items.Add(characters);
        var position = new MenuItem { Header = "额度条位置" };
        foreach (var dock in Enum.GetValues<QuotaDock>())
        {
            var item = new MenuItem { Header = DockLayout.Label(dock), IsCheckable = true, Tag = dock };
            item.Click += (_, _) => SetQuotaPosition(dock);
            position.Items.Add(item);
        }
        menu.Items.Add(position);
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
            foreach (MenuItem item in position.Items) item.IsChecked = (QuotaDock)item.Tag == Settings.QuotaPosition;
            foreach (MenuItem item in characters.Items) item.IsChecked = (PetCharacter)item.Tag == Settings.Character;
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
        quotaGesture = false;
        while (target is not null && target != Scene)
        {
            if (target is System.Windows.Controls.Primitives.ButtonBase or System.Windows.Controls.Primitives.ScrollBar) return;
            if (target == QuotaCard) quotaGesture = true;
            target = target is Visual ? VisualTreeHelper.GetParent(target) : LogicalTreeHelper.GetParent(target);
        }
        if (target != Scene) return; // Popup controls keep their own input surface.
        mouseStart = e.GetPosition(Scene);
        quotaGrabOffset = e.GetPosition(QuotaCard);
        dragged = false;
    }
    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (quotaDragging)
        {
            if (e.LeftButton == MouseButtonState.Pressed) UpdateQuotaDrag(e.GetPosition(Scene));
            else EndQuotaDrag(cancel: false);
            e.Handled = true;
            return;
        }
        if (mouseStart is not Point start || e.LeftButton != MouseButtonState.Pressed || dragged) return;
        if ((e.GetPosition(Scene) - start).Length < 4) return;
        if (quotaGesture)
        {
            BeginQuotaDrag(quotaGrabOffset);
            if (quotaDragging) UpdateQuotaDrag(e.GetPosition(Scene));
            e.Handled = true;
            return;
        }
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
        if (quotaDragging) { EndQuotaDrag(cancel: false); e.Handled = true; return; }
        var clicked = mouseStart is not null && !dragged && PetStage.IsMouseOver;
        mouseStart = null;
        if (!clicked) return;
        Model.CharacterMessage = "收到！我会看着的";
        Blink();
        try { await Task.Delay(1800, stop.Token); } catch (OperationCanceledException) { return; }
        Model.RestoreCharacterMessage();
    }

    internal void BeginQuotaDrag(Point grabOffset)
    {
        if (quotaDragging || exiting) return;
        var anchor = PetScreenOrigin();
        originalDock = draftDock = Settings.QuotaPosition;
        quotaGrabOffset = grabOffset;
        quotaDragging = dragged = true;
        mouseStart = null;
        StopDetailsTimers();
        ShowDetails(false);
        ApplyDockLayout(DockLayout.DragWorkspace(originalDock), anchor, constrain: false);
        DockTarget.Visibility = DockHint.Visibility = Visibility.Visible;
        ShowDockTarget();
        if (!verification)
        {
            Scene.Focus();
            if (!Scene.CaptureMouse()) EndQuotaDrag(cancel: true);
        }
    }
    internal void UpdateQuotaDrag(Point pointer)
    {
        if (!quotaDragging) return;
        var x = pointer.X - quotaGrabOffset.X;
        var y = pointer.Y - quotaGrabOffset.Y;
        var center = new Point(x + QuotaCard.Width / 2, y + QuotaCard.Height / 2);
        draftDock = DockLayout.Nearest(center, new(Canvas.GetLeft(PetStage), Canvas.GetTop(PetStage)), draftDock);
        Canvas.SetLeft(QuotaCard, Math.Clamp(x, 0, Scene.Width - QuotaCard.Width));
        Canvas.SetTop(QuotaCard, Math.Clamp(y, 0, Scene.Height - QuotaCard.Height));
        ShowDockTarget();
    }
    private void ShowDockTarget()
    {
        var target = DockLayout.Target(draftDock, new(Canvas.GetLeft(PetStage), Canvas.GetTop(PetStage)));
        Place(DockTarget, target);
        DockHintText.Text = "松开 → " + DockLayout.Label(draftDock) + " · Esc 取消";
        Canvas.SetLeft(DockHint, Math.Clamp(target.X, 0, Scene.Width - 150));
        Canvas.SetTop(DockHint, Math.Max(0, target.Y - 27));
    }
    internal void EndQuotaDrag(bool cancel)
    {
        if (!quotaDragging) return;
        var anchor = PetScreenOrigin();
        quotaDragging = dragged = quotaGesture = false;
        if (Scene.IsMouseCaptured) Scene.ReleaseMouseCapture();
        DockTarget.Visibility = DockHint.Visibility = Visibility.Collapsed;
        Settings = Settings with { QuotaPosition = cancel ? originalDock : draftDock };
        ApplyDockLayout(DockLayout.For(Settings.QuotaPosition), anchor, constrain: true);
        SavePlacement();
        if (Settings.Details == DetailsMode.Always) ShowDetails(true);
        else PointerChanged(Scene.IsMouseOver);
    }
    private void OnDockCaptureLost(object sender, MouseEventArgs e)
    {
        if (quotaDragging && !Scene.IsMouseCaptured) EndQuotaDrag(cancel: true);
    }
    private void OnDockKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape && quotaDragging) { EndQuotaDrag(cancel: true); e.Handled = true; }
    }

    private void Blink()
    {
        if (!IsVisible || verification || Settings.Character != PetCharacter.Cat) return;
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
        if (quotaDragging) EndQuotaDrag(cancel: true);
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
