using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using Forms = System.Windows.Forms;

namespace CutePet.Desktop;

internal static class NativePlacement
{
    [StructLayout(LayoutKind.Sequential)] private struct Bounds { public int Left, Top, Right, Bottom; }
    [DllImport("user32.dll")] private static extern bool GetWindowRect(IntPtr window, out Bounds bounds);
    [DllImport("user32.dll")] private static extern bool SetWindowPos(IntPtr window, IntPtr after,
        int x, int y, int width, int height, uint flags);

    // Store physical pixels, avoiding invalid DIP coordinates after a monitor/DPI change.
    public static (double Left, double Top) Get(Window window)
    {
        GetWindowRect(new WindowInteropHelper(window).Handle, out var bounds);
        return (bounds.Left, bounds.Top);
    }

    public static void Apply(Window window, double? left, double? top)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero || !GetWindowRect(handle, out var bounds)) return;
        var width = bounds.Right - bounds.Left;
        var height = bounds.Bottom - bounds.Top;
        var screens = Forms.Screen.AllScreens.OrderByDescending(s => s.Primary).ToArray();
        var x = left is double l ? (int)Math.Clamp(l, int.MinValue, int.MaxValue) : screens[0].WorkingArea.Right - width - 24;
        var y = top is double t ? (int)Math.Clamp(t, int.MinValue, int.MaxValue) : screens[0].WorkingArea.Bottom - height - 24;
        var desired = new System.Drawing.Rectangle(x, y, width, height);
        var screen = screens.OrderByDescending(s => Overlap(desired, s.WorkingArea)).First();
        if (Overlap(desired, screen.WorkingArea) == 0)
        {
            screen = screens[0];
            x = screen.WorkingArea.Right - width - 24;
            y = screen.WorkingArea.Bottom - height - 24;
        }
        x = Math.Clamp(x, screen.WorkingArea.Left, Math.Max(screen.WorkingArea.Left, screen.WorkingArea.Right - width));
        y = Math.Clamp(y, screen.WorkingArea.Top, Math.Max(screen.WorkingArea.Top, screen.WorkingArea.Bottom - height));
        SetWindowPos(handle, IntPtr.Zero, x, y, 0, 0, 0x0001 | 0x0004 | 0x0010);
    }

    private static long Overlap(System.Drawing.Rectangle a, System.Drawing.Rectangle b)
    {
        var overlap = System.Drawing.Rectangle.Intersect(a, b);
        return (long)Math.Max(overlap.Width, 0) * Math.Max(overlap.Height, 0);
    }

    public static void MoveUnclamped(Window window, double left, double top)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle != IntPtr.Zero)
            SetWindowPos(handle, IntPtr.Zero, (int)Math.Round(left), (int)Math.Round(top), 0, 0, 0x0001 | 0x0004 | 0x0010);
    }

    internal static (double Start, double Target, double Top) PlanDrift(Window window, double distance, int direction)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero || !GetWindowRect(handle, out var bounds)) return (0, direction * distance, 0);
        var area = Forms.Screen.FromHandle(handle).WorkingArea;
        var width = bounds.Right - bounds.Left;
        var height = bounds.Bottom - bounds.Top;
        var start = Math.Clamp(bounds.Left, area.Left, Math.Max(area.Left, area.Right - width));
        return (start, CloudFlight.Target(start, width, area.Left, area.Right, distance, direction),
            Math.Clamp(bounds.Top, area.Top, Math.Max(area.Top, area.Bottom - height)));
    }

    internal static bool PointerNear(Window window, int margin)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero || !GetWindowRect(handle, out var bounds)) return true;
        var point = Forms.Cursor.Position;
        return point.X >= bounds.Left - margin && point.X <= bounds.Right + margin
            && point.Y >= bounds.Top - margin && point.Y <= bounds.Bottom + margin;
    }
    internal static (Rect Area, Size Size, Point Origin) RoamingBounds(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        var screen = handle == IntPtr.Zero ? Forms.Screen.PrimaryScreen! : Forms.Screen.FromHandle(handle);
        var area = screen.WorkingArea;
        var dpi = VisualTreeHelper.GetDpi(window);
        var size = new Size(window.Width * dpi.DpiScaleX, window.Height * dpi.DpiScaleY);
        var origin = new Point();
        if (handle != IntPtr.Zero && GetWindowRect(handle, out var bounds))
        { size = new(bounds.Right - bounds.Left, bounds.Bottom - bounds.Top); origin = new(bounds.Left, bounds.Top); }
        return (new Rect(area.Left, area.Top, area.Width, area.Height), size, origin);
    }

    public static void SetPopupTopmost(FrameworkElement content, bool topmost)
    {
        if (PresentationSource.FromVisual(content) is HwndSource source)
            SetWindowPos(source.Handle, new IntPtr(topmost ? -1 : -2), 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010);
    }
}
