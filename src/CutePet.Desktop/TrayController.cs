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
        menu.Items.Add(new Forms.ToolStripSeparator());
        Add("移回屏幕右下角", window.ResetPosition);
        Add("退出 CutePet", exit);
        tray = new Forms.NotifyIcon { Icon = icon, Text = "CutePet · Codex 额度", ContextMenuStrip = menu, Visible = true };
        tray.DoubleClick += (_, _) => window.RestorePet();
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
