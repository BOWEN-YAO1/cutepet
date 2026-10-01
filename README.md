# CutePet

一个计划面向 Windows 的桌面宠物，用角色动画和小面板显示 Codex 剩余额度与重置时间，并在使用后自动同步官方数据。

> 已实现独立额度读取模块和 Windows 桌宠窗口：原创占位小猫、紧凑额度条、悬停详情、简单动画、托盘及设置。当前开发原型为 0.3.0，正式角色与完整桌面验收仍待完成。

## 桌宠启动

开发环境需要 Windows 和 .NET 8 SDK，安装兼容的原生 Codex CLI 并使用 ChatGPT 登录。

```powershell
dotnet build CutePet.sln
dotnet run --project src/CutePet.Desktop --no-build
```

便携开发包解压后双击 `CutePet.exe`。当前包依赖已安装的 [.NET 8 Desktop Runtime（Windows x64）](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)，不包含运行环境。桌宠会使用现有官方登录同步额度；找不到 CLI 时可右键选择 `codex.exe`，退出后重新启动生效。

拖动角色或面板移动窗口；右键调整大小、置顶、隐藏和退出；双击托盘图标恢复。默认每 60 秒读取，通知或手动刷新最短间隔 10 秒；不会自动开启系统启动项。

平时显示角色、最多两个额度窗口的剩余比例和同步状态。悬停 350 毫秒展开详情，鼠标移开 650 毫秒后收起；鼠标移入详情可继续查看和刷新。右键或托盘「详情显示」可选择 **悬停显示 / 固定显示 / 隐藏详情**，重启保留选择。详情中也可点击「固定」或「收起」。

完整说明、打包方法与验收边界见 [桌宠窗口说明](docs/desktop-shell.md)。

## 额度读取原型

需要 .NET 8 SDK（开发）和兼容的原生 Codex CLI。本机已验证 `codex-cli 0.159.2`。程序复用官方 CLI 的已有 ChatGPT 登录，只查询账号状态与额度，不运行模型任务，不复制凭据。

```powershell
dotnet build CutePet.sln
dotnet run --project src/CutePet.QuotaProbe --no-build
```

持续观察并输出脱敏 JSON：

```powershell
dotnet run --project src/CutePet.QuotaProbe --no-build -- --watch --json
```

CLI 不在 PATH 时，添加 `--codex "完整路径\codex.exe"`。当前 Windows 原型不支持 `.cmd` / `.ps1` 启动器。未登录时先执行官方 `codex login`；程序不会自动打开登录或注销已有账号。

详见 [额度模块接入说明](docs/quota-reader.md) 和 [接入验证记录](docs/codex-integration-validation.md)。验证记录区分真实账号测试、模拟协议测试和待验证项。

## 当前与后续功能

- 已实现：透明窗口、置顶、拖动、大小调整、托盘和设置保存。
- 已实现：官方剩余百分比、重置倒计时、定时刷新和旧数据标记。
- 已实现：默认紧凑额度条、悬停详情、固定 / 隐藏开关及设置保存。
- 已实现：占位小猫的眨眼、轻微上下移动、点击回应及低额度气泡。
- 待完成：正式角色、更多表情、人工桌面验收与 GitHub Releases 发布。

“自动扣除”指同步官方扣除后的结果，不自行估算或修改账号额度。独立读取和本机正常使用后的同步已验证；其他设备的推送、重置和真实账号切换仍待验证。

## 开发文档

详见 [项目开发文档](docs/development.md)，包含模块划分、技术方案、启动流程、接入验证、里程碑及验收要求。

## 计划使用方式

发布后：下载项目发布的 Windows ZIP → 解压 → 双击 `CutePet.exe` → 按指引连接账号 → 日常通过快捷方式启动。

正式版计划携带 .NET 运行环境；当前开发包需要 .NET 8 Desktop Runtime。已验证本机原生 CLI 0.159.2，其他安装方式和版本见验证记录。

## 许可证

仓库现有 [LICENSE](LICENSE) 为 GNU GPL 第 3 版文本。引入第三方依赖和角色素材时，将另行记录来源与许可。
