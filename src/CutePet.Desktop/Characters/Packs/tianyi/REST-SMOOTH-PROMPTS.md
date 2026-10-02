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
