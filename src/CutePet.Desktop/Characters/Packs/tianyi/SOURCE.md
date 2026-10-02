# 角色素材与接入（0.9.1）

角色选择：右键或托盘 → 角色选择 → 小猫 / 洛天依（同人）。选择立即生效，保存在个人 settings.json 中。旧配置继续采用小猫。

小猫的原矢量外观已导出为透明 PNG 帧，保留眨眼、点击轻摇和低额度眯眼；与洛天依、自定义角色共用角色包播放器。洛天依保留原四张透明 PNG，现有左右看、开心、抬手中间姿势和向外摆手五张新增图，支持眨眼、多姿态挥手、待机张望、随机点击回应与疲惫表情。隐藏与换角色清除临时动作。两种角色共用额度条、拖动、独立大小和详情模式。

## 素材来源

- 文件：src/CutePet.Desktop/Characters/Packs/tianyi/idle.png。
- 日期：2026-10-01。
- 工具：内置 image_gen.imagegen，默认工具模式，transparent_background=true。未使用 CLI / API key 回退。
- 人物设计参考：洛天依灰发、绿瞳及辫发形象，参考 [官方 VOCALOID 产品介绍](https://www.vocaloid.com/products/show/v5l_tianyi)。生成时没有下载或传入官方立绘。
- 本图为非官方 AI 同人图，角色权利说明见 仓库根目录 THIRD_PARTY_NOTICES.md。

## 实际生成提示词

```text
Use case: stylized-concept. Asset type: transparent PNG character sprite for a tiny Windows desktop pet. Primary request: a cute chibi fan-art interpretation of Luo Tianyi (洛天依), recognizable gray/silver hair, jade green eyes, looped braided hair buns and short side braids, jade hair ornaments, classic blue-and-white Chinese-inspired modest short dress with blue decorative trim, dark boots. Full-body front view, warm gentle smile, relaxed standing pose, hands held softly beside body. Super-deformed 2.5-head proportions, large expressive head, clean anime line art with restrained cel shading, readable silhouette at 150px tall. Single character only, centered, entire hair and shoes visible, very little transparent padding, no scenery, no ground shadow, no frame, no text, no logos, no watermark. Genuine transparent alpha background. This is an unofficial fan-art sprite, not a UI mockup. No extra characters or props.
```

## 接入方式

角色资源分别位于 `src/CutePet.Desktop/Characters/Packs/cat/` 和 `Characters/Packs/tianyi/`，每包包含 character.json、PNG 帧及素材权利说明。CharacterPackLoader 校验并解码素材，CharacterLibrary 管理内置和本地包，CharacterAnimation 共享触发规则并按包内帧时间播放。Preferences 使用稳定 CharacterPackId，旧 Character 枚举只用于迁移。

Views/Controllers/CharacterPresenter 的 40 毫秒计时器驱动图片帧，WPF 变换提供轻微浮动与点击轻摇；隐藏时停止并复位。低额度表情仅由新鲜且有效的数据驱动。两处菜单都从角色库生成，可从管理窗口导入 PNG / ZIP、预览、导出和移除角色。格式与边界见 仓库 docs/character-packs.md。0.9.0 保留六种动作、九张透明图片，仍使用逐帧播放器；没有骨骼动画。新规则见仓库 docs/tianyi-animation.md。

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


# 天依新增动作素材（0.9.0）

## 歪头帧画布修正

首次歪头输出为 1025×1535，经内置编辑工具修正为 1024×1535，未用脚本裁剪图片。

```text
Use case: precise-object-edit. Image 1 is the edit target, a transparent Luo Tianyi desktop pet head-tilt frame. Return a single PNG with precisely 1024 columns and 1535 rows, remove only the rightmost transparent column from the 1025x1535 input canvas. Keep all character pixels, positions, feet baseline, costume, expression and head tilt exactly unchanged. Genuine transparent background and alpha, no new background, shadow or glow. Do not redraw, reposition, rescale or add anything.
```


2026-10-02；使用内置 image_gen.imagegen 编辑模式，transparent_background=true。输入为项目原始同人 idle.png / greeting.png；未使用 CLI / API key。以下为最终选用图片的完整提示词。保留原始四张图，新增六张，人物身份与原角色权利说明见 LICENSE.txt 和仓库 THIRD_PARTY_NOTICES.md。

## look-left-v2.png

输入：idle.png

```text
Use case: identity-preserve. Edit target: Image 1, the supplied transparent full-body Luo Tianyi fan-art desktop-pet sprite. Produce exactly ONE animation frame, not a sheet. Keep the original canvas exactly 1024x1535 pixels (identical to the input; do not add a bottom row), original character size, feet baseline, body and costume placement, silver-gray braided hair, green eyes, all ornaments, colors, and detailed anime line art. Preserve everything except the specified facial/pose change. The character must remain registered to the original image so swapping frames does not jump. Genuine transparent alpha background including every hair-loop opening, no checkerboard baked into pixels, no scenery, shadow, text, watermark, or extra objects. Change ONLY the direction of both pupils: a clearly visible curious glance toward the viewer's LEFT. Keep both eyes fully open and keep the entire head, mouth, hair and body in exactly the original locations.
```

## look-right-v2.png

输入：idle.png

```text
Use case: identity-preserve. Edit target: Image 1, the supplied transparent full-body Luo Tianyi fan-art desktop-pet sprite. Produce exactly ONE animation frame, not a sheet. Keep the original canvas exactly 1024x1535 pixels (identical to the input; do not add a bottom row), original character size, feet baseline, body and costume placement, silver-gray braided hair, green eyes, all ornaments, colors, and detailed anime line art. Preserve everything except the specified facial/pose change. The character must remain registered to the original image so swapping frames does not jump. Genuine transparent alpha background including every hair-loop opening, no checkerboard baked into pixels, no scenery, shadow, text, watermark, or extra objects. Change ONLY the direction of both pupils: a clearly visible curious glance toward the viewer's RIGHT. Keep both eyes fully open and keep the entire head, mouth, hair and body in exactly the original locations.
```

## tilt-v2.png

输入：idle.png

```text
Use case: identity-preserve. Edit target: Image 1, the supplied transparent full-body Luo Tianyi fan-art desktop-pet sprite. Produce exactly ONE animation frame, not a sheet. Keep the original canvas exactly 1024x1535 pixels (identical to the input; do not add a bottom row), original character size, feet baseline, body and costume placement, silver-gray braided hair, green eyes, all ornaments, colors, and detailed anime line art. Preserve everything except the specified facial/pose change. The character must remain registered to the original image so swapping frames does not jump. Genuine transparent alpha background including every hair-loop opening, no checkerboard baked into pixels, no scenery, shadow, text, watermark, or extra objects. Gently tilt ONLY the head and attached upper hair about 6 degrees toward the viewer's RIGHT, pivoting at the neck. Curious gentle expression with both green eyes open. Body, hands, skirt, lower braids and boots must remain in their exact original locations. Keep all hair and ornaments inside the same canvas and very close to the original silhouette.
```

## happy-v2.png

输入：idle.png

```text
Use case: identity-preserve. Edit target: Image 1, the supplied transparent full-body Luo Tianyi fan-art desktop-pet sprite. Produce exactly ONE animation frame, not a sheet. Keep the original canvas exactly 1024x1535 pixels (identical to the input; do not add a bottom row), original character size, feet baseline, body and costume placement, silver-gray braided hair, green eyes, all ornaments, colors, and detailed anime line art. Preserve everything except the specified facial/pose change. The character must remain registered to the original image so swapping frames does not jump. Genuine transparent alpha background including every hair-loop opening, no checkerboard baked into pixels, no scenery, shadow, text, watermark, or extra objects. Change ONLY facial expression to a happy gentle smile: both eyes closed into soft upward smiling curves, cheeks slightly rosy, a small cheerful open smile. Head and every other body part must stay in the original pose and exact positions.
```

## greeting-mid-v2.png

输入：greeting.png

```text
Use case: identity-preserve. Image 1 is the edit target: transparent Luo Tianyi desktop pet greeting frame. Produce exactly ONE registered animation frame. Change ONLY the raised right arm on viewer LEFT to a midway greeting pose: lower the open waving hand to chest height, elbow bent. Preserve every other pixel position as closely as possible: exact head/hair/face/body/costume/other arm/boots, same character scale, canvas and feet baseline. Keep style, line art and colors. Full body, entire hair and boots. Genuine transparent alpha background including hair loops. No scenery, glow, shadow, effects, text, watermark or extra object. Keep input canvas 1024 by 1535 if supported, otherwise 1024 by 1536 without shifting character.
```

## greeting-out-v2.png

输入：greeting.png

```text
Use case: identity-preserve. Image 1 is the edit target: transparent Luo Tianyi desktop pet greeting frame. Produce exactly ONE registered animation frame. Change ONLY the raised right hand on viewer LEFT: tilt the open hand at its wrist outward by 15 degrees, fingertips toward viewer LEFT, small movement in a friendly wave. Keep elbow and sleeve where they are. Preserve every other pixel position as closely as possible: exact head/hair/face/body/costume/other arm/boots, same character scale, canvas and feet baseline. Keep style, line art and colors. Full body, entire hair and boots. Genuine transparent alpha background including hair loops. No scenery, glow, shadow, effects, text, watermark or extra object. Keep input canvas 1024 by 1535 if supported, otherwise 1024 by 1536 without shifting character.
```

## 0.9.1 撤掉歪头动作

根据使用反馈，从天依配置中移除 hover 动作，删除 tilt-v2.png。当前素材九张、动作六种；悬停仍能展开额度详情，角色保持站姿。公共自定义角色 hover 格式继续支持。下方或 PROMPTS.md 中的歪头提示词为历史记录，原图可在 0.9.0 提交 9b0c448 的源码中追溯。


# 王座休息动作生成记录（0.10.0）

日期：2026-10-02。工具：内置 image_gen.imagegen 编辑模式，transparent_background=true；未使用 CLI / API key。保留项目原始服装与鞋子，原图未覆盖。新增五张 1024×1535 透明 PNG，坐姿与过渡姿势基于同一王座帧编辑。王座设计为本次 AI 生成，天依人物仍为非官方同人形象，参见 LICENSE.txt 与仓库 THIRD_PARTY_NOTICES.md。

## magic-v1.png

输入：idle.png

```text
Use case: identity-preserve. Image 1 is the edit target: existing transparent full-body chibi Luo Tianyi fan-art sprite for a Windows desktop pet. Produce exactly ONE animation frame, not a sheet. Canvas must be exactly 1024x1535 pixels, identical to input. Keep original character identity, silver hair, jade green eyes, hair ornaments, blue-white modest Chinese-inspired costume, dark heeled boots, clean anime line art and colors. Preserve full-body scale, head position and feet baseline unless explicitly changed. Genuine transparent alpha background, including all hair-loop openings. Entire hair, footwear, effects and furniture must fit inside the canvas. No scenery, floor, cast shadow, text, rune lettering, watermark or extra characters. Change only her viewer-left hand: lift it slightly in a small spellcasting gesture with relaxed open fingers, and add a small delicate cyan-jade ring of magic with three tiny sparkles near that hand. Keep her standing upright with original gentle expression. The magic stays close to her hand, no large aura. All other character placement and body geometry remain registered to input.
```

## throne-standing-v1.png

输入：idle.png

```text
Use case: identity-preserve. Image 1 is the edit target: existing transparent full-body chibi Luo Tianyi fan-art sprite for a Windows desktop pet. Produce exactly ONE animation frame, not a sheet. Canvas must be exactly 1024x1535 pixels, identical to input. Keep original character identity, silver hair, jade green eyes, hair ornaments, blue-white modest Chinese-inspired costume, dark heeled boots, clean anime line art and colors. Preserve full-body scale, head position and feet baseline unless explicitly changed. Genuine transparent alpha background, including all hair-loop openings. Entire hair, footwear, effects and furniture must fit inside the canvas. No scenery, floor, cast shadow, text, rune lettering, watermark or extra characters. Keep the standing character in exactly the original pose and registered position. Add ONE elegant small royal throne directly behind her, facing forward: ivory frame, cyan-blue upholstery, jade accents, curved winglike top, two simple armrests and four small legs. Same cute anime style; graceful and readable at tiny size, no crown or new head ornament. Throne back rises just behind her head to no higher than her hair; chair seat behind hips; both armrests visible beside waist. Character remains in foreground; chair fits behind her existing width/silhouette, feet stay at the original baseline. No glow or magic on this fully materialized frame.
```

## throne-form-v1.png

输入：throne-standing-v1.png

```text
Use case: identity-preserve. Image 1 is the edit target, the existing transparent Luo Tianyi character with an ivory/cyan/jade throne behind her. Produce exactly ONE desktop-pet animation frame on exactly the same 1024x1535 canvas. Preserve throne design, all throne pixel positions, size and legs. Preserve character identity, gray hair, green eyes, hairstyle, ornaments, blue-white modest costume, dark heeled boots and anime art style. Entire character and throne must remain visible inside the canvas. Genuine transparent alpha background including openings in hair and furniture. No scenery, floor, cast shadow, text, rune lettering, new props, crown or watermark. Change ONLY the throne: show the exact same chair materializing at about 40 percent opacity, with a thin cyan-jade shimmer along its outer contours and a few tiny sparkles. Character remains fully opaque in exactly the input standing pose and location, expression and boots baseline. Retain actual partial alpha on the throne so this is a transparent animation transition, not a chair drawn against a background.
```

## sit-mid-v1.png

输入：throne-standing-v1.png

```text
Use case: identity-preserve. Image 1 is the edit target, the existing transparent Luo Tianyi character with an ivory/cyan/jade throne behind her. Produce exactly ONE desktop-pet animation frame on exactly the same 1024x1535 canvas. Preserve throne design, all throne pixel positions, size and legs. Preserve character identity, gray hair, green eyes, hairstyle, ornaments, blue-white modest costume, dark heeled boots and anime art style. Entire character and throne must remain visible inside the canvas. Genuine transparent alpha background including openings in hair and furniture. No scenery, floor, cast shadow, text, rune lettering, new props, crown or watermark. Change ONLY the character pose to the midway stage of sitting down onto this throne. Bend both knees slightly and lower hips toward the seat, torso upright, hands reaching toward both existing armrests. Feet stay near the original horizontal positions and boots soles remain at y=1510 approximately; original character scale and head proportions unchanged, head lowers naturally with torso. Keep knees together, modest skirt intact, hair drapes naturally. The throne stays in its exact input position. This pose must be visually BETWEEN standing and fully seated and will also be used backwards when rising.
```

## sit-v1.png

输入：throne-standing-v1.png

```text
Use case: identity-preserve. Image 1 is the edit target, the existing transparent Luo Tianyi character with an ivory/cyan/jade throne behind her. Produce exactly ONE desktop-pet animation frame on exactly the same 1024x1535 canvas. Preserve throne design, all throne pixel positions, size and legs. Preserve character identity, gray hair, green eyes, hairstyle, ornaments, blue-white modest costume, dark heeled boots and anime art style. Entire character and throne must remain visible inside the canvas. Genuine transparent alpha background including openings in hair and furniture. No scenery, floor, cast shadow, text, rune lettering, new props, crown or watermark. Change ONLY character pose to fully seated comfortably on the throne's existing cushion: hips supported by seat, torso upright, hands resting lightly on the existing two armrests, knees together, boots and heel soles resting naturally near the bottom center of the canvas. Modest dress covers upper legs; calm gentle smile, both eyes open. Retain exact original full-body character scale and huge chibi head proportions; lower head naturally with the seated torso, do not enlarge or shrink the character. Hair falls around the arms and chair naturally without hiding the boots. The throne stays in precisely the original position, unchanged.
```
