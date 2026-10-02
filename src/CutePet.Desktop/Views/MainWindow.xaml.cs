using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace CutePet.Desktop;

public partial class MainWindow : Window
{
    internal DockLayout CurrentLayout => DockLayout.For(Settings.QuotaPosition, Settings.EffectiveCharacterScale, Settings.EffectiveQuotaScale);
    public Size LayoutSize => CurrentLayout.Size;
    public static readonly DependencyProperty CompactColumnsProperty = DependencyProperty.Register(
        nameof(CompactColumns), typeof(int), typeof(MainWindow), new PropertyMetadata(1));
    public int CompactColumns { get => (int)GetValue(CompactColumnsProperty); private set => SetValue(CompactColumnsProperty, value); }
    public bool DetailsVisible => details.DetailsVisible;
    public PetViewModel Model { get; } = new();
    public Preferences Settings { get; private set; }
    private readonly PreferencesStore store;
    internal CharacterLibrary Characters { get; }
    internal CharacterPack SelectedCharacter { get; private set; } = null!;
    private readonly bool verification;
    private readonly IStartupRegistration startup;
    internal bool CanDrag => !Settings.PositionLocked && !exiting;
    private readonly CancellationTokenSource stop = new();
    private readonly DispatcherTimer countdown = new() { Interval = TimeSpan.FromSeconds(30) };
    private readonly DetailsController details;
    private readonly PetDragController dragging;
    private readonly CharacterPresenter characterPresenter;
    internal CharacterFrame CurrentCharacterFrame => characterPresenter.CurrentFrame;
    internal bool CharacterResting => characterPresenter.Resting;
    internal bool CharacterRestPose => characterPresenter.RestPose;
    public void ToggleCharacterRest() => characterPresenter.ToggleRest();
    internal void WakeCharacterImmediately() => characterPresenter.WakeImmediately();
    public void ToggleAutoRest()
    {
        Settings = Settings with { AutoRest = !Settings.AutoRest };
        if (!Settings.AutoRest && CharacterResting) characterPresenter.ToggleRest();
        SavePlacement();
    }
    internal bool CanPlayAmbient => !exiting && !dragging.Dragged && !dragging.IsDragging && !details.MenuOpen
        && Mouse.LeftButton != MouseButtonState.Pressed;
    private QuotaSession? session;
    private Task? syncTask;
    private bool loaded, exiting;
    public event Action? ExitRequested;
    internal void RequestExit() => ExitRequested?.Invoke();
    internal void UpdateDockPreference(QuotaDock dock) => Settings = Settings with { QuotaPosition = dock };

    public MainWindow(PreferencesStore store, bool verification = false, IStartupRegistration? startup = null)
    {
        this.store = store;
        this.verification = verification;
        this.startup = startup ?? (verification
            ? new StartupRegistration(new MemoryStartupStore(), @"C:\CutePet verification\CutePet.exe", _ => true)
            : StartupRegistration.ForCurrentApp());
        Settings = store.Load();
        Characters = new CharacterLibrary(store.CharacterDirectory);
        InitializeComponent();
        dragging = new PetDragController(this, verification);
        details = new DetailsController(this, verification, () => dragging.Dragged, () => exiting);
        characterPresenter = new CharacterPresenter(this, verification);
        DataContext = Model;
        DetailsViewport.DataContext = Model;
        Model.PropertyChanged += (_, _) => RefreshCharacterFrame();
        ApplyCharacter();
        if (Characters.Warning is not null) Model.CharacterMessage = Characters.Warning;
        ApplyLockCursor();
        RefreshStartupState();
        // When upgrading an enabled portable installation, point its own entry at the current EXE.
        if (!verification && Settings.StartWithWindows) ApplyStartupResult(this.startup.SetEnabled(true));
        DetailsViewport.Width = 348 * Settings.Scale;
        DetailsViewport.Height = 256 * Settings.Scale;
        ApplyDockLayout(CurrentLayout, null, constrain: true);
        Topmost = Settings.AlwaysOnTop;
        countdown.Tick += (_, _) => Model.Tick();
        Loaded += OnLoaded;
        IsVisibleChanged += (_, _) =>
        {
            Animate(IsVisible && !verification);
            if (!IsVisible) { details.ResetHover(); StopDetailsTimers(); ShowDetails(false); }
            else ShowDetails(Settings.Details == DetailsMode.Always);
        };
        DpiChanged += (_, _) => Dispatcher.BeginInvoke(() =>
        {
            if (loaded)
            {
                var current = NativePlacement.Get(this);
                if (!dragging.IsDragging) { NativePlacement.Apply(this, current.Left, current.Top); SavePlacement(); }
                RepositionDetails();
            }
        });
        Scene.ContextMenu = DesktopMenuBuilder.Create(this);
        DetailsScene.ContextMenu = DesktopMenuBuilder.Create(this);
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

    public void TogglePositionLock()
    {
        if (dragging.IsDragging) EndQuotaDrag(cancel: true);
        dragging.CancelPointer();
        Settings = Settings with { PositionLocked = !Settings.PositionLocked };
        ApplyLockCursor();
        SavePlacement();
    }
    private void ApplyLockCursor()
    {
        PetStage.Cursor = QuotaCard.Cursor = Settings.PositionLocked ? Cursors.Arrow : Cursors.SizeAll;
    }
    public void RefreshStartupState() => ApplyStartupResult(startup.Read());
    public void ToggleStartup()
    {
        var state = startup.Read();
        if (state.Error is not null) { ApplyStartupResult(state); return; }
        ApplyStartupResult(startup.SetEnabled(!state.Enabled));
        SavePlacement();
    }
    private void ApplyStartupResult(StartupState state)
    {
        Settings = Settings with { StartWithWindows = state.Enabled };
        if (state.Error is not null) Model.CharacterMessage = state.Error;
    }

    public void SetCharacter(PetCharacter character) => SetCharacterPackage(CharacterCatalog.BuiltInId(character));

    internal void SetCharacterPackage(string id)
    {
        if (dragging.IsDragging) EndQuotaDrag(cancel: true);
        Settings = Settings with { CharacterPackId = Characters.Find(id).Id };
        ApplyCharacter();
        SavePlacement();
    }

    private void ApplyCharacter()
    {
        var requested = Settings.CharacterPackId ?? CharacterCatalog.BuiltInId(Settings.Character);
        SelectedCharacter = Characters.Find(requested);
        Settings = Settings with { CharacterPackId = SelectedCharacter.Id,
            Character = SelectedCharacter.Id == "tianyi" ? PetCharacter.Tianyi : PetCharacter.Cat };
        characterPresenter.ApplyPack();
    }

    private CharacterManagerWindow? characterManager;
    public void ManageCharacters()
    {
        if (characterManager is not null) { characterManager.Activate(); return; }
        characterManager = new CharacterManagerWindow(this) { Owner = this };
        characterManager.Closed += (_, _) => characterManager = null;
        characterManager.Show();
    }

    internal CharacterPack ImportCharacter(string path)
    {
        var imported = Characters.Import(path);
        SetCharacterPackage(imported.Id);
        return imported;
    }
    internal void RemoveCharacter(CharacterPack pack)
    {
        Characters.Remove(pack);
        if (SelectedCharacter.Id == pack.Id) SetCharacter(PetCharacter.Cat);
    }

    public void SetCharacterScale(double value) => SetIndependentScale(value, character: true);
    public void SetQuotaScale(double value) => SetIndependentScale(value, character: false);

    private void SetIndependentScale(double value, bool character)
    {
        if (dragging.IsDragging) EndQuotaDrag(cancel: true);
        var anchor = PetScreenOrigin();
        Settings = (character ? Settings with { CharacterScale = value } : Settings with { QuotaScale = value }).Validated();
        ApplyDockLayout(CurrentLayout, anchor, constrain: true);
        SavePlacement();
    }

    public void SetQuotaPosition(QuotaDock dock)
    {
        if (dragging.IsDragging) EndQuotaDrag(cancel: true);
        var anchor = PetScreenOrigin();
        Settings = (Settings with { QuotaPosition = dock }).Validated();
        ApplyDockLayout(CurrentLayout, anchor, constrain: true);
        SavePlacement();
    }

    internal Point? PetScreenOrigin() => PresentationSource.FromVisual(PetStage) is not null
        ? PetStage.PointToScreen(new Point()) : null;

    internal void ApplyDockLayout(DockLayout layout, Point? anchor, bool constrain)
    {
        Scene.Width = layout.Size.Width;
        Scene.Height = layout.Size.Height;
        Width = layout.Size.Width;
        Height = layout.Size.Height;
        Place(PetStage, layout.Pet);
        Place(QuotaCard, layout.Quota);
        QuotaSurface.Width = layout.Columns == 2 ? 224 : 116;
        QuotaSurface.Height = layout.Columns == 2 ? 40 : 68;
        CompactColumns = layout.Columns;
        SpeechBubble.Margin = new Thickness(Settings.QuotaPosition == QuotaDock.Right ? 0 : -26, 4, 0, 0);
        DetailsPopup.Placement = Settings.QuotaPosition switch
        { QuotaDock.Left => PlacementMode.Left, QuotaDock.Right => PlacementMode.Right, _ => PlacementMode.Top };
        UpdateLayout();
        if (anchor is Point physical)
        {
            var dpi = VisualTreeHelper.GetDpi(this);
            var left = physical.X - layout.Pet.X * dpi.DpiScaleX;
            var top = physical.Y - layout.Pet.Y * dpi.DpiScaleY;
            if (constrain) NativePlacement.Apply(this, left, top);
            else NativePlacement.MoveUnclamped(this, left, top);
        }
        RepositionDetails();
    }
    internal static void Place(FrameworkElement element, Rect bounds)
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

    public void HidePet() { if (dragging.IsDragging) EndQuotaDrag(cancel: true); StopDetailsTimers(); ShowDetails(false); Animate(false); SavePlacement(); Hide(); }

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

    internal void SavePlacement()
    {
        if (dragging.IsDragging) return; // Never persist the temporary drag workspace.
        if (loaded)
        {
            var point = NativePlacement.Get(this);
            Settings = Settings with { Left = point.Left, Top = point.Top };
        }
        if (!store.Save(Settings)) Model.CharacterMessage = "设置暂时无法保存";
    }

    private void OnHide(object sender, RoutedEventArgs e) => HidePet();
    private void OnRefresh(object sender, RoutedEventArgs e) => RefreshQuota();
    private void OnPinDetails(object sender, RoutedEventArgs e) => details.TogglePinned();
    private void OnCollapseDetails(object sender, RoutedEventArgs e) => details.Collapse();
    public void SetDetailsMode(DetailsMode mode)
    {
        Settings = (Settings with { Details = mode }).Validated();
        details.ApplyMode();
        SavePlacement();
    }
    private void OnHoverEnter(object sender, MouseEventArgs e) => details.PointerChanged(true);
    private void OnPetEnter(object sender, MouseEventArgs e) => characterPresenter.PointerChanged(true);
    private void OnPetLeave(object sender, MouseEventArgs e) => characterPresenter.PointerChanged(false);
    internal void CharacterPointerChanged(bool inside) => characterPresenter.PointerChanged(inside);
    internal void AdvanceAmbient(TimeSpan elapsed) => characterPresenter.AdvanceAmbient(elapsed);
    private void OnHoverLeave(object sender, MouseEventArgs e) => details.PointerChanged(Scene.IsMouseOver || DetailsViewport.IsMouseOver);
    internal void PointerChanged(bool inside) => details.PointerChanged(inside);
    internal void CompleteHoverOpen() => details.CompleteHoverOpen();
    internal void CompleteHoverClose() => details.CompleteHoverClose();
    internal void BeginDetailsMenu() => details.BeginDetailsMenu();
    internal void EndDetailsMenu() => details.EndDetailsMenu();
    internal void StopDetailsTimers() => details.StopDetailsTimers();
    internal void ShowDetails(bool visible) => details.ShowDetails(visible);
    private void OnDetailsOpened(object? sender, EventArgs e) => details.OnDetailsOpened();
    private void RepositionDetails() => details.RepositionDetails();

    private void OnMouseDown(object sender, MouseButtonEventArgs e) => dragging.OnMouseDown(e);
    private void OnMouseMove(object sender, MouseEventArgs e) => dragging.OnMouseMove(e);
    private async void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if (!dragging.OnMouseUp(e)) return;
        Model.CharacterMessage = "收到！我会看着的";
        characterPresenter.PlayInteraction();
        try { await Task.Delay(1800, stop.Token); } catch (OperationCanceledException) { return; }
        Model.RestoreCharacterMessage();
    }
    internal void BeginQuotaDrag(Point grabOffset) => dragging.BeginQuotaDrag(grabOffset);
    internal void UpdateQuotaDrag(Point pointer) => dragging.UpdateQuotaDrag(pointer);
    internal void EndQuotaDrag(bool cancel) => dragging.EndQuotaDrag(cancel);
    private void OnDockCaptureLost(object sender, MouseEventArgs e) => dragging.OnDockCaptureLost();
    private void OnDockKeyDown(object sender, KeyEventArgs e) => dragging.OnDockKeyDown(e);

    internal void StartCharacterBlink() => characterPresenter.StartCharacterBlink();
    internal void StartAnimationClock() => characterPresenter.StartAnimationClock();
    internal void AdvanceCharacterAnimation(TimeSpan elapsed) => characterPresenter.AdvanceCharacterAnimation(elapsed);
    internal void PlayGreeting() => characterPresenter.PlayGreeting();
    internal void PlayCharacterInteraction() => characterPresenter.PlayInteraction();
    private void RefreshCharacterFrame() => characterPresenter.RefreshCharacterFrame();
    private void Animate(bool active) => characterPresenter.Animate(active);

    protected override void OnClosing(CancelEventArgs e)
    {
        if (!exiting) { e.Cancel = true; ExitRequested?.Invoke(); }
        base.OnClosing(e);
    }

    public async Task StopAsync()
    {
        if (exiting) return;
        if (dragging.IsDragging) EndQuotaDrag(cancel: true);
        characterManager?.Close();
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
