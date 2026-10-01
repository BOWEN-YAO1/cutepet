# Codex 额度读取模块接入说明

版本：0.1 原型；2026-10-01。

## 1. 当前交付

- `CutePet.Core`：公开的脱敏额度模型和解析器。
- `CutePet.Codex`：原生 CLI 定位、stdio 连接、请求匹配、账号检查、额度读取、错误分类及子进程清理。
- `CutePet.QuotaProbe`：一次读取、持续观察、文本或 JSON 输出。
- `CutePet.Tests`：无需第三方测试包的可执行测试工具，包含子进程模拟协议测试。

默认使用官方 CLI 已管理的登录，不复制 auth 文件，不读取或导出密码、访问令牌。程序不调用 `thread/start` 或 `turn/start`，也不购买 credits、消耗重置券、修改或注销账号。底层 CLI 会按自身机制使用和刷新已有凭据。

## 2. 运行环境

开发需要 .NET 8 SDK，仓库使用 `global.json` 锁定 8.0.4xx 系列并允许最新补丁。此选择适配本次已有环境，正式桌宠发布前重新评估受支持的 LTS 版本。

本次仅验证 Windows 上的原生 `codex.exe`、版本 `0.159.2`。优先使用明确的 `--codex` 路径，其次查找 PATH；Windows 最后尝试本次观察到的桌面应用安装目录 `%LOCALAPPDATA%\OpenAI\Codex\bin\*\codex.exe`，选择最新修改的文件。该目录布局不是稳定接口保证，未找到时仍可手动指定。NPM 的 `.cmd` / PowerShell 启动器暂不支持，不能据此宣称所有 CLI 安装方式均兼容。

可运行发布目录依赖 .NET 8 Runtime，不需要 SDK。当前模块原型并非开发文档中尚未发布的完整桌宠便携版。

## 3. 开发者启动

在仓库根目录：

```powershell
dotnet build CutePet.sln
dotnet run --project src/CutePet.QuotaProbe --no-build
```

指定 CLI：

```powershell
dotnet run --project src/CutePet.QuotaProbe --no-build -- --codex "C:\你的目录\codex.exe"
```

读取 JSON 或持续观察：

```powershell
dotnet run --project src/CutePet.QuotaProbe --no-build -- --json
dotnet run --project src/CutePet.QuotaProbe --no-build -- --watch --json
dotnet run --project src/CutePet.QuotaProbe --no-build -- --watch --interval 10 --samples 2 --json
```

`--help` 显示参数。`--interval` 接受 10–3600 秒，默认 60；`--timeout` 接受 1–300 秒，默认 30，是单个 RPC 请求的超时。`--samples` 限制 watch 的读取轮数（含失败轮次）。单次 JSON 美化显示，watch JSON 每行一条完整结果。Ctrl+C 取消并清理后台子进程。

退出码：0 正常结束或主动取消；1 连接/读取错误；2 参数错误。watch 遇到服务错误或超时会退避，其他错误退出以便用户处理。不输出原始服务报错、stderr、email 或 account ID。

## 4. 接入链路

进程通过参数列表启动原生 Codex，不经过系统 shell，不拼接用户输入为命令。通道为本地 stdio，无本地 HTTP 监听端口。

1. `initialize` 发送 CutePet 客户端信息。
2. 握手成功后发送 `initialized`。
3. `account/read` 检查认证，使用 `refreshToken: false`。
4. 未登录返回 `NeedsLogin`；API key 和其他认证返回 `UnsupportedAuth`。
5. `account/rateLimits/read` 取得官方额度；不启用实验接口。
6. 解析为公开的脱敏 `QuotaSnapshot`。

实现依据官方 [初始化说明](https://learn.chatgpt.com/docs/app-server#initialization) 和 [账号接口](https://learn.chatgpt.com/docs/app-server#auth-endpoints)，并核对本机生成的 schema。CLI 可用 `codex app-server generate-json-schema --out <目录>` 导出自己的版本定义；不需要提交整套 schema。

`account/updated` 使账号版本变化。初始化期间的通知不直接报错；读取额度期间收到变化时，丢弃该结果并最多重试一次，持续变化返回 `AccountChanged`。

`account/rateLimits/updated` 触发 `QuotaInvalidated` 事件，要求重新读取完整数据，不用局部通知覆盖完整快照。`account/updated` 额外触发 `AccountInvalidated`，让桌面消费者立即清空旧显示；仍触发额度重新读取。事件处理器运行于读取线程，应快速返回，界面消费者应调度到自己的 UI 线程。事件不包含原始账号信息。

## 5. 供后续桌宠使用

引用 `CutePet.Codex` 工程即可，WPF 不需要解析官方 JSON：

```csharp
using CutePet.Codex;

await using var reader = await CodexQuotaReader.ConnectAsync();
var snapshot = await reader.ReadAsync();
// 将 snapshot 交给显示层。
```

`ReadAsync` 使用信号量串行化读取，支持取消。单个 RPC 超时或取消不会让迟到响应成为下一次结果。连接结束会让等待中的请求失败。`DisposeAsync` 只终止此对象自己创建的进程树。

当前模型包括套餐（可空）、官方 `ordinaryUsageAllowed`（可空）、额度桶及 primary/secondary 窗口。每个窗口包含官方使用比例、换算后的剩余比例、窗口分钟数和 UTC 重置时间。

- 优先解析非空 `rateLimitsByLimitId`，否则使用旧版单桶视图。
- 剩余比例为 `clamp(100 - usedPercent, 0, 100)`；缺失值保持 null。
- 窗口名称按实际长度生成，不强制每个账号都有 5 小时/每周额度。
- 不把重置倒计时归零解释为额度已经恢复，不从百分比推断官方使用权限。
- 暂不展示 credits、API 账单或精确单任务消耗。

## 6. 刷新与错误边界

watch 默认 60 秒后重新读取，额度/账号通知可提前唤醒；同一连接至少间隔 10 秒，防止通知风暴。服务错误和超时指数退避至最多 300 秒，并添加少量抖动；失败时通知不绕过退避。

此命令行 PoC 不持久化缓存，失败输出仅说明错误，不伪造为“剩余 0%”。子进程崩溃后当前验证工具退出，下一次运行建立新连接。桌面端现有内存旧数据标记和重连策略见 [窗口说明](desktop-shell.md)；持久缓存、完整账号范围隔离和官方 Retry-After 解析仍待实现。

| 状态 | 行为 |
|---|---|
| MissingDependency | 指引安装兼容 CLI 或指定原生路径 |
| NeedsLogin | 指引用户先使用官方登录 |
| UnsupportedAuth | 说明 API key 账单不是订阅额度 |
| Timeout / ServiceError | 单次读取失败；watch 退避重试 |
| ConnectionClosed | 结束当前连接，需重新启动 |
| IncompatibleProtocol | 检查 CLI 版本和返回结构 |
| AccountChanged | 丢弃不稳定结果，重新连接或读取 |

无法仅凭一个通用 RPC 错误码准确区分断网、服务端限流和凭据过期，因此保留脱敏代码并统一提示检查网络/登录/版本，不宣称已精确识别所有故障。

## 7. 测试与发布目录

```powershell
dotnet build CutePet.sln
dotnet run --project tests/CutePet.Tests --no-build
dotnet publish src/CutePet.QuotaProbe -c Release --self-contained false -o artifacts/quota-probe
```

测试程序退出非零表示失败。它执行 parser 和进程协议测试，不连接真实账号、不发模型请求。CI 另执行不带账号的 WPF 状态验证，不保存个人凭据。

发布目录中的 `CutePet.QuotaProbe.exe` 可从终端运行；可用 `--watch` 持续显示。额度原型 ZIP 另附 `Read-Quota.cmd`（一次读取后保留窗口）和 `Watch-Quota.cmd`（持续读取），可双击启动。仓库中的两个脚本用于放进发布目录，不用于在 scripts 源码目录直接运行。此命令行工具没有桌宠 UI；独立桌面工程见 [窗口说明](desktop-shell.md)。
