using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using CutePet.Codex;
using CutePet.Core;

namespace CutePet.Desktop;

internal sealed class QuotaSession(string? codexPath)
{
    private readonly Channel<bool> refresh = Channel.CreateBounded<bool>(new BoundedChannelOptions(1)
        { FullMode = BoundedChannelFullMode.DropWrite });
    public event Action? Loading;
    public event Action<QuotaSnapshot>? Updated;
    public event Action<string, bool>? Failed;
    public void Refresh() => refresh.Writer.TryWrite(true);

    public async Task RunAsync(CancellationToken stop)
    {
        CodexQuotaReader? reader = null;
        var failures = 0;
        try
        {
            while (!stop.IsCancellationRequested)
            {
                while (refresh.Reader.TryRead(out _)) { }
                Loading?.Invoke();
                try
                {
                    if (reader is null)
                    {
                        reader = await CodexQuotaReader.ConnectAsync(codexPath, cancellationToken: stop);
                        reader.QuotaInvalidated += Refresh;
                        reader.AccountInvalidated += AccountChanged;
                    }
                    Updated?.Invoke(await reader.ReadAsync(stop));
                    failures = 0;
                }
                catch (QuotaException ex)
                {
                    failures++;
                    // No persisted quota cache. Identity/auth/transport failures clear in-memory data.
                    var clear = ex.Failure is not (QuotaFailure.Timeout or QuotaFailure.ServiceError);
                    Failed?.Invoke(ex.Failure switch
                    {
                        QuotaFailure.MissingDependency => "未找到 Codex · 右键选择程序路径",
                        QuotaFailure.NeedsLogin => "请先在官方 Codex 中登录",
                        QuotaFailure.UnsupportedAuth => "请使用 ChatGPT 登录读取订阅额度",
                        QuotaFailure.IncompatibleProtocol => "接口不兼容 · 请核对 Codex 版本",
                        QuotaFailure.AccountChanged => "账号变化 · 正在重新连接",
                        _ => "读取失败 · 稍后重试或手动刷新"
                    }, clear);
                    if (reader is not null)
                    {
                        reader.QuotaInvalidated -= Refresh;
                        reader.AccountInvalidated -= AccountChanged;
                        await reader.DisposeAsync();
                        reader = null;
                    }
                }
                var waitSeconds = failures == 0 ? 60 : Math.Min(300, 60 * Math.Pow(2, Math.Min(failures - 1, 3)));
                using var wait = CancellationTokenSource.CreateLinkedTokenSource(stop);
                var elapsed = System.Diagnostics.Stopwatch.StartNew();
                var timer = Task.Delay(TimeSpan.FromSeconds(waitSeconds), wait.Token);
                var requested = refresh.Reader.WaitToReadAsync(wait.Token).AsTask();
                await Task.WhenAny(timer, requested);
                wait.Cancel();
                try { await Task.WhenAll(timer, requested); } catch (OperationCanceledException) { }
                var throttle = TimeSpan.FromSeconds(10) - elapsed.Elapsed;
                if (throttle > TimeSpan.Zero) await Task.Delay(throttle, stop);
            }
        }
        catch (OperationCanceledException) when (stop.IsCancellationRequested) { }
        finally
        {
            if (reader is not null) { reader.QuotaInvalidated -= Refresh; reader.AccountInvalidated -= AccountChanged; await reader.DisposeAsync(); }
        }
    }

    private void AccountChanged() => Failed?.Invoke("账号更新中 · 重新读取额度", true);
}
