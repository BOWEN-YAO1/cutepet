# 项目结构与修改入口（0.13.0）

日期：2026-10-02。0.8.1 整理目录，并将主窗口中的交互状态和播放计时分配给独立模块。保留 0.8 的角色包格式、设置目录、程序入口与已实现功能。

## 仓库顶层

```text
cutepet/
├── src/                       产品源码
│   ├── CutePet.Core/          额度数据模型与解析
│   ├── CutePet.Codex/         官方 CLI 连接与读取
│   ├── CutePet.QuotaProbe/    独立读取验证工具
│   └── CutePet.Desktop/       Windows 桌宠
├── examples/                  可以复制修改的使用示例
│   ├── README.md              示例用途与入口
│   └── character-pack/        完整的自定义小猫示例
├── docs/                      开发、使用与素材说明
├── tests/CutePet.Tests/       额度协议测试
├── scripts/                   额度工具的启动脚本
└── .github/workflows/         自动构建和验证
```

examples 不参与程序编译。示例包含素材，是为了让用户能直接压缩并导入；内置资源由主程序嵌入，个人角色安装在用户数据目录。三者各自有明确用途。

## 桌宠目录

```text
CutePet.Desktop/
├── App.xaml / App.xaml.cs               程序入口、公共界面样式与退出流程
├── CutePet.Desktop.csproj               编译目标与资源配置
├── app.manifest                         Windows 应用声明
├── Views/
│   ├── MainWindow.xaml / .xaml.cs       桌宠窗口、设置入口及模块协调
│   ├── CharacterManagerWindow.*         角色管理窗口
│   └── Controllers/
│       ├── PetDragController.cs        鼠标手势、拖动状态与停靠预览
│       ├── DetailsController.cs        悬停计时、菜单保持与详情弹窗
│       ├── CharacterPresenter.cs       图片显示、动画计时与 WPF 变换
│       ├── CloudMotionController.cs    乘云行程、界面暂停与位置保存
│       └── DesktopMenuBuilder.cs       桌宠右键菜单组成与选中状态
├── Quota/
│   ├── QuotaSession.cs                 读取刷新、通知、重试及生命周期
│   └── PetViewModel.cs                 额度与提示的界面显示状态
├── Characters/
│   ├── CharacterCatalog.cs            内置包目录和旧角色编号兼容
│   ├── CharacterLibrary.cs            本地库、导入、导出、归档与坏包回退
│   ├── CharacterPack.cs               包的配置、动作与已加载帧模型
│   ├── CharacterPackLoader.cs         配置和图片校验、解码与缓存
│   ├── CharacterAnimation.cs          按包内时长推进的共享播放器
│   ├── CloudFlight.cs                 乘云阶段与屏幕边界目标计算
│   └── Packs/
│       ├── cat/                       内置小猫包
│       └── tianyi/                    内置洛天依同人包
├── Settings/
│   ├── Preferences.cs                 偏好数据、默认值和校验
│   └── PreferencesStore.cs            用户设置的读取与保存
├── Layout/DockLayout.cs               大小、停靠方向和拖动目标的几何计算
├── Platform/
│   ├── TrayController.cs              Windows 托盘与托盘菜单
│   ├── StartupRegistration.cs         当前用户开机启动项及验证存储
│   ├── SingleInstance.cs              防止重复启动、唤回已有实例
│   └── NativePlacement.cs             屏幕、DPI、位置约束和弹窗置顶
└── Verification/
    ├── DesktopVerification.cs         真实 WPF 离屏功能验证
    ├── ThroneMotionVerification.cs     王座衔接、反向播放及动画预览
    ├── CloudVerification.cs           乘云时钟、边界和交互验证
    └── CharacterPackVerification.cs   角色包端到端及错误边界验证
```

Views/Controllers 是与 WPF 控件关联的交互模块；角色播放器和布局计算仍各自独立。MainWindow 不再拥有拖动、悬停和动画的计时状态，通过事件适配和少量方法连接这些模块。它仍负责窗口生命周期、设置更新和额度连接，没有把全部协调逻辑拆成额外抽象层。

本次保留现有 `CutePet.Desktop` 命名空间和对外类型，目录按职责分类；移动文件不会改变个人配置格式或启动参数。编译资源路径调整为 Characters/Packs，加载与导出使用同一目录。App 通过代码创建窗口，未新增 StartupUri。

## 修改功能时从哪里开始

| 修改内容 | 主要入口 |
|---|---|
| 桌宠外观、额度条、详情布局 | Views/MainWindow.xaml |
| 角色管理页面 | Views/CharacterManagerWindow.xaml 与对应代码 |
| 拖动与停靠手势 | Views/Controllers/PetDragController.cs |
| 悬停展开与收起规则 | Views/Controllers/DetailsController.cs |
| 动画在桌面上的表现 | Views/Controllers/CharacterPresenter.cs |
| 帧顺序、动作优先级与结束规则 | Characters/CharacterAnimation.cs |
| 增加内置角色或动作素材 | Characters/Packs；新内置角色还需登记到 CharacterCatalog |
| 添加个人图片角色 | 使用程序中的导入入口，无需修改主程序 |
| 包格式与导入规则 | Characters/CharacterPackLoader.cs、CharacterLibrary.cs |
| 默认偏好与持久保存 | Settings/ |
| 额度读取协议 | CutePet.Codex 与 CutePet.Core |
| 平台托盘和启动项 | Platform/ |

## 整理后的验证

0.8.1 整理版本的 154 项 WPF / 功能检查与 14 项协议测试通过。对比 0.8 的 36 张离屏状态截图，所有 PNG 的 SHA-256 一致，覆盖角色、动画姿态、管理预览、不同大小、停靠方向和额度显示；对比使用模拟数据。

自动检查不会展示普通桌宠窗口或修改个人角色、额度缓存、启动项。真实鼠标、文件选择对话框、实际登录启动和多显示器体验仍按 [桌宠验收说明](desktop-shell.md) 进行。

## 0.9.0 动作扩展

目录结构继续沿用 0.8.1。天依新素材和帧时序位于 Characters/Packs/tianyi，动作优先级在 CharacterAnimation，悬停和随机触发在 CharacterPresenter，管理窗口支持动作选择预览。新增规则见 [动作增强说明](tianyi-animation.md)。当前 190 项 WPF / 功能检查和 14 项协议测试通过；0.8.1 的截图一致性结论仅适用于此前结构重构。

0.9.1 撤掉天依自己的 hover 动作和图片，公共播放器与自定义角色格式保持兼容。走动、坐下和边缘探头目前只是后续方案，尚未实现。

## 0.10.0 王座休息

新增五张透明素材与 conjure / sit / stand，天依共 14 张图、9 种动作。公共播放器处理坐姿基础状态、起身后的最近一次回应和清理；CharacterPresenter 处理 30 秒自动坐下、20 秒自动起身及浮动暂停。菜单支持手动休息和自动开关，开关写入 Preferences.AutoRest。旧包继续兼容。当前完整验证见 [王座休息说明](throne-rest.md)，此前版本的动作数与验证数为历史记录。

## 0.12.0 王座动作衔接

2026-10-02：新增 sit-prepare-v2.png、sit-lower-v2.png、sit-near-seat-v2.png 三张过渡帧，召唤 / 坐下 8 帧、起身 9 帧，按当前帧进度反向起身。格式继续为 1，像素限额调整到 33,554,432；旧包保留兼容。新图由内置 image_gen.imagegen 编辑，原图保留，完整提示词位于 REST-SMOOTH-PROMPTS.md 与 SOURCE.md。动作及验证边界详见 docs/rest-smooth.md，原角色权利说明继续适用。


## 0.13.0 坐姿互动

2026-10-02：新增 sit-blink-v1.png、sit-happy-v1.png、sit-wave-v1.png 三张坐姿动作图，来自内置 image_gen.imagegen 对 sit-v1.png 的编辑，保留原图。公共播放器区分坐姿和站姿回应；CharacterPresenter 保留自动休息并触发坐姿微笑。天依共 13 种动作、20 张人物图和 1 张云层，像素限额保持不变。完整提示词在 SEATED-PROMPTS.md / SOURCE.md，原角色权利说明继续适用。行为和 348 项窗口 / 功能验证见 docs/seated-actions.md。


## 0.14.0 人物与额度独立窗口

人物与额度改为两个透明小窗口，共享一个额度会话，独立拖动、缩放和保存位置。乘云只移动人物；详情锚定额度，四方向菜单只做一次性相对摆放。旧组合窗口坐标按原几何迁移一次。角色包格式和全部素材保留。当前仍为短距离乘云，尚未扩展为桌面巡游；行为、376 项窗口 / 功能验证与人工验收边界见 [独立窗口说明](independent-windows.md)。
