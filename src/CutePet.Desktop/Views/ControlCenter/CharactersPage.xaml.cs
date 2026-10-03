using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CutePet.Desktop;

public partial class CharactersPage : UserControl, IDisposable
{
    private readonly MainWindow host;
    private readonly CharacterAnimation animation = new();
    private readonly CloudFlight cloudPreview = new();
    private readonly Stopwatch clock = new();
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(40) };
    private CharacterPack? Selected => CharacterList.SelectedItem as CharacterPack;
    private string? lastUsedId;
    private sealed record ActionChoice(string Key, string Label);
    public CharactersPage(MainWindow host)
    {
        this.host = host;
        InitializeComponent();
        host.Characters.Reload();
        host.SetCharacterPackage(host.SelectedCharacter.Id);
        RefreshList(host.SelectedCharacter.Id);
        StatusText.Text = host.Characters.Warning ?? "";
        timer.Tick += (_, _) =>
        {
            var elapsed = clock.Elapsed;
            clock.Restart();
            animation.Advance(elapsed);
            cloudPreview.Advance(elapsed);
            CloudPreview.Opacity = cloudPreview.Opacity;
            Preview.Source = animation.Image;
        };
        IsVisibleChanged += (_, _) =>
        {
            if (IsVisible) { clock.Restart(); timer.Start(); }
            else { timer.Stop(); clock.Reset(); }
        };
        host.DesktopStateChanged += OnHostStateChanged;
        OnHostStateChanged();
    }
    private void OnHostStateChanged()
    {
        CurrentUseText.Text = "当前使用：" + host.SelectedCharacter.Name;
        if (lastUsedId is not null && lastUsedId != host.SelectedCharacter.Id)
            StatusText.Text = "";
        lastUsedId = host.SelectedCharacter.Id;
        var installed = host.Characters.Packs.ToArray();
        if (CharacterList.ItemsSource is not CharacterPack[] shown || !shown.SequenceEqual(installed))
            RefreshList(Selected?.Id ?? host.SelectedCharacter.Id);
    }
    public void Dispose() { host.DesktopStateChanged -= OnHostStateChanged; timer.Stop(); clock.Stop(); }
    private void RefreshList(string? selected)
    {
        CharacterList.ItemsSource = host.Characters.Packs.ToArray();
        CharacterList.SelectedItem = host.Characters.Find(selected);
    }
    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Selected is not CharacterPack pack) return;
        animation.Configure(pack);
        cloudPreview.Cancel();
        CloudPreview.Opacity = 0;
        CloudPreview.Source = pack.CloudImage;
        CloudPreview.Width = pack.Manifest.Cloud?.DisplayWidth ?? 140;
        CloudPreview.Height = pack.Manifest.Cloud?.DisplayHeight ?? 32;
        Preview.Source = animation.Image;
        var labels = new[] { ("idle", "待机"), ("blink", "眨眼"), ("greeting", "打招呼"), ("low", "低额度"),
            ("look", "张望"), ("hover", "悬停"), ("happy", "开心"), ("conjure", "召唤王座"), ("sit", "坐下休息"), ("stand", "起身收起"), ("summon-cloud", "召唤小云"),
            ("sit-blink", "坐姿眨眼"), ("sit-greeting", "坐姿挥手"), ("sit-happy", "坐姿微笑"),
            ("edge-idle", "左右贴边"), ("edge-peek", "左右探头微笑"), ("edge-top-idle", "花藤秋千"),
            ("edge-top-peek", "秋千轻摆"), ("edge-bottom-idle", "下沿托腮"), ("edge-bottom-peek", "下沿抬头微笑") };
        CharacterInfo.Text = pack.Name;
        RightsInfo.Text = $"作者：{(string.IsNullOrWhiteSpace(pack.Manifest.Author) ? "未填写" : pack.Manifest.Author)}\n"
            + (string.IsNullOrWhiteSpace(pack.Manifest.License) ? "未填写素材许可，请确认图片的使用权限。" : pack.Manifest.License);
        RemoveButton.IsEnabled = !pack.BuiltIn;
        PreviewAction.ItemsSource = labels.Where(pair => pack.Actions.ContainsKey(pair.Item1))
            .Select(pair => new ActionChoice(pair.Item1, pair.Item2)).ToArray();
        PreviewAction.SelectedValue = pack.Actions.ContainsKey("greeting") ? "greeting" : "idle";
        PreviewButton.IsEnabled = true;
    }
    private void OnUse(object sender, RoutedEventArgs e)
    {
        if (Selected is not CharacterPack pack) return;
        host.SetCharacterPackage(pack.Id);
        StatusText.Text = "";
    }
    private void OnPreview(object sender, RoutedEventArgs e)
    {
        cloudPreview.Cancel();
        if (PreviewAction.SelectedValue is string action)
        { animation.Preview(action); if (action == "summon-cloud") cloudPreview.Start(Selected!.Actions[action].Duration); }
        CloudPreview.Opacity = 0;
        Preview.Source = animation.Image;
    }
    private void OnImport(object sender, RoutedEventArgs e)
    {
        var picker = new Microsoft.Win32.OpenFileDialog { Title = "导入自定义角色",
            Filter = "角色图片与角色包|*.png;*.zip|透明 PNG 图片|*.png|CutePet 角色包|*.zip", CheckFileExists = true };
        if (picker.ShowDialog(Window.GetWindow(this)) != true) return;
        TryAction(() =>
        {
            var imported = host.ImportCharacter(picker.FileName);
            RefreshList(imported.Id);
            StatusText.Text = $"已导入并使用「{imported.Name}」。";
        });
    }
    private void OnExport(object sender, RoutedEventArgs e)
    {
        if (Selected is not CharacterPack pack) return;
        var picker = new Microsoft.Win32.SaveFileDialog { Title = "导出角色包", FileName = pack.Id + ".cutepet.zip",
            Filter = "CutePet 角色包|*.zip", DefaultExt = ".zip", AddExtension = true, OverwritePrompt = true };
        if (picker.ShowDialog(Window.GetWindow(this)) != true) return;
        TryAction(() => { host.Characters.Export(pack, picker.FileName); StatusText.Text = "角色包已导出。"; });
    }
    private void OnRemove(object sender, RoutedEventArgs e)
    {
        if (Selected is not CharacterPack pack) return;
        TryAction(() =>
        {
            host.RemoveCharacter(pack);
            RefreshList(host.SelectedCharacter.Id);
            StatusText.Text = "角色已移除。";
        });
    }
    private void TryAction(Action action)
    {
        try { action(); }
        catch (Exception ex) when (CharacterLibrary.IsPackageError(ex))
        {
            StatusText.Text = ex is InvalidDataException ? ex.Message : "操作未完成，请检查文件格式和目录权限。";
        }
    }
}
