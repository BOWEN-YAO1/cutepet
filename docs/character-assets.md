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
