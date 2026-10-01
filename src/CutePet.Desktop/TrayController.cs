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
        foreach (var character in Enum.GetValues<PetCharacter>())
        {
            var item = new Forms.ToolStripMenuItem(CharacterCatalog.Label(character)) { Tag = character };
            item.Click += (_, _) => window.SetCharacter(character);
            characters.DropDownItems.Add(item);
        }
        menu.Items.Add(characters);
        var characterSize = AddSizeMenu("角色大小", window.SetCharacterScale);
        var quotaSize = AddSizeMenu("额度数字大小", window.SetQuotaScale);
        menu.Opening += (_, _) =>
        {
            window.BeginDetailsMenu();
            foreach (Forms.ToolStripMenuItem item in details.DropDownItems)
                item.Checked = (DetailsMode)item.Tag! == window.Settings.Details;
            foreach (Forms.ToolStripMenuItem item in position.DropDownItems)
                item.Checked = (QuotaDock)item.Tag! == window.Settings.QuotaPosition;
            foreach (Forms.ToolStripMenuItem item in characters.DropDownItems)
                item.Checked = (PetCharacter)item.Tag! == window.Settings.Character;
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
