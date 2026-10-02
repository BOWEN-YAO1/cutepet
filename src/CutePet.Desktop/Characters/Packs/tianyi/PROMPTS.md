# 天依新增动作素材历史记录（0.9.0）

0.9.1 已移除歪头动作和 tilt-v2.png。以下保留完整生成历史；当前使用其余五张新增图，歪头图不再随当前角色包分发。

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

