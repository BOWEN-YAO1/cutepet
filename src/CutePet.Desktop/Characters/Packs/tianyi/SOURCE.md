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


# 小云素材提示词（0.11.0）

模式：内置 `image_gen.imagegen`，新图生成，`transparent_background: true`。
资产：`cloud-v1.png`；工具实际输出 1774×887（提示词请求 1024×512），保留原始透明 PNG，没有裁切或缩放文件。
云是本项目的同人桌宠设计，并非官方角色设定。

```text
Use case: stylized-concept
Asset type: transparent independent cloud layer for a small Windows desktop pet.
Primary request: one small floating magical cloud that a chibi character can stand on, no character present.
Style/medium: polished hand-painted anime game sprite with soft tidy outlines, matching an elegant blue-white-jade Chinese fantasy chibi costume.
Composition: exactly 1024x512 landscape transparent canvas; a single low, wide cloud centered, occupying about 90% width and 65% height. Flat gently curved upper surface to support two feet, several rounded white lobes and delicate cyan curled wisps below; readable at 140x32 screen pixels.
Palette: white and pale icy cyan, very subtle jade sparkle, blue shaded underside. Calm and light, no smoke or storm.
Constraints: genuinely transparent background and clean alpha edges. One cloud only; no people, shoes, throne, ground shadow, scenery, frame, text or watermark. Do not add scattered particles far outside the cloud.
```


# 王座过渡帧提示词（0.12.0）

使用内置 `image_gen.imagegen` 编辑模式，`transparent_background: true`。每次使用本地参考图；保留原图，新增版本文件。最终三张均为 1024×1535 透明 PNG，没有在文件外部裁剪、插值或缩放。以下记录完整原始提示词和定向修订。

## sit-prepare-v2.png 初稿

参考（编辑目标 / 后续姿势）：throne-standing-v1.png / sit-mid-v1.png。

```text
Use case: precise-object-edit
Asset type: one transparent in-between animation frame for an existing chibi desktop-pet throne sitting sequence.
Edit target Image 1 (standing), Image 2 is the later sitting-motion reference. Create the FIRST small in-between pose, 25% of the motion from Image 1 toward Image 2. Her hands move only halfway toward the armrests, elbows beginning to bend; knees flex very slightly, hips shift slightly back and down. Keep her face, head size, green eyes, expression, hair ornaments and head orientation identical to Image 1. Do not tilt her head. The change must be modest: she is still mostly standing, beginning to sit, no large body jump.
Invariants: EXACTLY preserve the reference throne silhouette, chair geometry, ornaments, armrest positions, seat height and chair feet positions; preserve character identity, pale silver braided hair, blue-white Chinese fantasy dress, navy boots and jade accessories. Match the same painterly anime style and light. Keep the same camera and scale. Output a single PNG frame on EXACTLY 1024 x 1535 pixel canvas with true transparent background. Preserve the original canvas alignment. No scene, background, floor, glow cloud, text, frame, watermark or extra objects. Do not enlarge or recenter the throne. No motion blur, duplicates or ghosting.
```

## sit-lower-v2.png 初稿

参考（编辑目标 / 后续姿势）：throne-standing-v1.png / sit-mid-v1.png。

```text
Use case: precise-object-edit
Asset type: one transparent in-between animation frame for an existing chibi desktop-pet throne sitting sequence.
Edit target Image 1 (standing), Image 2 is the later sitting-motion reference. Create a SECOND in-between pose, approximately 60% of the motion from Image 1 toward Image 2. Her hands have reached the armrests, knees are partly flexed, hips slightly back, lowering toward the seat but not seated yet. Her boots remain fully visible and aligned consistently. Preserve the face, head size, green eyes, expression, head orientation and hair ornaments. Do not tilt the head. A believable gentle lowering pose between the two reference poses, not a new static seated pose.
Invariants: EXACTLY preserve the reference throne silhouette, chair geometry, ornaments, armrest positions, seat height and chair feet positions; preserve character identity, pale silver braided hair, blue-white Chinese fantasy dress, navy boots and jade accessories. Match the same painterly anime style and light. Keep the same camera and scale. Output a single PNG frame on EXACTLY 1024 x 1535 pixel canvas with true transparent background. Preserve the original canvas alignment. No scene, background, floor, glow cloud, text, frame, watermark or extra objects. Do not enlarge or recenter the throne. No motion blur, duplicates or ghosting.
```

## sit-near-seat-v2.png 初稿

参考（编辑目标 / 后续姿势）：sit-mid-v1.png / sit-v1.png。

```text
Use case: precise-object-edit
Asset type: one transparent in-between animation frame for an existing chibi desktop-pet throne sitting sequence.
Edit target Image 1 (midway), Image 2 is the final seated reference. Create the THIRD intermediate pose, halfway between Image 1 and Image 2: gently lower the hips toward the cushion, bring knees and boots slightly closer together toward Image 2, hands settled on the armrests. Preserve her face, green eyes, calm expression, head orientation and head size. Do not tilt her head. Keep the costume and skirt coverage unchanged, with natural gentle folds. This is almost seated but before the final settled sitting pose.
Invariants: EXACTLY preserve the reference throne silhouette, chair geometry, ornaments, armrest positions, seat height and chair feet positions; preserve character identity, pale silver braided hair, blue-white Chinese fantasy dress, navy boots and jade accessories. Match the same painterly anime style and light. Keep the same camera and scale. Output a single PNG frame on EXACTLY 1024 x 1535 pixel canvas with true transparent background. Preserve the original canvas alignment. No scene, background, floor, glow cloud, text, frame, watermark or extra objects. Do not enlarge or recenter the throne. No motion blur, duplicates or ghosting.
```

## sit-prepare-v2.png 定向修订（最终）

参考：throne-standing-v1.png；初稿屈膝过深，最终仅开始抬手，腿保持接近站姿。

```text
Use case: precise-object-edit
Asset type: first tiny in-between frame of an existing chibi desktop pet preparing to sit.
Edit ONLY the arms and hands of the input standing character, plus a very subtle bend in her knees. Her legs MUST remain almost straight, essentially identical to the input standing pose. Her skirt, hips and boots stay at the same positions; do NOT turn her into a seated or squatting pose. Her palms move halfway from their current low positions towards the chair armrests (hands must be between the current position and armrests, not already resting on them). Elbows gently begin to bend. This is a 15% start of sitting, not a halfway pose. Preserve the entire face, head orientation and scale, expression, hair, costume and EVERY chair pixel/position unchanged. No head tilt. Same transparent canvas EXACTLY 1024x1535. One transparent PNG frame, no scene, text, duplicate figure, blur or watermark.
```

## sit-lower-v2.png 定向修订（最终）

参考：最终 sit-prepare-v2.png / 原 sit-mid-v1.png；降低屈膝幅度，使它位于准备与半坐之间。sit-near-seat-v2.png 采用初稿。

```text
Use case: precise-object-edit
Asset type: intermediate transparent sitting animation sprite.
Image 1 is the edit target, an almost standing character. Image 2 is the later half-seated pose reference. Create a pose ONLY ONE THIRD of the way from Image 1 to Image 2, specifically the knees must be only slightly bent (about 15 degrees), with the legs still mostly extended downward, NOT the deep bent legs in Image 2. Her hands move a little farther outward and gently down towards the armrests, but her hips and dress lower just a little. This is an early gentle descent, not a completed sitting posture. Keep the face, head pose, scale, outfit, navy boots and all ivory/cyan/jade throne geometry unchanged. Keep chair feet and armrests EXACTLY at their input positions. No head tilt, no change of costume, no exaggerated squat. Exactly 1024x1535 transparent PNG canvas, same camera and framing, a single aligned frame with clean alpha. No scene, blur, duplicates, text, watermark.
```


# 坐姿动作素材提示词（0.13.0）

工具：内置 image_gen.imagegen；模式：基于 sit-v1.png 编辑；transparent_background=true。原始生成文件保留，项目内使用版本化副本。

## sit-blink-v1.png

Use case: precise-object-edit
Asset type: one transparent seated desktop-pet animation frame.
Input image is the edit target and fixed seated base pose.
Change ONLY the eyes to a gentle natural blink: both eyelids closed, thin curved eyelid lines. Keep mouth expression the same.
Invariants: she remains FULLY SEATED on the same throne; preserve exact throne geometry, feet, cushion and armrest positions; preserve character identity, pale silver braided hair, hair ribbons and jade ornaments, blue-white Chinese fantasy dress and navy boots. Keep torso, skirt coverage, knees, boots and hair at the exact input positions. Preserve the same painterly anime sprite style, scale, camera and lighting. No whole-body movement. Exactly 1024x1535 transparent PNG canvas, identical alignment, clean alpha. No scenery, floor, shadow, cloud, extra items, text, watermark, motion blur, duplicated limbs or ghosting.

## sit-happy-v1.png

Use case: precise-object-edit
Asset type: one transparent seated desktop-pet animation frame.
Input image is the edit target and fixed seated base pose.
Change ONLY the face expression: a small warm happy smile, slightly happier eye expression with the original green eyes still open, very subtle rosy cheeks. Keep the head orientation exactly the same, no head tilt, no big open mouth or laughing face.
Invariants: she remains FULLY SEATED on the same throne; preserve exact throne geometry, feet, cushion and armrest positions; preserve character identity, pale silver braided hair, hair ribbons and jade ornaments, blue-white Chinese fantasy dress and navy boots. Keep torso, skirt coverage, knees, boots and hair at the exact input positions. Preserve the same painterly anime sprite style, scale, camera and lighting. No whole-body movement. Exactly 1024x1535 transparent PNG canvas, identical alignment, clean alpha. No scenery, floor, shadow, cloud, extra items, text, watermark, motion blur, duplicated limbs or ghosting.

## sit-wave-v1.png

Use case: precise-object-edit
Asset type: one transparent seated desktop-pet animation frame.
Input image is the edit target and fixed seated base pose.
Change ONLY the hand and wrist on the RIGHT SIDE OF THE IMAGE: raise that hand a SMALL amount, approximately 90 image pixels above its original armrest position, bend the elbow minimally and turn the relaxed palm slightly towards the viewer as a tiny seated wave. Keep the shoulder and upper arm position nearly fixed. The other hand remains resting on its original armrest. No wide waving gesture. Keep the entire face and head unchanged.
Invariants: she remains FULLY SEATED on the same throne; preserve exact throne geometry, feet, cushion and armrest positions; preserve character identity, pale silver braided hair, hair ribbons and jade ornaments, blue-white Chinese fantasy dress and navy boots. Keep torso, skirt coverage, knees, boots and hair at the exact input positions. Preserve the same painterly anime sprite style, scale, camera and lighting. No whole-body movement. Exactly 1024x1535 transparent PNG canvas, identical alignment, clean alpha. No scenery, floor, shadow, cloud, extra items, text, watermark, motion blur, duplicated limbs or ghosting.


# 屏幕边缘动作提示词 · v0.16.0

使用内置 image_gen 工具，透明背景。引用现有 idle.png 保持角色形象，保留原图；第二张引用 edge-v1.png，仅改变表情。所有输出原样复制进角色包，不裁剪、不重绘。

## edge-v1.png

Use case: identity-preserve.
Asset type: transparent PNG animation frame for the CutePet desktop character pack.
Input image: reference for exact character identity, clothing, painting style, and head size; preserve these invariants.
Create a new natural LEFT SCREEN EDGE peeking pose of this same chibi Luo Tianyi: silver-lavender looped hair and long braids, green eyes, blue ribbons and green clover ornament, blue-white Chinese fantasy dress. She is upright behind an imaginary vertical border on the LEFT, leaning her head and shoulders gently toward the RIGHT into view, with two small anatomically correct hands holding the imaginary left vertical edge. Her lower torso and legs remain concealed to the left, not a full standing body. Curious calm expression, open eyes, subtle smile. No throne, no cloud.
Composition: retain the exact reference canvas size 1024 x 1535 and transparent background. Keep the face approximately the same pixel size as the reference (about 550px wide). Hair top near y=140, face center near x=500 y=650, hands along x=35 to 140 at y=850 and 1100. Visible body/hair end by y=1400. Leave alpha-transparent space around the entire silhouette. Only the character is drawn: the imaginary vertical edge, wall, screen and any supporting structure are NOT drawn. Pose must read as peeking around a vertical edge, not standing with arms open, not a sideways-rotated character.
Constraints: same detailed polished anime watercolor shading and clean edges as reference, single character, no text, no watermark, true RGBA alpha transparency, no checkerboard painted in image, no duplicate limbs.

## edge-smile-v1.png

Use case: identity-preserve.
Asset type: second animation frame of the same transparent CutePet screen-edge sprite.
Input image: EXACT base-frame edit target. Change ONLY the facial expression: gently close both eyes into happy curved eyelids and open her little mouth into a modest cheerful smile. Keep the entire pose, hand shapes and locations, tilted head angle, outline, hairstyle, clothing, proportions, and every part's pixel position unchanged. Same hands holding an imaginary LEFT vertical edge, same silver-lavender hair, green ornaments and blue-white dress. Do not lean farther, add objects or draw the border. Match the exact 1024 x 1535 canvas and RGBA transparency of the input. No throne, no cloud, no text, no watermark, no checkerboard, no extra limbs. This will alternate with the base frame, so geometric consistency is crucial.

# 上下边缘动作提示词 · v0.17.0

使用内置 image_gen 工具，透明背景。现有 idle.png 用于保持角色形象；表情变化引用各自的基础姿势。四张新图原样复制到角色包，不裁剪、不重绘，原素材保留。

## edge-top-v1.png

Use case: identity-preserve.
Asset type: transparent CutePet desktop animation frame.
Input image: character identity and painting-style reference. Preserve the exact chibi Luo Tianyi identity, silver-lavender looped hair and long braids, green eyes and green clover hair ornament, navy ribbons, blue-white Chinese fantasy dress, face proportions and polished anime watercolor shading.
Primary request: a NEW upright hanging / peeking pose for the TOP edge of the desktop. She reaches both arms up and holds an imaginary horizontal upper border with two little hands, while her head and upper body hang naturally BELOW her hands, looking gently downward toward the viewer with curious open green eyes and a small smile. Keep head upright, naturally bend the arms, draw exactly two hands and anatomically plausible shoulders. Show only head, arms and upper torso, lower body concealed, no legs. No sideways or upside-down rotation.
Composition: exact 1024 x 1535 RGBA transparent canvas. The two hands touch the imaginary horizontal top edge around y=90, one at x=220 and one at x=800. Hair starts below that near y=150; face approximately 550 pixels wide, face center near x=512 y=700; blue-white sleeves frame the face. Body and long braids extend downward naturally and end before y=1450. Centered balanced silhouette. Keep a small transparent outer margin. Do NOT draw the imaginary edge, a wall, a platform, cloud or throne. Only the character cutout.
Constraints: preserve reference design and fine details, single character, true alpha transparency, no painted checkerboard, no text, no watermark, no extra limbs.

## edge-bottom-v1.png

Use case: identity-preserve.
Asset type: transparent CutePet desktop animation frame.
Input image: reference for exact character identity, clothing, head proportions and detailed polished anime watercolor style.
Primary request: a NEW BOTTOM-edge chin-rest pose of the same chibi Luo Tianyi, silver-lavender looped hair and braids, green eyes, green clover ornament, navy ribbons, blue-white Chinese fantasy dress. She leans forward comfortably over an imaginary horizontal ledge, rests BOTH elbows on that ledge, and cups her cheeks / chin gently in both hands. Curious relaxed open-eye expression with a small smile, looking at the viewer. Visible head, shoulders and sleeves only; lower torso hidden below the imaginary ledge. No full standing body, no legs, no throne or cloud.
Composition: exact 1024 x 1535 RGBA transparent canvas. Head approximately 550 pixels wide and face centered near x=512 y=700. Hands cup her chin around x=330 and x=690, y=1080; elbows and the bottoms of the two blue-white sleeves sit on an imaginary straight horizontal support around y=1370 (89.3% canvas height). Long braids remain above or finish at that support. Keep the silhouette centered with outer transparent margins. The ledge, desk, taskbar and border are NOT drawn; only the character cutout is drawn. No art extends beneath the elbow contact line except tiny natural sleeve folds.
Constraints: preserve detailed character design and identity, natural symmetrical two-hand chin-rest pose, true alpha transparency, no checkerboard, no text, no watermark, no extra hands or limbs.

## edge-top-smile-v1.png

Use case: identity-preserve. Asset type: transparent CutePet animation expression frame. Input image is the EXACT base-frame edit target.
Change ONLY her facial expression: gently closed eyes with happy curved eyelids and a modest cheerful open-mouth smile. Preserve ALL geometric positions: head angle, hair outline, hands, fingers, arms, body, clothing, ornaments, silhouette and contact line. Preserve the exact 1024 x 1535 RGBA transparent canvas and detailed anime watercolor shading. No head movement or repositioning; this will alternate with the base frame, geometric registration is crucial. No text, watermark, painted checkerboard or new objects. Keep both raised hands holding the imaginary TOP border in exactly the same places.

## edge-bottom-smile-v1.png

Use case: identity-preserve. Asset type: transparent CutePet animation expression frame. Input image is the EXACT base-frame edit target.
Change ONLY her facial expression: gently closed eyes with happy curved eyelids and a modest cheerful open-mouth smile. Preserve ALL geometric positions: head angle, hair outline, hands, fingers, arms, body, clothing, ornaments, silhouette and contact line. Preserve the exact 1024 x 1535 RGBA transparent canvas and detailed anime watercolor shading. No head movement or repositioning; this will alternate with the base frame, geometric registration is crucial. No text, watermark, painted checkerboard or new objects. Keep BOTH hands cupping her cheeks / chin and BOTH elbows resting on the imaginary BOTTOM support in exactly the same places.


# 上沿探头重绘提示词 · v0.18.0

使用内置 image_gen，2026-10-03。第一轮手位不符合上侧用途，随后定向修正；下列提示词完整保留。原始生成输出保留，新素材使用版本化文件名。

## 参考重绘（初稿，未接入）

```text
Use case: stylized-concept
Asset type: transparent PNG desktop pet top-screen-edge sprite, 1024 x 1535 canvas.
Input image 1: character identity and painting style reference only. Redraw a NEW pose.
Primary request: this same Luo Tianyi chibi girl is playfully leaning over the TOP edge of the screen from OUTSIDE above, looking down into the screen. Only her adorable upright face, crown hair buns, short side hair, and TWO SMALL relaxed hands are visible. Her torso, shoulders, forearms and long braids are hidden above the imaginary top screen edge. It is a relaxed peek, not hanging by raised arms.
Composition: virtual horizontal contact line y=150 on the 1024x1535 transparent canvas. Center head around x=512,y=440. Two small hands rest naturally over the imaginary line near x=210 and x=814, fingers loosely curled down over the line. Fingers/hands contact y=150. Keep hands short and at temple sides, no long arms above head. Hair buns may extend above the contact line, they will be clipped by the screen. Visible head and short hair end by y=850; the lower 600 pixels remain transparent. Slight downward leaning head, face readable and almost upright, open green eyes and tiny gentle smile. Preserve silver-grey hair, blue ribbons, green clover hair accessory, original painterly anime chibi style and face identity.
Background: real transparent alpha, no drawn ledge, no frame, no shadow panel, no text or watermark.
Avoid: torso or clothing below head, raised arms, fists, long dangling braids, upside-down face, full body, disconnected hands, extra fingers, additional objects. One character only. Output exact 1024x1535 PNG, keep full canvas and transparent margins.
```

## 上侧手位修正（接入版 edge-top-v2.png）

```text
Edit target: the newly generated head-only transparent sprite. Keep her exact face, hair identity, colors and painting style. Correct the pose for TOP edge peeking: MOVE BOTH HANDS from BELOW the chin to ABOVE and BESIDE the crown, centered at (230,150) and (795,150). Tiny relaxed fingers curl DOWN from an invisible contact line y=150, not fists. Move head down so eyes around y=480 and chin around y=650, head crown around y=120. Short hair ends y=720. Body is entirely outside canvas above the top screen border, and she leans forward to look downward into screen. No hands below chin. No arms, torso, long braids, clothing or drawn edge. Hands connect plausibly to wrists disappearing above y=150; minimal wrists only, not extended raised arms. Top-edge composition, NOT looking over a bottom ledge. Preserve exact 1024x1535 transparent canvas and leave all below y=800 empty alpha. No text or background.
```

## 表情帧（edge-top-smile-v2.png）

```text
Use case: precise-object-edit
Edit target: the supplied transparent top-edge peeking head sprite. Change ONLY facial expression: both green eyes become soft closed smiling crescent eyes, tiny mouth becomes a cheerful small smile. This is a paired animation frame: keep the exact same head position, head silhouette, hair strands, buns, blue ribbons, clover accessory and TWO hands pixel-aligned with original; do not move, resize or redraw their positions. No movement of wrists, fingers or contact points. Keep exact 1024x1535 transparent canvas and all transparent margins. No torso, arms, clothing, long braids, ledge, background, text or watermark. Preserve painting style, colors and alpha transparency.
```


上沿 v2 仅替换上沿动作引用，旧 v1 文件保留在源码；角色包导出仅包含当前引用图片。接触线 edgeTopAnchorY = 0.12。原始生成文件保留于 image_gen 默认生成目录。


# 上沿秋千提示词 · v0.19.0

使用内置 image_gen，2026-10-03。坐姿原图原样复制；绳子由 WPF 独立绘制。微笑帧两次生成出现背景与位置变化，未接入，仅保留提示词供后续重试。

## 坐姿（接入 swing-v1.png）

```text
Use case: stylized-concept
Asset type: transparent PNG seated-on-swing desktop pet sprite, exact 1024x1535 canvas.
Input image 1 is identity and painterly anime style reference only; redraw a new seated pose.
Primary request: Luo Tianyi chibi girl comfortably seated on a SMALL SIMPLE WOODEN SWING SEAT, full seated figure facing viewer with open green eyes and gentle smile. Same silver-gray double hair buns and long side braids, green clover hair accessory, dark blue ribbons, white and blue Chinese-inspired dress, matching boots. Knees gently together, feet dangling relaxed below the seat. Hands relaxed on the lap, elegant friendly posture.
Composition: center at x512. Leave TOP y0..230 totally transparent for suspension ropes to be rendered separately by the app. Hair crown starts at y270. Wooden seat is a thin horizontal board with rounded ends, from x180 to x844, its TOP SURFACE at y1070. Hair/legs hang below but end by y1440. Entire subject fits x80..945, no cropping. Board must have visible clear corners at x210 and x814 at y1070 to attach the ropes.
Constraints: DO NOT DRAW ROPES, chains, swing frame or poles: the application will render moving ropes. Only character and wooden board, transparent background with genuine alpha. No throne, cloud, ground, text, watermark, extra character or objects. Preserve face and detailed original art style. Exact 1024x1535 PNG, preserve blank top margin.
```

## 微笑帧（未接入）

```text
Use case: precise-object-edit
Edit target: supplied transparent Luo Tianyi seated on a small wooden swing board sprite. Change ONLY her facial expression to cheerful closed-eye smile: both eyes soft smiling crescent shapes, small happy open mouth. Keep EXACT same seated pose, face location, hair silhouette, board shape and location, hands on lap, legs, boots, dress, colors and painting style. Animation pair must be registered and pixel-aligned. No ropes, poles or frame, no extra object, text, background or shadow. Preserve original exact 1024x1535 transparent canvas with alpha and all blank margins.
```

## 透明背景修正（未接入）

```text
Use case: background-extraction
Edit target image 1: smiling girl seated on swing board. Remove ALL background and glow/halo so only the girl and board remain as a clean transparent-alpha PNG cutout, INCLUDING transparency inside the open loops of her two hair buns and between separate hair strands. No opaque black or gray background, no blurred glow, no colored fringe. Preserve original illustration and expression, exact placement, size and 1024x1535 canvas; do not change the face, body, board, hands or boots. Reference image 2 is the registered open-eye animation pair: preserve same silhouette details and clean transparency as image 2, but retain smiling closed eyes from image 1. Keep the blank top margin and no ropes. Output actual alpha transparency.
```


原始文件 exec-6eb3aa3a-3385-428a-8154-14e9a061d018.png 保留在 image_gen 默认生成目录。上侧秋千替换上侧动作引用，旧上侧素材保留在源码。


# 仙气秋千提示词 · v0.20.0

2026-10-03，使用内置 image_gen 编辑已有 swing-v1.png。新坐姿保存为 swing-jade-v2.png；保留旧木板素材。悬绳由现有 WPF 图层绘制，颜色 #77B4A8，不画入 PNG。

```text
Use case: precise-object-edit
Asset type: transparent PNG fantasy swing desktop-pet sprite, exact 1024x1535.
Edit target: supplied Luo Tianyi girl seated on a plain wooden swing board. Preserve the EXACT character artwork, pose, face, hair, dress, hands on lap, boots, seated position and scale. Change ONLY the swing seat and its small decorative fittings.
Primary request: replace the chunky brown wood board with a graceful xianxia celestial swing seat: a slim pearl-white and pale celadon jade seat, softly curved carved cloud-scroll ends, delicate fine antique gold edge line, subtle blue-green auspicious cloud engravings. Its elegant silhouettes look light and magical, like a small floating jade cloud supporting her, while still clearly a swing seat. Add small jade ring suspension fittings at each outer end, and TWO SHORT turquoise silk tassels hanging from the underside near the outer ends. Tiny restrained pearlescent highlights on the jade, not a glowing aura.
Geometry invariant: seat top contact line stays at y1060, left/right suspension fittings centered at x145 and x880, so separately rendered suspension cords connect. Preserve board footprint approximately x130..912, y1050..1130. Cloud-scroll ends can rise only slightly. Tassels end before y1270, do not cover boots or hands. No large throne back, no flowers covering the person.
Background: genuine transparent alpha cutout, clean open transparent holes in hair loops and between strands. Preserve all blank margins, especially top y0..190. No ground, no atmosphere, no particles scattered across canvas, no halo or background glow, no drawn ropes or chains, no frame, no text or watermark. Keep same detailed painterly anime style. One character, exact 1024x1535 PNG.
```

原始生成文件 exec-a2b4ca54-aaf5-4146-89c1-31bb976498cf.png 保留在 image_gen 默认生成目录。


# 秋千悬绳挂饰提示词 · v0.21.0

2026-10-03，使用内置 image_gen 生成独立挂饰，未引用人物。接入 rope-ornament-v1.png；坐姿继续使用青玉秋千 v2。曾尝试进一步重绘坐板，图像工具输出审核拦截，无输出图片、未接入。下方保留尝试记录。

## 独立挂饰（已接入）

```text
Use case: stylized-concept
Asset type: transparent PNG ornamental pendant for a fantasy jade swing, exact 1024x1535 canvas.
Primary request: ONE elegant vertical xianxia rope ornament, only the accessory with no character or human figure. A small round pale celadon jade bead at top, a delicate antique-gold Chinese knot around a tiny pearl-white lotus with translucent jade petals in the middle, and a short fan of turquoise silk tassel strands at bottom, with two fine ivory-gold ribbons curling gracefully beside the tassel. Jewel-like highlights, subtle intricate gold filigree and small pearl accents. Bright moon-white, teal jade and pale gold palette, detailed painterly anime prop style, luxurious celestial and magical but clear clean silhouette readable at very small size.
Composition: single pendant centered x512. Top loop/contact point x512,y200; ornament occupies approximately x220..800, y180..1360, no cropping. Symmetric front view. Large enough to fill central canvas, transparent margins all around. No long rope above, no swing seat or poles, no other objects.
Background: actual transparent alpha, no background color, no shadow panel, no glow outside the ornament, no particles, no text or watermark. Exact 1024x1535 PNG.
```

原始生成文件 exec-ea4a2c5e-be9b-4267-83f0-3e94505f7f13.png 保留在 image_gen 默认生成目录。

## 坐板装饰重绘尝试（被工具拦截，未接入）

```text
Use case: precise-object-edit
Asset type: transparent PNG ornate celestial swing seated sprite, exact 1024x1535.
Edit target: supplied Luo Tianyi seated on pearl-white celadon jade cloud-scroll swing.
Change ONLY embellishments on the swing seat, keep character artwork and pose exactly: add a small pale turquoise lotus ornament at each outside cloud-scroll end (a few pearl white translucent-looking carved petals with fine gold outlines), delicate gold filigree on the existing cloud engraving, and 3 tiny pearl beads along each seat-end trim. Slightly enrich the existing jade ring fittings with delicate gold collars. Keep existing two turquoise silk tassels, don't lengthen them.
Preserve seat anchor line near y1060, entire original seat footprint and canvas alignment, girl face, hands, hair, clothes, boots, color palette and original detailed painterly anime style. Embellishments must stay within the original silhouette plus at most 20px, not obscure character. Elegant fantastical xianxia accessories, clearer details readable at small size.
Actual alpha transparency, clean cutout and all transparent holes and margins. No ropes, frame, ground, background, halo, aura, particles, text or watermark. Output exact 1024x1535 PNG.
```


# 秋千两侧云莲小景提示词 · v0.22.0

2026-10-03，使用内置 image_gen 制作独立配景，不引用人物。保存为 scenery-lotus-v1.png，生成画布实际为 1254×1254；原样复制，透明 PNG。左右复用同一图片，右侧由 WPF 镜像；飘带与星光由 WPF 绘制。

```text
Use case: stylized-concept
Asset type: transparent square PNG small scenery decoration for a celestial desktop swing, exact 1024x1024 canvas.
Primary request: ONE compact asymmetric celestial cloud-and-lotus cluster prop, no people or characters. Pearl-white softly curled auspicious cloud scrolls form a small floating island. A pale celadon and moon-white lotus with delicate gold petal edges blooms from it, with two graceful jade-green lotus leaves and a short fine gold curling vine. A couple of tiny pearls rest on the cloud. Whimsical Chinese xianxia fairy-garden style, rich detailed painterly anime prop illustration, matching pale jade, teal, ivory and antique gold colors. Cloud base faces left and lotus rises to the right, pleasing silhouette. Small scene reads clearly at icon size; leaves create distinct silhouette rather than dense clutter.
Composition: single cluster centered, fills central 800x750 region of a 1024x1024 square, complete intact silhouette, generous transparent margins. No big moon, no landscape frame, no floor, no background, no floating particles outside cluster, no text or watermark, no ropes or swing, no human figure. Clean genuine transparent alpha including holes between leaf stems. No glow/halo around cutout. Output exact 1024x1024 PNG.
```

原始生成文件 exec-40a7872e-5ac2-43ee-becf-bedd6f4a7cb2.png 保留在 image_gen 默认生成目录。


## v0.23.0 连贯花藤配景

保留 scenery-lotus-v1.png、rope-ornament-v1.png 与 swing-jade-v2.png 原图。云莲缩小并移至坐板两侧，新增侧枝、玉叶、小花、珠饰及柔雾由项目 WPF 原生几何和渐变绘制；没有新生成或编辑的位图，原提示词与来源继续适用。布局配置为 topSwing.scenery.layout: garden；省略时为 floating。


## 0.23.1 两侧莲花姿态修正

保留云莲原图，garden 布局通过 WPF 绘制变换向外轻斜，并错开两侧大小和高度。没有新增或编辑的 PNG，原提示词和素材权利说明继续适用。


## 0.24.0 花月风铃配景

月牙玉坠、风铃、蝴蝶和三种小花由项目 WPF 原生几何与渐变绘制，没有新增或编辑 PNG。沿用既有花藤、悬绳挂饰、云莲原图和秋千坐姿，原提示词与素材权利说明继续适用。garden 配景启用这些图案，不改变导入 / 导出或解码预算。


## 0.25.0 独立环绕花藤

新增两簇不连绳的花藤、十朵花和四十片叶由项目 WPF 原生几何绘制，复用 v0.24 三种花形、原叶片几何和渐变。没有新增或编辑 PNG，原提示词和素材权利说明继续适用。garden 配景启用独立花藤，不改变导入 / 导出或解码预算。


## 0.26.0 上沿分枝花藤

独立花藤根部接到桌面上沿，主藤增加弯曲、六条侧枝和末端卷须，分枝补花叶。由项目 WPF 原生几何绘制，沿用原花形和叶片，无新增或编辑 PNG，原提示词与素材权利说明继续适用。根点固定，运行时只更新缩放及绕根点轻摆。


## 0.27.0 绚彩花型

花饰改为六种原生花型：玫粉重瓣、紫色星形、蓝紫垂铃、金黄菊形、桃粉梅形和青玉莲形。由项目 WPF 几何、渐变及花丝花蕊绘制，无新增或编辑 PNG，原提示词与素材权利说明继续适用。图案冻结并共享于全部花藤，沿用上沿根点固定和分枝卷须。
