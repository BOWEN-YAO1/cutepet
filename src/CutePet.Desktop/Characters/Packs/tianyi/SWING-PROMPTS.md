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

