using System;
using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace CutePet.Desktop;

internal sealed record CenterChoice<T>(T Value, string Label);

// One view of the existing desktop state. Commands reuse the host; no second quota reader or preferences file.
internal sealed class CenterViewModel : INotifyPropertyChanged, IDisposable
{
    private readonly MainWindow host;
    public event PropertyChangedEventHandler? PropertyChanged;
    internal event Action? CommandsChanged;
    public PetViewModel Quota => host.Model;
    public string CharacterName => host.SelectedCharacter.Name;
    public BitmapSource CharacterImage => host.SelectedCharacter.Idle.Frames[0].Image;
    public string VersionText => "版本 " + typeof(App).Assembly.GetName().Version!.ToString(3);
    public string VisibilityText => host.IsVisible ? "桌宠正在显示" : "桌宠已隐藏";
    public string VisibilityAction => host.IsVisible ? "隐藏桌宠" : "显示桌宠";
    public string ActivityText => !host.IsVisible ? "已隐藏 · 活动暂停" : host.ScreenEdgeActive ? "正在屏幕边缘休息"
        : host.CloudActive ? "正在乘云游动" : host.CharacterResting ? "正在王座上休息" : "正在待机";
    public string RestAction => host.CharacterResting ? "起身收起王座" : "召唤王座休息";
    public string DataNote => host.Model.IsStale && host.Model.HasData ? "同步尚未恢复。下方如有数值，为上次读取的记录。"
        : !host.Model.HasData ? "连接后会显示官方剩余百分比和重置时间。" : "使用后自动同步官方额度。";
    public string CodexPathText => host.Settings.CodexPath ?? "自动查找已安装的 Codex";
    public string Notice => host.Model.CharacterMessage is "我来帮你看额度" or "等连接恢复再看哦" or "额度快用完啦" or "收到！我会看着的"
        ? "" : host.Model.CharacterMessage;
    public bool CanCloud => host.SelectedCharacter.CloudImage is not null;
    public bool CanAutoRest => host.SelectedCharacter.Manifest.RestAfterMs > 0;
    public bool CanEdge => EdgeActions.Available(host.SelectedCharacter);
    public bool CanRoam => CanCloud && !PositionLocked && !host.CharacterRestPose && !host.CloudActive
        && !(host.Model.IsLow && !host.Model.IsStale);
    public bool CanRest => host.CharacterResting || host.SelectedCharacter.Actions.ContainsKey("sit") && !host.CharacterRestPose
        && !(host.Model.IsLow && !host.Model.IsStale);
    public string CloudHint => !CanCloud ? "当前角色未配置乘云动作。可切换天依或导入带云层的角色包。"
        : PositionLocked ? "位置已锁定，解锁后才能乘云。" : host.CharacterRestPose ? "先起身，再出发。"
        : host.Model.IsLow && !host.Model.IsStale ? "当前额度较低，乘云暂时停用。" : "仅在当前屏幕可用区域活动，额度面板保持固定。";
    public string RestHint => !host.SelectedCharacter.Actions.ContainsKey("sit") ? "当前角色未配置坐姿动作。"
        : "施法召唤王座，坐下休息；再次点击可起身。";
    public bool AlwaysOnTop { get => host.Topmost; set { if (value != AlwaysOnTop) host.ToggleTopmost(); } }
    public bool PositionLocked { get => host.Settings.PositionLocked; set { if (value != PositionLocked) host.TogglePositionLock(); } }
    public bool AutoCloud { get => host.Settings.AutoCloud; set { if (value != AutoCloud) host.ToggleAutoCloud(); } }
    public bool AutoRest { get => host.Settings.AutoRest; set { if (value != AutoRest) host.ToggleAutoRest(); } }
    public bool EdgeInteraction { get => host.Settings.EdgeInteraction; set { if (value != EdgeInteraction) host.ToggleEdgeInteraction(); } }
    public bool StartWithWindows { get => host.Settings.StartWithWindows; set { if (value != StartWithWindows) host.ToggleStartup(); Refresh(); } }
    public double CharacterScale { get => host.Settings.EffectiveCharacterScale; set { if (Math.Abs(value - CharacterScale) > 0.001) host.SetCharacterScale(value); } }
    public double QuotaScale { get => host.Settings.EffectiveQuotaScale; set { if (Math.Abs(value - QuotaScale) > 0.001) host.SetQuotaScale(value); } }
    public DetailsMode Details { get => host.Settings.Details; set { if (value != Details) host.SetDetailsMode(value); } }
    // Docking is an explicit action because a manually moved quota card has no persistent dock relationship.
    public CenterChoice<QuotaDock>[] Docks { get; } = Array.ConvertAll(Enum.GetValues<QuotaDock>(), dock => new CenterChoice<QuotaDock>(dock, DockLayout.Label(dock)));
    public CenterChoice<DetailsMode>[] DetailChoices { get; } = { new(DetailsMode.Hover, "悬停显示"), new(DetailsMode.Always, "固定显示"), new(DetailsMode.Hidden, "隐藏详情") };
    public CenterChoice<double>[] Scales { get; } = { new(0.8,"80%"),new(1,"100%"),new(1.2,"120%"),new(1.4,"140%"),new(1.5,"150%"),new(1.6,"160%"),new(1.8,"180%"),new(2,"200%") };
    public ICommand RefreshCommand { get; }
    public ICommand VisibilityCommand { get; }
    public ICommand RecallCommand { get; }
    public ICommand RoamCommand { get; }
    public ICommand RestCommand { get; }
    public ICommand GreetCommand { get; }
    public ICommand PathCommand { get; }
    public ICommand ResetCommand { get; }
    public ICommand DockCommand { get; }
    public ICommand ExitCommand { get; }
    public CenterViewModel(MainWindow host, Action choosePath)
    {
        this.host = host;
        RefreshCommand = Command(host.RefreshQuota, () => host.Model.CanRefresh);
        VisibilityCommand = Command(() => { if (host.IsVisible) host.HidePet(); else host.RestorePet(); });
        RecallCommand = Command(() => { host.RestorePet(); host.RecallPet(); });
        RoamCommand = Command(() => { host.RestorePet(); host.RoamDesktop(); }, () => CanRoam);
        RestCommand = Command(() => { host.RestorePet(); host.ToggleCharacterRest(); }, () => CanRest);
        GreetCommand = Command(() => { host.RestorePet(); host.PlayCharacterInteraction(); });
        PathCommand = Command(choosePath);
        ResetCommand = Command(host.ResetPosition);
        DockCommand = new CenterCommand(this, value => { if (value is QuotaDock dock) { host.SetQuotaPosition(dock); Refresh(); } });
        ExitCommand = Command(host.RequestExit);
        host.DesktopStateChanged += Refresh;
        host.Model.PropertyChanged += ModelChanged;
    }
    private CenterCommand Command(Action action, Func<bool>? enabled = null) => new(this, _ => { action(); Refresh(); }, _ => enabled?.Invoke() ?? true);
    private void ModelChanged(object? sender, PropertyChangedEventArgs e) => Refresh();
    internal void Refresh()
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
        CommandsChanged?.Invoke();
    }
    public void Dispose() { host.DesktopStateChanged -= Refresh; host.Model.PropertyChanged -= ModelChanged; CommandsChanged = null; }
    private sealed class CenterCommand : ICommand
    {
        private readonly Action<object?> action;
        private readonly Func<object?, bool>? enabled;
        internal CenterCommand(CenterViewModel model, Action<object?> action, Func<object?, bool>? enabled = null)
        { this.action = action; this.enabled = enabled; model.CommandsChanged += Changed; }
        public event EventHandler? CanExecuteChanged;
        public bool CanExecute(object? parameter) => enabled?.Invoke(parameter) ?? true;
        public void Execute(object? parameter) { if (CanExecute(parameter)) action(parameter); }
        internal void Changed() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }
}
