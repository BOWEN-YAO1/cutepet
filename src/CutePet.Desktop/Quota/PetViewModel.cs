using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using CutePet.Core;

namespace CutePet.Desktop;

public sealed record WindowRow(string Label, string RemainingText, double Progress,
    double Opacity, Brush Accent, DateTimeOffset? ResetsAtUtc) : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public string ShortLabel
    {
        get
        {
            var separator = Label.LastIndexOf(" · ", StringComparison.Ordinal);
            var label = separator >= 0 ? Label[(separator + 3)..] : Label;
            return label.Replace(" 小时额度", "h").Replace("每周额度", "周")
                .Replace(" 分钟额度", "m").Replace("额度窗口", "额度");
        }
    }
    public string ResetText => ResetsAtUtc is not DateTimeOffset reset ? "重置时间暂无数据"
        : reset <= DateTimeOffset.UtcNow ? "等待官方额度更新"
        : (reset - DateTimeOffset.UtcNow).TotalDays >= 1
            ? $"约 {(reset - DateTimeOffset.UtcNow).Days} 天 {(reset - DateTimeOffset.UtcNow).Hours} 小时后重置"
            : $"约 {(int)(reset - DateTimeOffset.UtcNow).TotalHours} 小时 {(reset - DateTimeOffset.UtcNow).Minutes} 分钟后重置";
    public void Tick() => PropertyChanged?.Invoke(this, new(nameof(ResetText)));
}

public sealed class PetViewModel : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;
    public ObservableCollection<WindowRow> Windows { get; } = new();
    // Detailed rows retain the last snapshot. Without a status line, stale compact values must be unknown.
    public IEnumerable<WindowRow> CompactWindows => Windows.Take(2).Select(row => IsStale
        ? row with { RemainingText = "—", Accent = Brushes.Gray } : row);
    public string MoreWindowsText => Windows.Count > 2 ? $" +{Windows.Count - 2}" : "";
    private string statusText = "正在连接 Codex…";
    private string footerText = "正在等待首次额度同步";
    private Brush statusBrush = Brushes.DarkSeaGreen;
    private bool canRefresh = true;
    private string characterMessage = "我来帮你看额度";
    public bool HasData { get; private set; }
    public bool IsStale { get; private set; }
    public bool IsLow { get; private set; }
    public string StatusText { get => statusText; private set { statusText = value; Changed(); Changed(nameof(CompactStatus)); } }
    public string CompactStatus => StatusText switch
    {
        "已同步官方额度" => "已同步",
        "演示数据 · 用于界面验证" => "演示数据",
        "官方提示：当前使用受限" => "使用受限",
        "未同步 · 当前为上次数据" => "上次数据",
        "暂未取得额度" => "未连接",
        "正在刷新 · 当前为上次数据" => "刷新中 · 上次数据",
        _ => "连接中"
    };
    public string FooterText { get => footerText; private set { footerText = value; Changed(); } }
    public Brush StatusBrush { get => statusBrush; private set { statusBrush = value; Changed(); } }
    public bool CanRefresh { get => canRefresh; set { canRefresh = value; Changed(); } }
    public string CharacterMessage { get => characterMessage; set { characterMessage = value; Changed(); Changed(nameof(ShowCharacterMessage)); } }
    public bool ShowCharacterMessage => IsStale || IsLow || CharacterMessage != "我来帮你看额度";

    public PetViewModel() => ShowUnknown();
    private void ShowUnknown()
    {
        Windows.Clear();
        Windows.Add(new("额度窗口", "—", 0, 0.4, Brush("#91A397"), null));
        CompactChanged();
    }

    public void Loading()
    {
        CanRefresh = false;
        StatusText = HasData ? "正在刷新 · 当前为上次数据" : "正在连接 Codex…";
        StatusBrush = Brush("#AABBA7");
    }

    public void Apply(QuotaSnapshot snapshot, bool demo = false)
    {
        Windows.Clear();
        var multiple = snapshot.Buckets.Count > 1;
        foreach (var bucket in snapshot.Buckets)
        foreach (var window in bucket.Windows)
        {
            var label = window.WindowDurationMinutes switch { 300 => "5 小时额度", 10080 => "每周额度",
                int n => $"{n} 分钟额度", _ => "额度窗口" };
            if (multiple) label = (bucket.DisplayName ?? bucket.LimitId) + " · " + label;
            Windows.Add(new(label, window.RemainingPercent is double remaining ? $"{remaining:0.#}%" : "—",
                window.RemainingPercent ?? 0, window.RemainingPercent is null ? 0.4 : 1,
                Brush(window.RemainingPercent < 20 ? "#C18651" : "#568B70"), window.ResetsAtUtc));
        }
        if (Windows.Count == 0) ShowUnknown();
        HasData = true;
        IsStale = false;
        CompactChanged();
        IsLow = snapshot.Buckets.SelectMany(b => b.Windows).Any(w => w.RemainingPercent < 20);
        StatusText = demo ? "演示数据 · 用于界面验证" : snapshot.OrdinaryUsageAllowed == false ? "官方提示：当前使用受限" : "已同步官方额度";
        FooterText = demo ? "界面预览 · 当前为演示额度" : $"更新于 {snapshot.LastSuccessfulSyncUtc.ToLocalTime():HH:mm:ss}";
        StatusBrush = Brush(IsLow ? "#C18651" : "#568B70");
        CanRefresh = true;
        RestoreCharacterMessage();
    }

    public void Failure(string message, bool clear)
    {
        if (clear) { HasData = false; IsLow = false; ShowUnknown(); }
        IsStale = true;
        CompactChanged();
        StatusText = HasData ? "未同步 · 当前为上次数据" : "暂未取得额度";
        FooterText = message;
        StatusBrush = Brush("#C18651");
        CanRefresh = true;
        RestoreCharacterMessage();
    }

    public void RestoreCharacterMessage() => CharacterMessage = IsStale ? "等连接恢复再看哦" : IsLow ? "额度快用完啦" : "我来帮你看额度";
    public void Tick() { foreach (var window in Windows) window.Tick(); }
    private void CompactChanged() { Changed(nameof(CompactWindows)); Changed(nameof(MoreWindowsText)); }
    private static Brush Brush(string value) => new SolidColorBrush((Color)ColorConverter.ConvertFromString(value));
    private void Changed([CallerMemberName] string? name = null) => PropertyChanged?.Invoke(this, new(name));
}
