using System;
using System.Windows.Controls;

namespace CutePet.Desktop;

internal static class DesktopMenuBuilder
{
    internal static ContextMenu Create(MainWindow window)
    {
        var menu = new ContextMenu();
        Add("刷新额度", window.RefreshQuota);
        var pin = Add("始终置顶", window.ToggleTopmost);
        pin.IsCheckable = true;
        pin.IsChecked = window.Topmost;
        var positionLock = Add("锁定位置", window.TogglePositionLock);
        positionLock.IsCheckable = true;
        var autoStart = Add("开机启动", window.ToggleStartup);
        autoStart.IsCheckable = true;
        var characterSize = AddSizeMenu("角色大小", window.SetCharacterScale);
        var quotaSize = AddSizeMenu("额度数字大小", window.SetQuotaScale);
        var characters = new MenuItem { Header = "角色选择" };
        menu.Items.Add(characters);
        Add("管理 / 导入角色…", window.ManageCharacters);
        var rest = Add("召唤王座 / 坐下休息", window.ToggleCharacterRest);
        var autoRest = Add("自动坐下休息", window.ToggleAutoRest);
        autoRest.IsCheckable = true;
        var cloud = Add("乘云去逛逛", window.RoamDesktop);
        var autoCloud = Add("自由乘云活动", window.ToggleAutoCloud);
        autoCloud.IsCheckable = true;
        Add("召回到额度旁", window.RecallPet);
        var edge = Add("屏幕边缘互动", window.ToggleEdgeInteraction);
        edge.IsCheckable = true;
        var position = new MenuItem { Header = "额度条位置" };
        foreach (var dock in Enum.GetValues<QuotaDock>())
        {
            var item = new MenuItem { Header = DockLayout.Label(dock), IsCheckable = true, Tag = dock };
            item.Click += (_, _) => window.SetQuotaPosition(dock);
            position.Items.Add(item);
        }
        menu.Items.Add(position);
        var details = new MenuItem { Header = "详情显示" };
        foreach (var (mode, label) in new[] { (DetailsMode.Hover, "悬停显示"), (DetailsMode.Always, "固定显示"), (DetailsMode.Hidden, "隐藏详情") })
        {
            var item = new MenuItem { Header = label, IsCheckable = true, Tag = mode };
            item.Click += (_, _) => window.SetDetailsMode(mode);
            details.Items.Add(item);
        }
        menu.Items.Add(details);
        Add("移回屏幕右下角", window.ResetPosition);
        Add("选择 Codex 程序路径…", window.SelectCodexPath);
        menu.Items.Add(new Separator());
        Add("隐藏到托盘", window.HidePet);
        Add("退出 CutePet", window.RequestExit);
        menu.Opened += (_, _) =>
        {
            window.BeginDetailsMenu();
            pin.IsChecked = window.Topmost;
            window.RefreshStartupState();
            positionLock.IsChecked = window.Settings.PositionLocked;
            autoStart.IsChecked = window.Settings.StartWithWindows;
            rest.Header = window.CharacterResting ? "起身并收起王座" : "召唤王座 / 坐下休息";
            rest.IsEnabled = window.SelectedCharacter.Actions.ContainsKey("sit") && !window.CharacterRestPose
                && !(window.Model.IsLow && !window.Model.IsStale)
                || window.CharacterResting;
            autoRest.IsEnabled = window.SelectedCharacter.Manifest.RestAfterMs > 0;
            autoRest.IsChecked = window.Settings.AutoRest;
            cloud.IsEnabled = window.SelectedCharacter.CloudImage is not null && !window.Settings.PositionLocked
                && !window.CharacterRestPose && !window.CloudActive && !(window.Model.IsLow && !window.Model.IsStale);
            autoCloud.IsEnabled = window.SelectedCharacter.CloudImage is not null;
            autoCloud.IsChecked = window.Settings.AutoCloud;
            edge.IsEnabled = EdgeActions.Available(window.SelectedCharacter);
            edge.IsChecked = window.Settings.EdgeInteraction;
            foreach (MenuItem item in characterSize.Items)
                item.IsChecked = Math.Abs(window.Settings.EffectiveCharacterScale - (double)item.Tag) < 0.01;
            foreach (MenuItem item in quotaSize.Items)
                item.IsChecked = Math.Abs(window.Settings.EffectiveQuotaScale - (double)item.Tag) < 0.01;
            foreach (MenuItem item in details.Items) item.IsChecked = (DetailsMode)item.Tag == window.Settings.Details;
            foreach (MenuItem item in position.Items) item.IsChecked = (QuotaDock)item.Tag == window.Settings.QuotaPosition;
            characters.Items.Clear();
            foreach (var pack in window.Characters.Packs)
            {
                var item = new MenuItem { Header = pack.Name, IsCheckable = true, IsChecked = pack.Id == window.SelectedCharacter.Id };
                item.Click += (_, _) => window.SetCharacterPackage(pack.Id);
                characters.Items.Add(item);
            }
        };
        menu.Closed += (_, _) => window.EndDetailsMenu();
        return menu;
        MenuItem AddSizeMenu(string label, Action<double> setScale)
        {
            var size = new MenuItem { Header = label };
            foreach (var scale in new[] { 0.8, 1.0, 1.2, 1.4, 1.6, 1.8, 2.0 })
            {
                var item = new MenuItem { Header = $"{scale * 100:0}%", IsCheckable = true, Tag = scale };
                item.Click += (_, _) => setScale(scale);
                size.Items.Add(item);
            }
            menu.Items.Add(size);
            return size;
        }
        MenuItem Add(string text, Action action)
        {
            var item = new MenuItem { Header = text };
            item.Click += (_, _) => action();
            menu.Items.Add(item);
            return item;
        }
    }

}
