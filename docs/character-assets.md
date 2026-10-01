# 角色素材与接入（0.7.0）

角色选择：右键或托盘 → 角色选择 → 小猫 / 洛天依（同人）。选择立即生效，保存在个人 settings.json 中。旧配置继续采用小猫。

小猫保留矢量绘制与眨眼，新增点击轻摇和低额度眯眼。洛天依使用待机、闭眼、抬手、低额度四张透明 PNG，支持浮动、160 毫秒眨眼、约 1.08 秒的两姿态挥手和疲惫表情。隐藏与换角色清除临时动作。两种角色共用额度条、拖动、独立大小和详情模式。

## 素材来源

- 文件：src/CutePet.Desktop/Assets/tianyi-fanart-v1.png。
- 日期：2026-10-01。
- 工具：内置 image_gen.imagegen，默认工具模式，transparent_background=true。未使用 CLI / API key 回退。
- 人物设计参考：洛天依灰发、绿瞳及辫发形象，参考 [官方 VOCALOID 产品介绍](https://www.vocaloid.com/products/show/v5l_tianyi)。生成时没有下载或传入官方立绘。
- 本图为非官方 AI 同人图，角色权利说明见 [THIRD_PARTY_NOTICES.md](../THIRD_PARTY_NOTICES.md)。

## 实际生成提示词

```text
Use case: stylized-concept. Asset type: transparent PNG character sprite for a tiny Windows desktop pet. Primary request: a cute chibi fan-art interpretation of Luo Tianyi (洛天依), recognizable gray/silver hair, jade green eyes, looped braided hair buns and short side braids, jade hair ornaments, classic blue-and-white Chinese-inspired modest short dress with blue decorative trim, dark boots. Full-body front view, warm gentle smile, relaxed standing pose, hands held softly beside body. Super-deformed 2.5-head proportions, large expressive head, clean anime line art with restrained cel shading, readable silhouette at 150px tall. Single character only, centered, entire hair and shoes visible, very little transparent padding, no scenery, no ground shadow, no frame, no text, no logos, no watermark. Genuine transparent alpha background. This is an unofficial fan-art sprite, not a UI mockup. No extra characters or props.
```

## 接入方式

CharacterCatalog 定义稳定角色枚举、菜单名称与缓存图片，Preferences 保存并校验 Character 值。PNG 以 WPF Resource 嵌入程序，用 pack URI 按需加载，解码后冻结并复用。CharacterAnimation 使用经过时间推进眨眼与挥手，当前交互覆盖旧交互。MainWindow 的 80 毫秒计时器驱动帧切换，WPF 变换驱动浮动与轻摇；隐藏时停止并复位。低额度表情仅由新鲜且有效的数据驱动。两个菜单都从同一角色枚举生成选项。

新增图片角色可扩展枚举和目录，加入嵌入资源与显示分支；当前没有外部素材导入、热加载或骨骼播放器。本版挥手只有待机与抬手两个姿态，并非完整连续序列。

## 验证

0.7.0 的 115 项检查中含全部角色默认值、缓存、透明背景与尺寸一致性检查，以及眨眼结束、挥手交替、结束回到低额度状态、旧数据不驱动疲惫表情、隐藏和换角色复位。离屏渲染含闭眼、挥手和疲惫图；预览为模拟额度。状态检查推进与实际计时器共用函数，并运行真实 WPF 帧计时器验证动作结束与隐藏后停止；不移动真实鼠标，不声称实际桌面动画观感已验收。

## 2026-10-02 动画帧编辑记录

使用内置 image_gen.imagegen 的编辑模式，referenced_image_paths 指向本项目的原始同人图，transparent_background=true。未使用 CLI / API key 回退，原始图未覆盖。三个文件保存于 src/CutePet.Desktop/Assets/，与原图同画布大小。

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
