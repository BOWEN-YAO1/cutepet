# 发布包目录与打包（0.46.0）

```text
CutePet-Desktop-v0.46.0-win-x64/
├── CutePet.exe
├── CutePet.dll
├── CutePet.Core.dll
├── CutePet.Codex.dll
├── CutePet.deps.json
├── CutePet.runtimeconfig.json
├── 使用说明.md
├── characters/          天依、小猫可导入角色包和示例
├── previews/
│   ├── interface/       四页界面、交流设置截图
│   ├── animations/      站立、王座、坐姿、四边、收云 GIF、上侧及侧边原绘图 GIF 与姿势对照图
│   └── dialogue/        台词气泡预览
├── docs/                功能、格式与开发文档
├── licenses/            LICENSE 与 THIRD_PARTY_NOTICES.md
└── source/              对应源码 ZIP 与 SOURCE.txt
```

主目录只放启动程序、运行文件和使用说明。当前是框架依赖的 Windows x64 便携版，DLL 与两份 .NET 配置需保持在 EXE 旁边；图片和图标已嵌入程序，预览仅供查看。不要只复制 EXE 或把 DLL 移进其他文件夹。无需安装，完整解压后双击 EXE；依赖 .NET 8 Desktop Runtime。

台词角色包只包含实际动作引用的 PNG、manifest 与来源 / 许可说明。早期姿势图、生成草稿和历史来源档案保留在匹配源码 ZIP 中。旧导入角色不会被覆盖，需要手动导入新角色包或选择更新后的内置角色。

## 维护者打包

先提交发布修改，再构建、验证和发布，使程序与 `git archive HEAD` 一致。使用 Python 3 标准库打包，不需要 Pillow 或其他依赖：

```powershell
dotnet build CutePet.sln -c Release
dotnet run --project tests/CutePet.Tests -c Release --no-build
dotnet src/CutePet.Desktop/bin/Release/net8.0-windows/CutePet.dll --verify artifacts/desktop-verification
dotnet publish src/CutePet.Desktop -c Release --no-build -o artifacts/publish
python scripts/package-desktop.py --publish-dir artifacts/publish --verification-dir artifacts/desktop-verification --output-dir artifacts/packages
$taskCommit = git rev-parse HEAD
python scripts/verify-package.py artifacts/packages/CutePet-Desktop-v0.46.0-win-x64.zip --expected-commit $taskCommit
```

任一步失败则停止发布。打包器拒绝未提交修改或未通过的窗口验证报告，从项目版本生成文件名，创建全新的临时目录，按分类复制文档和新预览，并附对应提交的 GPL 源码和 SHA-256。打包器本身不证明发布目录与提交一致，维护者应按上述顺序执行，不复用旧版 publish 目录。

校验脚本检查 ZIP 完整性、安全唯一路径、根目录精简、五类目录、许可与源码提交 / 哈希、角色台词和所有实际引用图与附带源码一致，以及连续姿势、王座、坐姿、三组边缘动作、柔和收云和交流预览。GitHub Actions 也执行分组打包和 ZIP 校验。维护者还需完整解压包，在隔离目录运行 `CutePet.exe --verify <新验证目录>`，检查实际分发 EXE；不替代多屏、任务栏、真实拖动和长期使用验收。当前不自动发布 GitHub Release 或安装器。
