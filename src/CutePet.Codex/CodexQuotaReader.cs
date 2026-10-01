using System.Diagnostics;
using System.Text.Json;
using CutePet.Core;

namespace CutePet.Codex;

public sealed class CodexQuotaReader : IAsyncDisposable
{
    private readonly StdioRpcClient rpc;
    private readonly SemaphoreSlim reads = new(1, 1);
    private long accountRevision;
    public event Action? QuotaInvalidated;
    public event Action? AccountInvalidated;

    private CodexQuotaReader(StdioRpcClient rpc)
    {
        this.rpc = rpc;
        rpc.Notification += HandleNotification;
    }

    public static async Task<CodexQuotaReader> ConnectAsync(string? codexPath = null,
        TimeSpan? timeout = null, CancellationToken cancellationToken = default)
    {
        var start = new ProcessStartInfo(CodexLocator.Find(codexPath));
        foreach (var arg in new[] { "-c", "analytics.enabled=false", "app-server", "--listen", "stdio://" })
            start.ArgumentList.Add(arg);
        return await ConnectAsync(start, timeout ?? TimeSpan.FromSeconds(30), cancellationToken);
    }

    // Allows process-based protocol tests without starting a real Codex account session.
    public static async Task<CodexQuotaReader> ConnectAsync(ProcessStartInfo start,
        TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var client = new CodexQuotaReader(new StdioRpcClient(start, timeout));
        try
        {
            await client.rpc.RequestAsync("initialize", new
            {
                clientInfo = new { name = "cutepet_quota_probe", title = "CutePet Quota Probe", version = "0.1.0" }
            }, cancellationToken);
            await client.rpc.NotifyAsync("initialized", cancellationToken);
            return client;
        }
        catch { await client.DisposeAsync(); throw; }
    }

    private void HandleNotification(string method)
    {
        if (method == "account/updated")
        {
            Interlocked.Increment(ref accountRevision);
            AccountInvalidated?.Invoke();
        }
        if (method is "account/updated" or "account/rateLimits/updated") QuotaInvalidated?.Invoke();
    }

    public async Task<QuotaSnapshot> ReadAsync(CancellationToken cancellationToken = default)
    {
        await reads.WaitAsync(cancellationToken);
        try
        {
            for (var attempt = 0; attempt < 2; attempt++)
            {
                var accountResult = await rpc.RequestAsync("account/read", new { refreshToken = false }, cancellationToken);
                if (accountResult.ValueKind != JsonValueKind.Object)
                    throw new QuotaException(QuotaFailure.IncompatibleProtocol, "账号数据结构不兼容。");
                if (!accountResult.TryGetProperty("account", out var account)
                    || account.ValueKind == JsonValueKind.Null)
                    throw new QuotaException(QuotaFailure.NeedsLogin,
                        "当前 Codex 未登录。请先通过官方 Codex 登录，再重试读取。");
                if (account.ValueKind != JsonValueKind.Object || QuotaParser.Text(account, "type") is null)
                    throw new QuotaException(QuotaFailure.IncompatibleProtocol, "账号数据结构不兼容。");
                if (QuotaParser.Text(account, "type") != "chatgpt")
                    throw new QuotaException(QuotaFailure.UnsupportedAuth,
                        "此模块读取 ChatGPT 订阅额度；API key 或其他认证的账单不属于该数据源。");
                // Initial credential loading may emit account/updated during account/read.
                var revision = Interlocked.Read(ref accountRevision);
                var quota = await rpc.RequestAsync("account/rateLimits/read", null, cancellationToken);
                if (revision != Interlocked.Read(ref accountRevision)) continue;
                try { return QuotaParser.Parse(quota, QuotaParser.Text(account, "planType")); }
                catch (FormatException)
                {
                    throw new QuotaException(QuotaFailure.IncompatibleProtocol, "额度数据结构不兼容，请核对 CLI 版本。");
                }
            }
            throw new QuotaException(QuotaFailure.AccountChanged, "读取期间账号持续发生变化，请重新读取。");
        }
        finally { reads.Release(); }
    }

    public async ValueTask DisposeAsync()
    {
        rpc.Notification -= HandleNotification;
        await rpc.DisposeAsync();
    }
}
