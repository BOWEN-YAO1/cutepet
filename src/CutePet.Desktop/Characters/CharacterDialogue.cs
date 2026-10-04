using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CutePet.Desktop;

// Plain text from the selected pack; elapsed time comes from the existing presentation clock.
internal sealed class CharacterDialogue
{
    internal static readonly string[] Events = { "startup", "morning", "evening", "click", "hover", "drag", "cloud", "rest", "wake", "edge-side", "edge-top", "edge-bottom", "ambient", "low", "recovery", "offline" };
    private static readonly Dictionary<string, string[]> Fallback = new()
    {
        ["startup"] = new[] { "我来陪你啦。", "今天也一起慢慢来吧。" },
        ["morning"] = new[] { "早上好，今天也请多关照。" }, ["evening"] = new[] { "晚上好，别忘了休息哦。" },
        ["click"] = new[] { "在呢，有什么新发现？", "收到啦，我就在这里。", "要一起休息一下吗？" },
        ["hover"] = new[] { "你来啦。", "我看到你啦。" }, ["drag"] = new[] { "这里也不错。", "换个地方陪你。" },
        ["cloud"] = new[] { "出发啦，去逛一小圈。", "慢慢飘，看看周围。" },
        ["rest"] = new[] { "坐下来歇一会儿。", "休息一下再继续吧。" }, ["wake"] = new[] { "休息好了，继续陪你。", "起来活动一下。" },
        ["edge-side"] = new[] { "从这里探个头。", "我在这边呢。" }, ["edge-top"] = new[] { "在上面看风景。", "这里的风景不错。" },
        ["edge-bottom"] = new[] { "在这里陪你一会儿。", "这里很舒服。" }, ["ambient"] = new[] { "慢慢来，我陪着你。", "忙累了就休息一下吧。", "我就在旁边。" },
        ["low"] = new[] { "额度有点少了，留意一下哦。", "剩余额度不多啦，可以看看详情。" },
        ["recovery"] = new[] { "额度状态更新啦，可以再看看。", "可以继续查看最新额度了。" },
        ["offline"] = new[] { "暂时没连上，等同步恢复吧。", "连接休息了一下，暂时看不到最新额度。" }
    };
    private readonly Func<int, int> choose;
    private readonly Dictionary<string, string> previous = new();
    private Dictionary<string, string[]>? lines;
    private double age, sinceDirect = double.PositiveInfinity, sinceProactive;
    private string last = "";
    internal string Text { get; private set; } = "";
    internal bool Visible => Text.Length > 0;
    internal bool Important { get; private set; }
    internal DialogueFrequency Frequency { get; set; } = DialogueFrequency.Normal;
    internal bool Proactive { get; set; } = true;
    internal double ProactiveInterval => Frequency switch { DialogueFrequency.Quiet => 180, DialogueFrequency.Lively => 45, _ => 90 };
    private double DirectInterval => Frequency switch { DialogueFrequency.Quiet => 8, DialogueFrequency.Lively => 3, _ => 5 };
    internal CharacterDialogue(Func<int, int>? choose = null) => this.choose = choose ?? Random.Shared.Next;
    internal static void Validate(Dictionary<string, string[]>? dialogue)
    {
        if (dialogue is null) return;
        var count = 0;
        if (dialogue.Count > Events.Length) throw new InvalidDataException("台词事件过多。");
        foreach (var (name, items) in dialogue)
            if (!Events.Contains(name) || items is null || items.Length is < 1 or > 12 || (count += items.Length) > 160
                || items.Any(line => string.IsNullOrWhiteSpace(line) || line.Length > 80 || line.Any(char.IsControl)))
                throw new InvalidDataException("台词事件或内容不合法：每组 1–12 句，每句最多 80 字，不允许控制字符，总数最多 160 句。");
    }
    internal void Configure(Dictionary<string, string[]>? dialogue)
    {
        Validate(dialogue); lines = dialogue; previous.Clear(); last = "";
        sinceDirect = double.PositiveInfinity; sinceProactive = 0; Clear();
    }
    internal bool Speak(string context, bool proactive = false, bool important = false)
    {
        if (!Events.Contains(context)) return false;
        if (!important && (Important && Visible || proactive && (!Proactive || sinceProactive < ProactiveInterval)
            || !proactive && sinceDirect < DirectInterval)) return false;
        var candidates = lines is not null && lines.TryGetValue(context, out var custom) ? custom : Fallback[context];
        previous.TryGetValue(context, out var prior);
        var available = candidates.Distinct().Where(line => line != prior && line != last).ToArray();
        if (available.Length == 0) available = candidates.Distinct().Where(line => line != last).ToArray();
        if (available.Length == 0) available = candidates.Distinct().ToArray();
        Text = available[Math.Clamp(choose(available.Length), 0, available.Length - 1)];
        previous[context] = last = Text; Important = important; age = 0;
        sinceDirect = sinceProactive = 0;
        return true;
    }
    internal void Advance(TimeSpan elapsed, bool allowProactive)
    {
        var seconds = double.IsFinite(elapsed.TotalSeconds) ? Math.Max(0, elapsed.TotalSeconds) : 0;
        sinceDirect += seconds;
        if (allowProactive) sinceProactive += seconds;
        if (Visible && (age += seconds) >= Math.Clamp(4 + Text.Length * .08, 4, 8)) Clear();
    }
    internal void Clear() { Text = ""; age = 0; Important = false; }
}
