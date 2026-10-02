using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CutePet.Desktop;

// Render both real WPF trees into one review image; no desktop screenshot or user window movement.
internal static class WindowPreview
{
    internal static RenderTargetBitmap Capture(MainWindow window, double dpi, double? motion = null, double width = 0, double height = 0)
    {
        var card = window.QuotaHost;
        var petImage = Surface((FrameworkElement)window.Content, window.Width, window.Height, dpi);
        var cardImage = Surface((FrameworkElement)card.Content, card.Width, card.Height, dpi);
        width = width > 0 ? width : window.Width + card.Width + 24;
        height = height > 0 ? height : Math.Max(window.Height, card.Height) + 8;
        var scene = new DrawingVisual();
        using (var draw = scene.RenderOpen())
        {
            draw.DrawRectangle(new SolidColorBrush(Color.FromRgb(239, 245, 244)), null, new Rect(0, 0, width, height));
            draw.DrawImage(cardImage, new Rect(8, Math.Max(0, height - card.Height - 8), card.Width, card.Height));
            draw.DrawImage(petImage, new Rect(motion is double x ? 250 + x : card.Width + 16, 0, window.Width, window.Height));
        }
        var result = new RenderTargetBitmap((int)Math.Ceiling(width * dpi / 96), (int)Math.Ceiling(height * dpi / 96), dpi, dpi, PixelFormats.Pbgra32);
        result.Render(scene);
        return result;
    }
    internal static RenderTargetBitmap Surface(FrameworkElement visual, double width, double height, double dpi)
    {
        visual.Measure(new Size(width, height));
        visual.Arrange(new Rect(0, 0, width, height));
        visual.UpdateLayout();
        var bitmap = new RenderTargetBitmap((int)Math.Ceiling(width * dpi / 96), (int)Math.Ceiling(height * dpi / 96), dpi, dpi, PixelFormats.Pbgra32);
        bitmap.Render(visual);
        return bitmap;
    }
    internal static RenderTargetBitmap CaptureRoaming(MainWindow window, Point position, Rect area)
    {
        const double width = 800, height = 480;
        var scale = Math.Min((width - 24) / area.Width, (height - 40) / area.Height);
        var card = window.QuotaHost;
        var petDpi = VisualTreeHelper.GetDpi(window);
        var cardDpi = VisualTreeHelper.GetDpi(card);
        var scene = new DrawingVisual();
        using (var draw = scene.RenderOpen())
        {
            draw.DrawRectangle(new SolidColorBrush(Color.FromRgb(239, 245, 244)), null, new Rect(0, 0, width, height));
            draw.DrawRectangle(null, new Pen(new SolidColorBrush(Color.FromRgb(192, 209, 201)), 1),
                new Rect(12, 12, area.Width * scale, area.Height * scale));
            draw.DrawImage(Surface((FrameworkElement)card.Content, card.Width, card.Height, 96),
                new Rect(12 + (card.Position.X - area.Left) * scale, 12 + (card.Position.Y - area.Top) * scale,
                    card.Width * cardDpi.DpiScaleX * scale, card.Height * cardDpi.DpiScaleY * scale));
            draw.DrawImage(Surface((FrameworkElement)window.Content, window.Width, window.Height, 96),
                new Rect(12 + (position.X - area.Left) * scale, 12 + (position.Y - area.Top) * scale,
                    window.Width * petDpi.DpiScaleX * scale, window.Height * petDpi.DpiScaleY * scale));
            draw.DrawText(new FormattedText("当前屏幕 · 巡游预览（加速）", System.Globalization.CultureInfo.GetCultureInfo("zh-CN"),
                FlowDirection.LeftToRight, new Typeface("Microsoft YaHei"), 12, new SolidColorBrush(Color.FromRgb(75, 102, 88)), 1),
                new Point(12, height - 25));
        }
        var result = new RenderTargetBitmap((int)width, (int)height, 96, 96, PixelFormats.Pbgra32);
        result.Render(scene);
        return result;
    }
}
