using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace CutePet.Codex;

public sealed class StdioRpcClient : IAsyncDisposable
{
    private readonly Process process;
    private readonly TimeSpan timeout;
    private readonly CancellationTokenSource lifetime = new();
    private readonly SemaphoreSlim writer = new(1, 1);
    private readonly ConcurrentDictionary<long, TaskCompletionSource<JsonElement>> pending = new();
    private readonly Task readTask;
    private readonly Task stderrTask;
    private long nextId;
    private int closed;
    private int disposed;
    public event Action<string>? Notification;

    public StdioRpcClient(ProcessStartInfo start, TimeSpan timeout)
    {
        if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        this.timeout = timeout;
        start.UseShellExecute = false;
        start.CreateNoWindow = true;
        start.RedirectStandardInput = start.RedirectStandardOutput = start.RedirectStandardError = true;
        start.StandardInputEncoding = new UTF8Encoding(false);
        start.StandardOutputEncoding = Encoding.UTF8;
        start.StandardErrorEncoding = Encoding.UTF8;
        process = new Process { StartInfo = start };
        try
        {
            if (!process.Start()) throw new InvalidOperationException();
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            process.Dispose();
            throw new QuotaException(QuotaFailure.MissingDependency, "无法启动 Codex 可执行文件。");
        }
        readTask = ReadAsync();
        stderrTask = DrainErrorAsync();
    }

    public async Task<JsonElement> RequestAsync(string method, object? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfClosed();
        var id = Interlocked.Increment(ref nextId);
        var completion = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        pending[id] = completion;
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, lifetime.Token);
        deadline.CancelAfter(timeout);
        try
        {
            ThrowIfClosed();
            await WriteAsync(new { id, method, @params = parameters }, deadline.Token);
            return await completion.Task.WaitAsync(deadline.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new QuotaException(Volatile.Read(ref closed) != 0
                ? QuotaFailure.ConnectionClosed : QuotaFailure.Timeout,
                "Codex 连接已关闭或请求超时；请检查网络、登录状态和 CLI 版本。");
        }
        finally { pending.TryRemove(id, out _); }
    }

    public Task NotifyAsync(string method, CancellationToken token = default) =>
        WriteAsync(new { method }, token);

    private async Task WriteAsync(object message, CancellationToken token)
    {
        await writer.WaitAsync(token);
        try
        {
            ThrowIfClosed();
            await process.StandardInput.WriteLineAsync(JsonSerializer.Serialize(message).AsMemory(), token);
            await process.StandardInput.FlushAsync(token);
        }
        catch (IOException)
        {
            throw new QuotaException(QuotaFailure.ConnectionClosed, "Codex 输入连接已关闭。");
        }
        finally { writer.Release(); }
    }

    private async Task ReadAsync()
    {
        QuotaException failure = new(QuotaFailure.ConnectionClosed, "Codex 后台进程已结束。");
        try
        {
            while (await process.StandardOutput.ReadLineAsync(lifetime.Token) is { } line)
            {
                using var document = JsonDocument.Parse(line);
                var message = document.RootElement;
                if (message.ValueKind != JsonValueKind.Object) throw new JsonException();
                if (message.TryGetProperty("method", out var method))
                {
                    if (message.TryGetProperty("id", out var serverId))
                    {
                        // Do not approve unexpected server requests or request fresh credentials.
                        await WriteAsync(new { id = serverId.Clone(), error = new { code = -32601,
                            message = "Read-only quota client does not support server requests" } }, lifetime.Token);
                    }
                    else if (method.ValueKind == JsonValueKind.String)
                        Notification?.Invoke(method.GetString()!);
                    continue;
                }
                if (!message.TryGetProperty("id", out var id) || id.ValueKind != JsonValueKind.Number
                    || !id.TryGetInt64(out var requestId)
                    || !pending.TryRemove(requestId, out var completion)) continue;
                if (message.TryGetProperty("error", out var error))
                {
                    int? code = error.ValueKind == JsonValueKind.Object && error.TryGetProperty("code", out var c)
                        && c.ValueKind == JsonValueKind.Number && c.TryGetInt32(out var n) ? n : null;
                    // Never surface raw server error strings, which may contain account details.
                    completion.TrySetException(new QuotaException(code == -32601
                        ? QuotaFailure.IncompatibleProtocol : QuotaFailure.ServiceError,
                        "官方额度请求失败；请检查网络、登录状态和 CLI 兼容性。", code));
                }
                else if (message.TryGetProperty("result", out var result))
                    completion.TrySetResult(result.Clone());
                else completion.TrySetException(new QuotaException(QuotaFailure.IncompatibleProtocol,
                    "Codex 响应缺少 result/error。"));
            }
        }
        catch (JsonException) { failure = new(QuotaFailure.IncompatibleProtocol, "Codex 返回了无法识别的协议消息。"); }
        catch (Exception ex) when (ex is OperationCanceledException or IOException or QuotaException) { }
        finally
        {
            Interlocked.Exchange(ref closed, 1);
            foreach (var pair in pending)
                if (pending.TryRemove(pair.Key, out var completion)) completion.TrySetException(failure);
        }
    }

    private async Task DrainErrorAsync()
    {
        // Drain to avoid pipe deadlock; intentionally do not retain or publish diagnostics.
        var buffer = new char[4096];
        try { while (await process.StandardError.ReadAsync(buffer.AsMemory(), lifetime.Token) > 0) { } }
        catch (Exception ex) when (ex is IOException or OperationCanceledException) { }
    }

    private void ThrowIfClosed()
    {
        if (Volatile.Read(ref closed) != 0 || Volatile.Read(ref disposed) != 0)
            throw new QuotaException(QuotaFailure.ConnectionClosed, "Codex 连接已关闭。");
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref disposed, 1) != 0) return;
        lifetime.Cancel();
        // This Process is exclusively created by this client; never kill another Codex instance.
        try { if (!process.HasExited) process.Kill(entireProcessTree: true); }
        catch (InvalidOperationException) { }
        await Task.WhenAll(readTask, stderrTask);
        process.Dispose();
        lifetime.Dispose();
        writer.Dispose();
    }
}
