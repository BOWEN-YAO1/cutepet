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
