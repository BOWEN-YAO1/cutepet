# CutePet

一个计划面向 Windows 的桌面宠物，用角色动画和小面板显示 Codex 剩余额度与重置时间，并在使用后自动同步官方数据。

> 已实现第一阶段的独立额度读取模块和命令行验证原型，桌宠窗口与动画尚未实现。

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

## 计划功能

- 透明桌宠窗口，支持置顶、拖动、缩放和托盘操作。
- 展示账号实际返回的额度窗口、剩余百分比和重置倒计时。
- 自动刷新，清楚标记断网与旧数据。
- 基础动画、点击回应和低额度表情。
- 通过 GitHub Releases 提供 Windows 便携包。

“自动扣除”指同步官方扣除后的结果，不自行估算或修改账号额度。独立读取和本机正常使用后的同步已验证；其他设备的推送、重置和真实账号切换仍待验证。

## 开发文档

详见 [项目开发文档](docs/development.md)，包含模块划分、技术方案、启动流程、接入验证、里程碑及验收要求。

## 计划使用方式

发布后：下载项目发布的 Windows ZIP → 解压 → 双击 `CutePet.exe` → 按指引连接账号 → 日常通过快捷方式启动。

v0.1 计划依赖已安装的兼容 Codex CLI，主程序携带 .NET 运行环境。具体安装步骤与支持版本将在接入验证和发布完成后提供。

## 许可证

仓库现有 [LICENSE](LICENSE) 为 GNU GPL 第 3 版文本。引入第三方依赖和角色素材时，将另行记录来源与许可。
