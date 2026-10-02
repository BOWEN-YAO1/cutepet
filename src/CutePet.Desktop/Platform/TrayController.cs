using System;
using System.Drawing;
using System.Runtime.InteropServices;
using Forms = System.Windows.Forms;

namespace CutePet.Desktop;

internal sealed class TrayController : IDisposable
{
    private readonly Forms.NotifyIcon tray;
    private readonly Icon icon;
    private readonly Forms.ContextMenuStrip menu;
    [DllImport("user32.dll")] private static extern bool DestroyIcon(IntPtr icon);

    public TrayController(MainWindow window, Action exit)
    {
        icon = MakeIcon();
        menu = new Forms.ContextMenuStrip();
        Add("显示桌宠", window.RestorePet);
        Add("隐藏桌宠", window.HidePet);
        Add("刷新额度", window.RefreshQuota);
        var pin = Add("始终置顶", window.ToggleTopmost);
        var positionLock = Add("锁定位置", window.TogglePositionLock);
        var autoStart = Add("开机启动", window.ToggleStartup);
        menu.Opening += (_, _) => pin.Checked = window.Topmost;
        var details = new Forms.ToolStripMenuItem("详情显示");
        foreach (var (mode, label) in new[] { (DetailsMode.Hover, "悬停显示"), (DetailsMode.Always, "固定显示"), (DetailsMode.Hidden, "隐藏详情") })
        {
            var item = new Forms.ToolStripMenuItem(label) { Tag = mode };
            item.Click += (_, _) => window.SetDetailsMode(mode);
            details.DropDownItems.Add(item);
        }
        menu.Items.Add(details);
        var position = new Forms.ToolStripMenuItem("额度条位置");
        foreach (var dock in Enum.GetValues<QuotaDock>())
        {
            var item = new Forms.ToolStripMenuItem(DockLayout.Label(dock)) { Tag = dock };
            item.Click += (_, _) => window.SetQuotaPosition(dock);
            position.DropDownItems.Add(item);
        }
        menu.Items.Add(position);
        var characters = new Forms.ToolStripMenuItem("角色选择");
        menu.Items.Add(characters);
        Add("管理 / 导入角色…", window.ManageCharacters);
        var rest = Add("召唤王座 / 坐下休息", () => { window.RestorePet(); window.ToggleCharacterRest(); });
        var autoRest = Add("自动坐下休息", window.ToggleAutoRest);
        var cloud = Add("乘云去逛逛", () => { window.RestorePet(); window.RoamDesktop(); });
        var autoCloud = Add("自由乘云活动", window.ToggleAutoCloud);
        Add("召回到额度旁", () => { window.RestorePet(); window.RecallPet(); });
        var edge = Add("屏幕边缘互动", window.ToggleEdgeInteraction);
        var characterSize = AddSizeMenu("角色大小", window.SetCharacterScale);
        var quotaSize = AddSizeMenu("额度数字大小", window.SetQuotaScale);
        menu.Opening += (_, _) =>
        {
            window.BeginDetailsMenu();
            window.RefreshStartupState();
            positionLock.Checked = window.Settings.PositionLocked;
            autoStart.Checked = window.Settings.StartWithWindows;
            rest.Text = window.CharacterResting ? "起身并收起王座" : "召唤王座 / 坐下休息";
            rest.Enabled = window.SelectedCharacter.Actions.ContainsKey("sit") && !window.CharacterRestPose
                && !(window.Model.IsLow && !window.Model.IsStale) || window.CharacterResting;
            autoRest.Enabled = window.SelectedCharacter.Manifest.RestAfterMs > 0;
            autoRest.Checked = window.Settings.AutoRest;
            cloud.Enabled = window.SelectedCharacter.CloudImage is not null && !window.Settings.PositionLocked
                && !window.CharacterRestPose && !window.CloudActive && !(window.Model.IsLow && !window.Model.IsStale);
            autoCloud.Enabled = window.SelectedCharacter.CloudImage is not null;
            autoCloud.Checked = window.Settings.AutoCloud;
            edge.Enabled = window.SelectedCharacter.Actions.ContainsKey("edge-idle");
            edge.Checked = window.Settings.EdgeInteraction;
            foreach (Forms.ToolStripMenuItem item in details.DropDownItems)
                item.Checked = (DetailsMode)item.Tag! == window.Settings.Details;
            foreach (Forms.ToolStripMenuItem item in position.DropDownItems)
                item.Checked = (QuotaDock)item.Tag! == window.Settings.QuotaPosition;
            while (characters.DropDownItems.Count > 0)
            { var old = characters.DropDownItems[0]; characters.DropDownItems.RemoveAt(0); old.Dispose(); }
            foreach (var pack in window.Characters.Packs)
            {
                var item = new Forms.ToolStripMenuItem(pack.Name) { Checked = pack.Id == window.SelectedCharacter.Id };
                item.Click += (_, _) => window.SetCharacterPackage(pack.Id);
                characters.DropDownItems.Add(item);
            }
            foreach (Forms.ToolStripMenuItem item in characterSize.DropDownItems)
                item.Checked = Math.Abs(window.Settings.EffectiveCharacterScale - (double)item.Tag!) < 0.01;
            foreach (Forms.ToolStripMenuItem item in quotaSize.DropDownItems)
                item.Checked = Math.Abs(window.Settings.EffectiveQuotaScale - (double)item.Tag!) < 0.01;
        };
        menu.Closed += (_, _) => window.EndDetailsMenu();
        menu.Items.Add(new Forms.ToolStripSeparator());
        Add("移回屏幕右下角", window.ResetPosition);
        Add("退出 CutePet", exit);
        tray = new Forms.NotifyIcon { Icon = icon, Text = "CutePet · Codex 额度", ContextMenuStrip = menu, Visible = true };
        tray.DoubleClick += (_, _) => window.RestorePet();
        Forms.ToolStripMenuItem AddSizeMenu(string label, Action<double> setScale)
        {
            var size = new Forms.ToolStripMenuItem(label);
            foreach (var scale in new[] { 0.8, 1.0, 1.2, 1.4, 1.6, 1.8, 2.0 })
            {
                var item = new Forms.ToolStripMenuItem($"{scale * 100:0}%") { Tag = scale };
                item.Click += (_, _) => setScale(scale);
                size.DropDownItems.Add(item);
            }
            menu.Items.Add(size);
            return size;
        }
        Forms.ToolStripMenuItem Add(string text, Action action)
        {
            var item = new Forms.ToolStripMenuItem(text);
            item.Click += (_, _) => action();
            menu.Items.Add(item);
            return item;
        }
    }

    private static Icon MakeIcon()
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);
        using var green = new SolidBrush(Color.FromArgb(70, 113, 89));
        graphics.FillEllipse(green, 2, 2, 28, 28);
        using var font = new Font("Segoe UI", 17, FontStyle.Bold, GraphicsUnit.Pixel);
        graphics.DrawString("C", font, Brushes.White, 7, 5);
        var handle = bitmap.GetHicon();
        try { using var borrowed = Icon.FromHandle(handle); return (Icon)borrowed.Clone(); }
        finally { DestroyIcon(handle); }
    }

    public void Dispose() { tray.Visible = false; tray.Dispose(); menu.Dispose(); icon.Dispose(); }
}
