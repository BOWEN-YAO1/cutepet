using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using CutePet.Codex;
using CutePet.Core;

if (args.Length > 0 && args[0] == "--fake-server")
{
    await FakeServer(args[1]);
    return 0;
}

var tests = new (string Name, Func<Task> Run)[]
{
    ("多额度桶优先、缺失比例不填零、无权限不推断恢复", () => Sync(() =>
    {
        var snapshot = Parse("""{"ordinaryUsageAllowed":false,"rateLimits":{"primary":{"usedPercent":90}},"rateLimitsByLimitId":{"one":{"primary":{"usedPercent":125,"windowDurationMins":15,"resetsAt":999999999999999}},"two":{"primary":{"usedPercent":null},"secondary":null}},"accountId":"private","extra":true}""");
        Check(snapshot.Buckets.Count == 2 && snapshot.Buckets[0].Windows[0].RemainingPercent == 0);
        Check(snapshot.Buckets[0].Windows[0].ResetsAtUtc is null);
        Check(snapshot.Buckets[1].Windows[0].RemainingPercent is null && snapshot.OrdinaryUsageAllowed == false);
        Check(!JsonSerializer.Serialize(snapshot).Contains("private"));
    })),
    ("单桶兼容、零消耗、未知窗口、空数据", () => Sync(() =>
    {
        var snapshot = Parse("""{"rateLimitsByLimitId":{},"rateLimits":{"primary":{"usedPercent":0,"windowDurationMins":300,"resetsAt":1790852322},"secondary":{"usedPercent":-5,"windowDurationMins":null}}}""");
        Check(snapshot.Buckets[0].Windows.All(w => w.RemainingPercent == 100));
        Check(snapshot.Buckets[0].Windows[1].WindowDurationMinutes is null);
        var empty = Parse("""{"rateLimits":{"primary":null,"secondary":null}}""");
        Check(empty.Buckets[0].Windows.Count == 0);
        try { Parse("{}"); throw new Exception("Expected invalid payload"); } catch (FormatException) { }
    })),
    ("真实子进程握手、初始账号通知、脱敏快照", async () =>
    {
        await using var reader = await CodexQuotaReader.ConnectAsync(Start("normal"), TimeSpan.FromSeconds(3));
        var snapshot = await reader.ReadAsync();
        Check(snapshot.Buckets[0].Windows[0].RemainingPercent == 75);
        Check(!JsonSerializer.Serialize(snapshot).Contains("private"));
    }),
    ("未登录和 API key 认证分开处理", async () =>
    {
        foreach (var (mode, failure) in new[] { ("logged-out", QuotaFailure.NeedsLogin), ("api-key", QuotaFailure.UnsupportedAuth) })
        {
            await using var reader = await CodexQuotaReader.ConnectAsync(Start(mode), TimeSpan.FromSeconds(3));
            await Expect(failure, () => reader.ReadAsync());
        }
    }),
    ("读取中账号切换不发布混合快照", async () =>
    {
        await using var reader = await CodexQuotaReader.ConnectAsync(Start("account-changing"), TimeSpan.FromSeconds(3));
        var invalidations = 0;
        reader.AccountInvalidated += () => Interlocked.Increment(ref invalidations);
        await Expect(QuotaFailure.AccountChanged, () => reader.ReadAsync());
        Check(invalidations >= 2);
    }),
    ("账号响应形状变化给出兼容性错误", async () =>
    {
        await using var reader = await CodexQuotaReader.ConnectAsync(Start("bad-account"), TimeSpan.FromSeconds(3));
        await Expect(QuotaFailure.IncompatibleProtocol, () => reader.ReadAsync());
    }),
    ("额度通知仅触发重新读取", async () =>
    {
        await using var reader = await CodexQuotaReader.ConnectAsync(Start("quota-notify"), TimeSpan.FromSeconds(3));
        var notices = 0;
        reader.QuotaInvalidated += () => Interlocked.Increment(ref notices);
        var first = await reader.ReadAsync();
        var second = await reader.ReadAsync();
        Check(notices > 0 && first.Buckets[0].Windows[0].RemainingPercent == 75
            && second.Buckets[0].Windows[0].RemainingPercent == 70);
    }),
    ("交错响应按请求 ID 匹配", async () =>
    {
        await using var rpc = new StdioRpcClient(Start("normal"), TimeSpan.FromSeconds(3));
        var first = rpc.RequestAsync("pair", new { marker = 1 });
        var second = rpc.RequestAsync("pair", new { marker = 2 });
        Check((await first).GetProperty("marker").GetInt32() == 1);
        Check((await second).GetProperty("marker").GetInt32() == 2);
    }),
    ("超时后迟到响应不污染下一请求", async () =>
    {
        await using var rpc = new StdioRpcClient(Start("normal"), TimeSpan.FromMilliseconds(500));
        await Expect(QuotaFailure.Timeout, () => rpc.RequestAsync("hang"));
        Check((await rpc.RequestAsync("ping")).GetProperty("ok").GetBoolean());
    }),
    ("取消单个请求不关闭连接", async () =>
    {
        await using var rpc = new StdioRpcClient(Start("normal"), TimeSpan.FromSeconds(3));
        using var stop = new CancellationTokenSource();
        var waiting = rpc.RequestAsync("hang", cancellationToken: stop.Token);
        stop.Cancel();
        try { await waiting; throw new Exception("Expected cancellation"); } catch (OperationCanceledException) { }
        Check((await rpc.RequestAsync("ping")).GetProperty("ok").GetBoolean());
    }),
    ("服务错误不泄露原始报错内容", async () =>
    {
        await using var rpc = new StdioRpcClient(Start("normal"), TimeSpan.FromSeconds(3));
        var failure = await Expect(QuotaFailure.ServiceError, () => rpc.RequestAsync("error"));
        Check(failure.RpcCode == 401 && !failure.Message.Contains("secret"));
    }),
    ("畸形协议和子进程退出及时结束请求", async () =>
    {
        foreach (var (method, failure) in new[] { ("malformed", QuotaFailure.IncompatibleProtocol), ("crash", QuotaFailure.ConnectionClosed) })
        {
            await using var rpc = new StdioRpcClient(Start("normal"), TimeSpan.FromSeconds(3));
            await Expect(failure, () => rpc.RequestAsync(method));
        }
    }),
    ("拒绝意外服务器请求、清理自己创建的进程", async () =>
    {
        var rpc = new StdioRpcClient(Start("normal"), TimeSpan.FromSeconds(3));
        var pid = (await rpc.RequestAsync("server-request")).GetProperty("pid").GetInt32();
        await rpc.DisposeAsync();
        await rpc.DisposeAsync();
        try { using var child = Process.GetProcessById(pid); Check(child.HasExited); }
        catch (ArgumentException) { }
    }),
    ("缺少依赖给出明确状态", () => Sync(() =>
    {
        try { CodexLocator.Find(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".exe")); throw new Exception("Expected missing dependency"); }
        catch (QuotaException e) { Check(e.Failure == QuotaFailure.MissingDependency); }
    }))
};

var failed = 0;
foreach (var test in tests)
{
    try { await test.Run(); Console.WriteLine($"PASS {test.Name}"); }
    catch (Exception e) { failed++; Console.Error.WriteLine($"FAIL {test.Name}: {e.GetType().Name}: {e.Message}"); }
}
Console.WriteLine($"{tests.Length - failed}/{tests.Length} passed");
return failed == 0 ? 0 : 1;

static QuotaSnapshot Parse(string json)
{
    using var doc = JsonDocument.Parse(json);
    return QuotaParser.Parse(doc.RootElement);
}
static Task Sync(Action action) { action(); return Task.CompletedTask; }
static void Check(bool condition) { if (!condition) throw new Exception("Assertion failed"); }
static async Task<QuotaException> Expect(QuotaFailure failure, Func<Task> run)
{
    try { await run(); }
    catch (QuotaException e) { Check(e.Failure == failure); return e; }
    throw new Exception($"Expected {failure}");
}
static ProcessStartInfo Start(string mode)
{
    var path = Environment.ProcessPath ?? throw new Exception("No process path");
    var start = new ProcessStartInfo(path);
    if (Path.GetFileNameWithoutExtension(path).Equals("dotnet", StringComparison.OrdinalIgnoreCase))
        start.ArgumentList.Add(Assembly.GetExecutingAssembly().Location);
    start.ArgumentList.Add("--fake-server");
    start.ArgumentList.Add(mode);
    return start;
}

static async Task FakeServer(string mode)
{
    JsonElement? firstPair = null;
    JsonElement? serverRequest = null;
    JsonElement? lateRequest = null;
    var initialized = false;
    var initialAccount = true;
    var quotaReads = 0;
    while (await Console.In.ReadLineAsync() is { } line)
    {
        using var document = JsonDocument.Parse(line);
        var request = document.RootElement;
        if (!request.TryGetProperty("method", out var method))
        {
            Check(request.GetProperty("error").GetProperty("code").GetInt32() == -32601);
            if (serverRequest is { } saved) Send(saved, new { pid = Environment.ProcessId });
            continue;
        }
        switch (method.GetString())
        {
            case "initialize": Send(request, new { userAgent = "fake" }); break;
            case "initialized": initialized = true; break;
            case "account/read":
                Check(initialized);
                if (mode == "bad-account") { Send(request, "wrong shape"); break; }
                if (initialAccount)
                {
                    Console.WriteLine("""{"method":"account/updated","params":{"authMode":"chatgpt"}}""");
                    initialAccount = false;
                }
                Send(request, new { account = mode == "logged-out" ? null : (object)new
                { type = mode == "api-key" ? "apiKey" : "chatgpt", email = "private@example.invalid", planType = "plus" } });
                break;
            case "account/rateLimits/read":
                Check(initialized);
                if (mode == "account-changing") Console.WriteLine("""{"method":"account/updated","params":{}}""");
                if (mode == "quota-notify") Console.WriteLine("""{"method":"account/rateLimits/updated","params":{"rateLimits":{"primary":{"usedPercent":99}}}}""");
                Send(request, new { accountId = "private", rateLimits = new
                { limitId = "codex", primary = new { usedPercent = quotaReads++ == 0 ? 25 : 30, windowDurationMins = 300, resetsAt = 1790852322 } } });
                break;
            case "pair":
                if (firstPair is null) firstPair = request.Clone();
                else { Send(request, request.GetProperty("params")); Send(firstPair.Value, firstPair.Value.GetProperty("params")); }
                break;
            case "hang": lateRequest = request.Clone(); break;
            case "ping":
                if (lateRequest is { } late) { Send(late, new { wrong = true }); lateRequest = null; }
                Send(request, new { ok = true }); break;
            case "error": Console.WriteLine(JsonSerializer.Serialize(new
            { id = request.GetProperty("id"), error = new { code = 401, message = "secret-token private@example.invalid" } })); break;
            case "malformed": Console.WriteLine("not JSON"); break;
            case "crash": return;
            case "server-request":
                serverRequest = request.Clone();
                Console.WriteLine("""{"id":"server-1","method":"account/chatgptAuthTokens/refresh","params":{}}""");
                break;
        }
    }
    static void Send(JsonElement request, object result) => Console.WriteLine(JsonSerializer.Serialize(
        new { id = request.GetProperty("id"), result }));
}
