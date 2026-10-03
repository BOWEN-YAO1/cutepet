# 角色素材与接入（0.9.1）

角色选择：右键或托盘 → 角色选择 → 小猫 / 洛天依（同人）。选择立即生效，保存在个人 settings.json 中。旧配置继续采用小猫。

小猫的原矢量外观已导出为透明 PNG 帧，保留眨眼、点击轻摇和低额度眯眼；与洛天依、自定义角色共用角色包播放器。洛天依保留原四张透明 PNG，现有左右看、开心、抬手中间姿势和向外摆手五张新增图，支持眨眼、多姿态挥手、待机张望、随机点击回应与疲惫表情。隐藏与换角色清除临时动作。两种角色共用额度条、拖动、独立大小和详情模式。

## 素材来源

- 文件：src/CutePet.Desktop/Characters/Packs/tianyi/idle.png。
- 日期：2026-10-01。
- 工具：内置 image_gen.imagegen，默认工具模式，transparent_background=true。未使用 CLI / API key 回退。
- 人物设计参考：洛天依灰发、绿瞳及辫发形象，参考 [官方 VOCALOID 产品介绍](https://www.vocaloid.com/products/show/v5l_tianyi)。生成时没有下载或传入官方立绘。
- 本图为非官方 AI 同人图，角色权利说明见 [THIRD_PARTY_NOTICES.md](../THIRD_PARTY_NOTICES.md)。

## 实际生成提示词

```text
Use case: stylized-concept. Asset type: transparent PNG character sprite for a tiny Windows desktop pet. Primary request: a cute chibi fan-art interpretation of Luo Tianyi (洛天依), recognizable gray/silver hair, jade green eyes, looped braided hair buns and short side braids, jade hair ornaments, classic blue-and-white Chinese-inspired modest short dress with blue decorative trim, dark boots. Full-body front view, warm gentle smile, relaxed standing pose, hands held softly beside body. Super-deformed 2.5-head proportions, large expressive head, clean anime line art with restrained cel shading, readable silhouette at 150px tall. Single character only, centered, entire hair and shoes visible, very little transparent padding, no scenery, no ground shadow, no frame, no text, no logos, no watermark. Genuine transparent alpha background. This is an unofficial fan-art sprite, not a UI mockup. No extra characters or props.
```

## 接入方式

角色资源分别位于 `src/CutePet.Desktop/Characters/Packs/cat/` 和 `Characters/Packs/tianyi/`，每包包含 character.json、PNG 帧及素材权利说明。CharacterPackLoader 校验并解码素材，CharacterLibrary 管理内置和本地包，CharacterAnimation 共享触发规则并按包内帧时间播放。Preferences 使用稳定 CharacterPackId，旧 Character 枚举只用于迁移。

Views/Controllers/CharacterPresenter 的 40 毫秒计时器驱动图片帧，WPF 变换提供轻微浮动与点击轻摇；隐藏时停止并复位。低额度表情仅由新鲜且有效的数据驱动。两处菜单都从角色库生成，可从管理窗口导入 PNG / ZIP、预览、导出和移除角色。格式与边界见 [角色包说明](character-packs.md)。0.9.0 保留六种动作、九张透明图片，仍使用逐帧播放器；没有骨骼动画。新规则见仓库 docs/tianyi-animation.md。

## 验证

0.9.1 的 190 项回归检查中含全部角色默认值、缓存、透明背景与尺寸一致性检查，以及眨眼结束、挥手多姿态切换、新动作优先级、悬停与随机张望触发、结束回到低额度状态、旧数据不驱动疲惫表情、隐藏和换角色复位。离屏渲染含闭眼、挥手和疲惫图；预览为模拟额度。状态检查推进与实际计时器共用函数，并运行真实 WPF 帧计时器验证动作结束与隐藏后停止；不移动真实鼠标，不声称实际桌面动画观感已验收。

## 2026-10-02 动画帧编辑记录

使用内置 image_gen.imagegen 的编辑模式，referenced_image_paths 指向本项目的原始同人图，transparent_background=true。未使用 CLI / API key 回退，原始图未覆盖。原文件在 0.8.0 改名，0.8.1 移入 src/CutePet.Desktop/Characters/Packs/tianyi/，依次命名 blink.png、greeting.png、low.png；与 idle.png 同画布大小。下列名称为生成时的原始名称。

### 闭眼帧

文件：tianyi-blink-v1.png

```text
Use case: identity-preserve. Edit target: the supplied transparent Luo Tianyi fan-art desktop-pet sprite. Create exactly one blink animation frame. Change ONLY the two eyes to gently closed curved eyelids. Preserve the exact full-body pose, mouth, face shape, hair, ornaments, costume, hands, shoes, line art, colors, character proportions, scale, alignment, canvas dimensions and transparent padding from the input. Same head location and same feet baseline for animation registration. Do not redraw the whole character or add effects. Genuine transparent alpha background, no text or extra objects.
```

### 挥手帧

文件：tianyi-wave-v1.png

```text
Use case: identity-preserve. Edit target: supplied transparent Luo Tianyi fan-art desktop pet. Create one friendly greeting animation frame. Change ONLY her right arm (on the viewer's left) to raise her open hand beside her shoulder in a small wave, and make her smile a little brighter. Keep both eyes open. Preserve exact character identity, hair, clothes, other arm, body pose, head position, feet baseline, full-body proportions, pixel scale, canvas dimensions, alignment and transparent padding. Keep the raised hand inside the current silhouette width. Clean anime line art, same colors. Genuine transparent alpha background. No effects, text, icons, shadow or props. This frame will alternate with the original for a short wave animation.
```

### 低额度帧

文件：tianyi-low-v1.png

```text
Use case: identity-preserve. Edit target: supplied transparent Luo Tianyi fan-art desktop-pet sprite. Create one low-quota tired expression frame. Change ONLY her facial expression: green eyes gently half-lidded, slightly concerned small mouth, mild tired look but still cute and friendly. Preserve exact full-body standing pose, both arms and hands, face geometry, hair, ornaments, costume, boots, line art, colors, proportions, head location, feet baseline, canvas dimensions, pixel scale and transparent padding from the input. Do not add sweat drops, symbols, effects, props, ground shadows or text. Genuine transparent alpha background. This frame must align with the original for a desktop animation.
```

## 0.9.0 新增六张动作帧

使用内置 image_gen.imagegen 编辑项目原始同人图，transparent_background=true；保留原四张图。完整生成和画布修正提示词见 [天依包 PROMPTS.md](../src/CutePet.Desktop/Characters/Packs/tianyi/PROMPTS.md)，同样嵌入包内 SOURCE.md 并随角色包导出。所有最终帧均为 1024×1535。触发与人工验收见 [动作增强说明](tianyi-animation.md)。

## 0.9.1 撤掉歪头动作

根据使用反馈，从天依配置中移除 hover 动作，删除 tilt-v2.png。当前素材九张、动作六种；悬停仍能展开额度详情，角色保持站姿。公共自定义角色 hover 格式继续支持。下方或 PROMPTS.md 中的歪头提示词为历史记录，原图可在 0.9.0 提交 9b0c448 的源码中追溯。

## 0.10.0 王座休息素材

新增施法、王座显现、完整王座、下坐过渡与坐姿五张透明图，使用内置 imagegen 编辑，保留服装、鞋子及原有动作。完整提示词见 [THRONE-PROMPTS.md](../src/CutePet.Desktop/Characters/Packs/tianyi/THRONE-PROMPTS.md)，也嵌入包内 SOURCE.md 随导出分发。当前共 14 张图、9 种动作。流程与验收见 [王座休息说明](throne-rest.md)。

## 0.11.0 乘云飘动

2026-10-02：新增天依独立云层（cloud-v1.png）、施法召唤和短距离横向飘动、自动开关与交互暂停。角色素材保留原有脸、服装和动作；小猫保留原行为。云由内置 image_gen.imagegen 生成，1774×887 原始透明 PNG，完整提示词位于角色包 CLOUD-PROMPTS.md 及 SOURCE.md。运动与验证边界详见 docs/cloud-drift.md；属于非官方同人设计，原角色权利说明继续适用。

## 0.12.0 王座动作衔接

2026-10-02：新增 sit-prepare-v2.png、sit-lower-v2.png、sit-near-seat-v2.png 三张过渡帧，召唤 / 坐下 8 帧、起身 9 帧，按当前帧进度反向起身。格式继续为 1，像素限额调整到 33,554,432；旧包保留兼容。新图由内置 image_gen.imagegen 编辑，原图保留，完整提示词位于 REST-SMOOTH-PROMPTS.md 与 SOURCE.md。动作及验证边界详见 docs/rest-smooth.md，原角色权利说明继续适用。


## 0.13.0 坐姿互动

2026-10-02：新增 sit-blink-v1.png、sit-happy-v1.png、sit-wave-v1.png 三张坐姿动作图，来自内置 image_gen.imagegen 对 sit-v1.png 的编辑，保留原图。公共播放器区分坐姿和站姿回应；CharacterPresenter 保留自动休息并触发坐姿微笑。天依共 13 种动作、20 张人物图和 1 张云层，像素限额保持不变。完整提示词在 SEATED-PROMPTS.md / SOURCE.md，原角色权利说明继续适用。行为和 348 项窗口 / 功能验证见 docs/seated-actions.md。


## 0.16.0 屏幕边缘互动

天依支持拖至左右屏幕边缘贴边、点击 / 悬停探头，额度保持独立固定。新增角色包动作 edge-idle / edge-peek、可选 edgeAnchorX 和持久化开关 EdgeInteraction。Layout/ScreenEdgeLayout.cs 计算吸附位置，Views/Controllers/EdgeMotionController.cs 控制贴边与轻微探出，CharacterAnimation / CharacterPresenter 接入角色包动作与生命周期。旧角色包兼容。范围、素材、内存限额、退出规则和验收见 [屏幕边缘互动](screen-edge.md)。

## 0.17.0 上下边缘与任务栏

新增天依上沿抓边、下沿托腮及各自微笑图，人物支持四向贴边，额度保持固定。任务栏正常显示时贴在工作区底边；未预留任务栏高度时额外留 12 个逻辑像素底部空间。工作区 / DPI 改变后保持方向重新对齐，区域过小时安全退出。新增 edge-top-idle / edge-top-peek / edge-bottom-idle / edge-bottom-peek 与两个纵向接触线字段，旧角色包兼容。594 项窗口 / 功能检查、14 项额度协议测试通过；真实鼠标、多屏和任务栏显隐仍需试用。完整行为、限制、素材和像素上限见 [屏幕边缘互动](screen-edge.md)。


## 0.18.0 上侧探头重绘

上侧改为从屏幕外俯身探头，只露头、短发和两只自然搭在上沿的小手，移除上侧回应时的拉伸。采用版本化 `edge-top-v2.png` / `edge-top-smile-v2.png`，上侧接触线更新为 0.12；下侧托腮和左右动作沿用。当前引用图片数量与像素预算不变，旧上侧图保留在源码，导出角色包只打包引用图。提示词见天依包 `TOP-EDGE-PROMPTS.md`，完整来源记录仍写入 `SOURCE.md`。594 项窗口 / 功能检查及 14 项额度协议测试通过；实际鼠标与多屏体验仍需试用。详见 [屏幕边缘互动](screen-edge.md)。


## 0.19.0 屏幕上沿秋千

天依上側改为坐在悬挂秋千上，约 3.2 秒轻摆一周期，点击或悬停短暂增加摆幅；绳索顶端固定、底端跟随坐板，额度保持独立固定。采用 `swing-v1.png` 与可选 `topSwing` 配置，旧上侧素材保留在源码，旧角色包不配置时保持原有姿势。当前引用 25 张人物图及 1 张云层，像素预算不变。623 项窗口 / 功能检查、14 项额度协议测试通过。来源和提示词见天依包 `SOURCE.md` / `SWING-PROMPTS.md`，参数与限制见 [屏幕边缘互动](screen-edge.md)。
