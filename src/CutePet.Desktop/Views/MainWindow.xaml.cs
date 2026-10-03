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
    internal DockLayout CurrentLayout => SplitWindowLayout.For(Settings);
    public Size LayoutSize => CurrentLayout.Size;
    internal QuotaWindow QuotaHost { get; }
    internal Viewbox QuotaCard => QuotaHost.QuotaCard;
    internal Border QuotaSurface => QuotaHost.QuotaSurface;
    internal Viewbox DetailsViewport => QuotaHost.DetailsViewport;
    internal Grid DetailsScene => QuotaHost.DetailsScene;
    internal Popup DetailsPopup => QuotaHost.DetailsPopup;
    internal Button PinDetailsButton => QuotaHost.PinDetailsButton;
    public int CompactColumns => QuotaHost.CompactColumns;
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
    private readonly CloudMotionController cloudMotion;
    private readonly EdgeMotionController edgeMotion;
    internal bool ScreenEdgeActive => edgeMotion.Active;
    internal EdgeAttachment? ScreenEdgeAttachment => edgeMotion.Attachment;
    internal double ScreenEdgePeekOffset => edgeMotion.PeekOffset;
    internal void StartEdgePose(string baseAction) => characterPresenter.AttachEdge(baseAction);
    internal void RefreshScreenEdgeBounds(Rect area, Size size) => edgeMotion.Reanchor(area, size);
    internal string? ScreenEdgeResponse => edgeMotion.Response;
    internal void StartEdgePeek(string? action = null) => characterPresenter.PeekEdge(action);
    internal void PeekScreenEdge() => edgeMotion.Peek();
    internal void CancelScreenEdge() => edgeMotion.Cancel();
    internal void AdvanceScreenEdge(TimeSpan elapsed) => edgeMotion.Advance(elapsed);
    public void ToggleEdgeInteraction()
    {
        Settings = Settings with { EdgeInteraction = !Settings.EdgeInteraction };
        if (!Settings.EdgeInteraction && ScreenEdgeActive) WakeCharacterImmediately();
        SavePlacement();
    }
    internal bool CompletePetDrag(Rect area, Size size, Point released, double dpi)
    {
        var attached = edgeMotion.Attach(area, size, released, dpi);
        if (!attached && !verification) NativePlacement.Apply(this, released.X, released.Y);
        SavePlacement();
        return attached;
    }
    private bool cloudPointerInside;
    internal bool CloudActive => cloudMotion.Active;
    internal bool CharacterIdle => characterPresenter.Idle;
    internal double CloudRequestedX => cloudMotion.RequestedX;
    internal double CloudRequestedY => cloudMotion.RequestedY;
    internal RoamingRoute? CloudRoute => cloudMotion.Route;
    internal double CloudTripDuration => cloudMotion.TripDuration;
    internal Point? LastRecallPosition { get; private set; }
    internal bool CanCloudMove => CanPlayAmbient && !cloudPointerInside && !DetailsVisible
        && ControlCenter?.IsCharactersPage != true && (verification || !NativePlacement.PointerNear(this, 28));
    public void SummonCloud() { if (ScreenEdgeActive) WakeCharacterImmediately(); cloudMotion.Start(); }
    public void RoamDesktop() { if (ScreenEdgeActive) WakeCharacterImmediately(); cloudMotion.Start(roam: true); }
    public void RecallPet()
    {
        WakeCharacterImmediately();
        var pet = NativePlacement.RoamingBounds(this);
        var quota = NativePlacement.RoamingBounds(QuotaHost);
        var destination = DesktopRoaming.Recall(quota.Area, pet.Size, new Rect(QuotaHost.Position, quota.Size));
        LastRecallPosition = destination;
        if (!verification) NativePlacement.Apply(this, destination.X, destination.Y);
        SavePlacement();
    }
    public void ToggleAutoCloud()
    {
        Settings = Settings with { AutoCloud = !Settings.AutoCloud };
        if (!Settings.AutoCloud) cloudMotion.Cancel();
        SavePlacement();
    }
    internal void AdvanceCloud(TimeSpan elapsed) => cloudMotion.Advance(elapsed);
    internal void CancelCloud() => cloudMotion.Cancel();
    internal void StartCloudSpell() => characterPresenter.StartCloudSpell();
    internal void CancelCloudSpell() => characterPresenter.CancelCloudSpell();
    internal CharacterFrame CurrentCharacterFrame => characterPresenter.CurrentFrame;
    internal LoadedFrame CurrentSpriteFrame => characterPresenter.SpriteFrame;
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
    internal event Action? DesktopStateChanged;
    internal ControlCenterWindow? ControlCenter { get; private set; }
    internal void RequestExit() => ExitRequested?.Invoke();

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
        Icon = AppIcon.WindowIcon;
        if (verification) { Opacity = 0; Left = Top = -10000; }
        QuotaHost = new QuotaWindow(this, verification);
        dragging = new PetDragController(this, verification);
        details = new DetailsController(this, verification, () => dragging.Dragged, () => exiting);
        characterPresenter = new CharacterPresenter(this, verification);
        cloudMotion = new CloudMotionController(this, verification);
        edgeMotion = new EdgeMotionController(this, verification);
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
        QuotaHost.MoveTo(new(Settings.QuotaLeft ?? 0, Settings.QuotaTop ?? 0));
        Topmost = Settings.AlwaysOnTop;
        countdown.Tick += (_, _) => Model.Tick();
        Loaded += OnLoaded;
        IsVisibleChanged += (_, _) =>
        {
            Animate(IsVisible && !verification);
            if (!IsVisible) { QuotaHost.Hide(); cloudPointerInside = false; details.ResetHover(); StopDetailsTimers(); ShowDetails(false); }
            else if (loaded) { QuotaHost.ShowAt(QuotaHost.Position); ShowDetails(Settings.Details == DetailsMode.Always); }
            DesktopStateChanged?.Invoke();
        };
        DpiChanged += (_, _) => Dispatcher.BeginInvoke(() =>
        {
            var bounds = NativePlacement.EdgeBounds(this);
            if (ScreenEdgeActive && !dragging.Dragged) RefreshScreenEdgeBounds(bounds.Area, bounds.Size);
            CancelCloud();
            if (loaded)
            {
                var current = NativePlacement.Get(this);
                if (!dragging.Dragged) { NativePlacement.Apply(this, current.Left, current.Top); SavePlacement(); }
                RepositionDetails();
            }
        });
        Scene.ContextMenu = DesktopMenuBuilder.Create(this);
        ShowDetails(Settings.Details == DetailsMode.Always);
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (loaded) return;
        var dpi = VisualTreeHelper.GetDpi(this);
        Settings = SplitWindowLayout.Migrate(Settings, dpi.DpiScaleX, dpi.DpiScaleY);
        if (verification) NativePlacement.MoveUnclamped(this, -10000, -10000);
        else NativePlacement.Apply(this, Settings.Left, Settings.Top);
        loaded = true;
        if (Settings.QuotaLeft is double qx && Settings.QuotaTop is double qy) QuotaHost.ShowAt(new(qx, qy));
        else { QuotaHost.ShowAt(QuotaNearPet()); }
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
        CancelCloud();
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

    public void ManageCharacters() => OpenControlCenter("characters");
    public void OpenControlCenter() => OpenControlCenter("overview");
    internal void OpenControlCenter(string page)
    {
        if (exiting) return;
        if (ControlCenter is null)
        {
            ControlCenter = new ControlCenterWindow(this, verification);
            ControlCenter.Closed += (_, _) => ControlCenter = null;
        }
        ControlCenter.Navigate(page);
        if (!ControlCenter.IsVisible) ControlCenter.Show();
        if (ControlCenter.WindowState == WindowState.Minimized) ControlCenter.WindowState = WindowState.Normal;
        if (!verification) ControlCenter.Activate();
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
        if (character) ApplyDockLayout(CurrentLayout, anchor, constrain: true);
        else QuotaHost.ResizeCard(Settings.QuotaPosition, Settings.EffectiveQuotaScale);
        SavePlacement();
    }

    public void SetQuotaPosition(QuotaDock dock)
    {
        if (dragging.IsDragging) EndQuotaDrag(cancel: true);
        Settings = (Settings with { QuotaPosition = dock }).Validated();
        QuotaHost.ResizeCard(Settings.QuotaPosition, Settings.EffectiveQuotaScale);
        ApplyDetailsPlacement();
        QuotaHost.MoveTo(QuotaNearPet());
        SavePlacement();
    }

    internal Point? PetScreenOrigin() => PresentationSource.FromVisual(PetStage) is not null
        ? PetStage.PointToScreen(new Point()) : null;

    internal void ApplyDockLayout(DockLayout layout, Point? anchor, bool constrain)
    {
        if (ScreenEdgeActive) WakeCharacterImmediately();
        CancelCloud();
        Scene.Width = layout.Size.Width;
        Scene.Height = layout.Size.Height;
        Width = layout.Size.Width;
        Height = layout.Size.Height;
        Place(PetStage, layout.Pet);
        QuotaHost.ResizeCard(Settings.QuotaPosition, Settings.EffectiveQuotaScale);
        SpeechBubble.Margin = new Thickness(0, 4, 0, 0);
        ApplyDetailsPlacement();
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
    internal Point QuotaNearPet()
    {
        var pet = NativePlacement.Get(this);
        var dpi = VisualTreeHelper.GetDpi(this);
        var offset = SplitWindowLayout.QuotaOffset(Settings, dpi.DpiScaleX, dpi.DpiScaleY);
        return new(pet.Left + offset.X, pet.Top + offset.Y);
    }
    private void ApplyDetailsPlacement() => DetailsPopup.Placement = Settings.QuotaPosition switch
    { QuotaDock.Left => PlacementMode.Left, QuotaDock.Right => PlacementMode.Right, _ => PlacementMode.Top };
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
        QuotaHost.Topmost = Topmost;
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
            if (verification) NativePlacement.MoveUnclamped(this, -10000, -10000);
            else NativePlacement.Apply(this, current.Left, current.Top);
        }
        Activate();
    }

    public void HidePet() { if (dragging.IsDragging) EndQuotaDrag(cancel: true); StopDetailsTimers(); ShowDetails(false); Animate(false); SavePlacement(); Hide(); }

    public void ResetPosition()
    {
        if (ScreenEdgeActive) WakeCharacterImmediately();
        CancelCloud();
        NativePlacement.Apply(this, null, null);
        QuotaHost.MoveTo(QuotaNearPet());
        RepositionDetails();
        SavePlacement();
    }

    public void SelectCodexPath() => SelectCodexPath(this);
    internal void SelectCodexPath(Window owner)
    {
        var picker = new Microsoft.Win32.OpenFileDialog
        {
            Title = "选择官方 Codex 原生程序", Filter = "Codex 原生程序 (codex.exe)|codex.exe",
            CheckFileExists = true, Multiselect = false
        };
        if (picker.ShowDialog(owner) != true) return;
        Settings = Settings with { CodexPath = picker.FileName };
        SavePlacement();
        Model.CharacterMessage = "已保存，下次启动生效";
    }

    internal void SetInterfaceColors(string theme, string font)
    {
        Settings = (Settings with { ThemeColor = theme, FontColor = font }).Validated();
        if (!store.Save(Settings)) Model.CharacterMessage = "设置暂时无法保存";
        DesktopStateChanged?.Invoke();
    }

    internal void SavePlacement()
    {
        if (dragging.IsDragging) return; // Persist quota coordinates only after the gesture commits.
        if (loaded)
        {
            var point = NativePlacement.Get(this);
            Settings = Settings with { Left = point.Left, Top = point.Top };
        }
        Settings = Settings with { QuotaLeft = QuotaHost.Position.X, QuotaTop = QuotaHost.Position.Y };
        if (!store.Save(Settings)) Model.CharacterMessage = "设置暂时无法保存";
        DesktopStateChanged?.Invoke();
    }

    internal void CollapseDetails() => details.Collapse();
    public void SetDetailsMode(DetailsMode mode)
    {
        Settings = (Settings with { Details = mode }).Validated();
        details.ApplyMode();
        SavePlacement();
    }
    private void OnHoverEnter(object sender, MouseEventArgs e) => cloudPointerInside = true;
    private void OnPetEnter(object sender, MouseEventArgs e) => characterPresenter.PointerChanged(true);
    private void OnPetLeave(object sender, MouseEventArgs e) => characterPresenter.PointerChanged(false);
    internal void CharacterPointerChanged(bool inside) => characterPresenter.PointerChanged(inside);
    internal void AdvanceAmbient(TimeSpan elapsed) => characterPresenter.AdvanceAmbient(elapsed);
    private void OnHoverLeave(object sender, MouseEventArgs e) => cloudPointerInside = Scene.IsMouseOver;
    internal void PointerChanged(bool inside) { cloudPointerInside = inside; details.PointerChanged(inside); }
    internal void CompleteHoverOpen() => details.CompleteHoverOpen();
    internal void CompleteHoverClose() => details.CompleteHoverClose();
    internal void BeginDetailsMenu() => details.BeginDetailsMenu();
    internal void EndDetailsMenu() => details.EndDetailsMenu();
    internal void StopDetailsTimers() => details.StopDetailsTimers();
    internal void ShowDetails(bool visible) => details.ShowDetails(visible);
    internal void RepositionDetails() { if (details is not null) details.RepositionDetails(); }

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
        ControlCenter?.Close();
        exiting = true;
        SavePlacement();
        countdown.Stop();
        StopDetailsTimers();
        ShowDetails(false);
        Animate(false);
        QuotaHost.CloseForExit();
        stop.Cancel();
        if (syncTask is not null) await syncTask;
        stop.Dispose();
    }
}
