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

