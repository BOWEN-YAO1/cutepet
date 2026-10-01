using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace CutePet.Desktop;

public partial class CharacterManagerWindow : Window
{
    private readonly MainWindow host;
    private readonly CharacterAnimation animation = new();
    private readonly Stopwatch clock = new();
    private readonly DispatcherTimer timer = new() { Interval = TimeSpan.FromMilliseconds(40) };
    private CharacterPack? Selected => CharacterList.SelectedItem as CharacterPack;
    public CharacterManagerWindow(MainWindow host)
    {
        this.host = host;
        InitializeComponent();
        host.Characters.Reload();
        host.SetCharacterPackage(host.SelectedCharacter.Id);
        RefreshList(host.SelectedCharacter.Id);
        StatusText.Text = host.Characters.Warning ?? "导入只在本机保存，不会上传图片。内置角色始终保留。";
        timer.Tick += (_, _) =>
        {
            var elapsed = clock.Elapsed;
            clock.Restart();
            animation.Advance(elapsed);
            Preview.Source = animation.Image;
        };
        IsVisibleChanged += (_, _) =>
        {
            if (IsVisible) { clock.Restart(); timer.Start(); }
            else { timer.Stop(); clock.Reset(); }
        };
        Closed += (_, _) => { timer.Stop(); clock.Stop(); };
    }
    private void RefreshList(string? selected)
    {
        CharacterList.ItemsSource = host.Characters.Packs.ToArray();
        CharacterList.SelectedItem = host.Characters.Find(selected);
    }
    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Selected is not CharacterPack pack) return;
        animation.Configure(pack);
        Preview.Source = animation.Image;
        var labels = new[] { ("idle", "待机"), ("blink", "眨眼"), ("greeting", "打招呼"), ("low", "低额度") };
        CharacterInfo.Text = $"{(pack.BuiltIn ? "内置角色" : "自定义角色")} · {pack.Name}\n动作："
            + string.Join("、", labels.Where(pair => pack.Actions.ContainsKey(pair.Item1)).Select(pair => pair.Item2));
        RightsInfo.Text = $"作者：{(string.IsNullOrWhiteSpace(pack.Manifest.Author) ? "未填写" : pack.Manifest.Author)}\n"
            + (string.IsNullOrWhiteSpace(pack.Manifest.License) ? "未填写素材许可，请确认图片的使用权限。" : pack.Manifest.License);
        RemoveButton.IsEnabled = !pack.BuiltIn;
        PreviewButton.IsEnabled = pack.Actions.ContainsKey("greeting");
    }
    private void OnUse(object sender, RoutedEventArgs e)
    {
        if (Selected is not CharacterPack pack) return;
        host.SetCharacterPackage(pack.Id);
        StatusText.Text = $"已使用「{pack.Name}」，重启后会保留。";
    }
    private void OnPreview(object sender, RoutedEventArgs e) { animation.Greet(); Preview.Source = animation.Image; }
    private void OnImport(object sender, RoutedEventArgs e)
    {
        var picker = new Microsoft.Win32.OpenFileDialog { Title = "导入自定义角色",
            Filter = "角色图片与角色包|*.png;*.zip|透明 PNG 图片|*.png|CutePet 角色包|*.zip", CheckFileExists = true };
        if (picker.ShowDialog(this) != true) return;
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
        if (picker.ShowDialog(this) != true) return;
        TryAction(() => { host.Characters.Export(pack, picker.FileName); StatusText.Text = "角色包已导出，包含图片、动作配置和素材说明。"; });
    }
    private void OnRemove(object sender, RoutedEventArgs e)
    {
        if (Selected is not CharacterPack pack) return;
        TryAction(() =>
        {
            host.RemoveCharacter(pack);
            RefreshList(host.SelectedCharacter.Id);
            StatusText.Text = "已移除，原文件保留在角色目录的 .removed 文件夹。";
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
