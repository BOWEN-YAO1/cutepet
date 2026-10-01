using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Channels;
using CutePet.Codex;
using CutePet.Core;

Console.OutputEncoding = new System.Text.UTF8Encoding(false);
var jsonOptions = new JsonSerializerOptions
{
    WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    Converters = { new JsonStringEnumConverter() }
};
string? path = null;
bool json = false, watch = false;
int interval = 60, timeout = 30;
int? samples = null;
try
{
    for (var i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--codex": path = args[++i]; break;
            case "--json": json = true; break;
            case "--watch": watch = true; break;
            case "--interval": interval = int.Parse(args[++i]); break;
            case "--timeout": timeout = int.Parse(args[++i]); break;
            case "--samples": samples = int.Parse(args[++i]); break;
            case "--help":
                Console.WriteLine("CutePet Quota Probe\n--codex <原生可执行文件> --json --watch --interval <秒，10–3600> --timeout <秒，1–300> --samples <持续读取次数>\n默认只读取一次；Ctrl+C 退出持续读取。首次登录请使用官方 Codex。");
                return 0;
            default: throw new ArgumentException();
        }
    }
    if (interval is < 10 or > 3600 || timeout is < 1 or > 300 || samples < 1 || samples is not null && !watch)
        throw new ArgumentException();
    jsonOptions.WriteIndented = !watch;
}
catch (Exception ex) when (ex is ArgumentException or IndexOutOfRangeException or FormatException or OverflowException)
{
    Console.Error.WriteLine("参数无效。请使用 --help 查看说明。");
    return 2;
}

using var stop = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; stop.Cancel(); };
// Notifications request a fresh read, never directly replace a partial snapshot.
var signals = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
    { FullMode = BoundedChannelFullMode.DropWrite, SingleReader = true });
try
{
    await using var reader = await CodexQuotaReader.ConnectAsync(path, TimeSpan.FromSeconds(timeout), stop.Token);
    reader.QuotaInvalidated += () => signals.Writer.TryWrite(true);
    var failures = 0;
    var observations = 0;
    do
    {
        while (signals.Reader.TryRead(out _)) { }
        try
        {
            var snapshot = await reader.ReadAsync(stop.Token);
            failures = 0;
            if (json) Console.WriteLine(JsonSerializer.Serialize(new { status = "connected", snapshot }, jsonOptions));
            else PrintSnapshot(snapshot);
        }
        catch (QuotaException ex)
        {
            PrintError(ex);
            if (!watch || ex.Failure != QuotaFailure.ServiceError && ex.Failure != QuotaFailure.Timeout)
                return 1;
            failures++;
        }
        if (!watch || samples is int count && ++observations >= count) break;
        // At least 10 seconds between reads, including repeated notifications and failed reads.
        var delay = failures > 0 ? Math.Min(300, interval * Math.Pow(2, Math.Min(failures - 1, 5))) : interval;
        if (failures > 0) delay += Random.Shared.NextDouble() * 3;
        using var wait = CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
        var timer = Task.Delay(TimeSpan.FromSeconds(delay), wait.Token);
        var changed = signals.Reader.WaitToReadAsync(wait.Token).AsTask();
        var startedWaiting = System.Diagnostics.Stopwatch.StartNew();
        if (failures > 0) await timer;
        else await Task.WhenAny(timer, changed);
        wait.Cancel();
        try { await Task.WhenAll(timer, changed); } catch (OperationCanceledException) { }
        var minimumWait = TimeSpan.FromSeconds(10) - startedWaiting.Elapsed;
        if (minimumWait > TimeSpan.Zero) await Task.Delay(minimumWait, stop.Token);
    } while (!stop.IsCancellationRequested);
    return 0;
}
catch (OperationCanceledException) when (stop.IsCancellationRequested) { return 0; }
catch (QuotaException ex) { PrintError(ex); return 1; }

void PrintError(QuotaException ex)
{
    if (json) Console.WriteLine(JsonSerializer.Serialize(new { status = "error", failure = ex.Failure,
        message = ex.Message, rpcCode = ex.RpcCode, isStale = true }, jsonOptions));
    else Console.Error.WriteLine($"[{ex.Failure}] {ex.Message}");
}

void PrintSnapshot(QuotaSnapshot snapshot)
{
    Console.WriteLine($"同步时间：{snapshot.LastSuccessfulSyncUtc.ToLocalTime():yyyy-MM-dd HH:mm:ss zzz}  套餐：{snapshot.PlanType ?? "未知"}");
    foreach (var bucket in snapshot.Buckets)
    {
        Console.WriteLine($"额度桶：{bucket.DisplayName ?? bucket.LimitId}");
        if (bucket.Windows.Count == 0) Console.WriteLine("  暂无额度窗口数据");
        foreach (var window in bucket.Windows)
        {
            var label = window.WindowDurationMinutes switch
            {
                300 => "5 小时", 10080 => "每周", int minutes => $"{minutes} 分钟", _ => window.Kind
            };
            var remaining = window.RemainingPercent is double n ? $"{n:0.##}%" : "暂无数据";
            var reset = window.ResetsAtUtc?.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss zzz") ?? "暂无数据";
            Console.WriteLine($"  {label}：剩余 {remaining}；重置 {reset}");
        }
    }
}
