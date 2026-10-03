# 逐帧侧身探出提示词 · v0.33.0

2026-10-03，使用内置 image_gen；参考 idle.png 生成八张姿势的透明图集，再修正裙摆与扶边细节。最终 edge-sequence-v3.png 为 1536×1024 RGBA，原样复制，不裁剪或缩放文件。运行时按 384×512 区域取帧，逐帧对齐扶边点，角色右侧由 WPF 镜像。原角色权利说明继续适用。

## 图集生成

原始输出 exec-d0f3cf84-7103-413d-9edb-a5b7f969b524.png 保留在默认生成目录，未接入。

```text
Use case: identity-preserve animation sprite-sheet generation from the provided Luo Tianyi reference. Transparent PNG landscape game animation atlas, EXACTLY 1536 x 1024 pixels, arranged as FOUR columns and TWO rows of EIGHT equal 384 x 512 cells. No cell outlines, no labels, no text, no background. One same character in each cell, chronological poses read left-to-right, top-to-bottom. Same detailed anime watercolor artwork, silver gray hair with loop buns and long braids, green eyes and clover hair jewel, blue-white Chinese fantasy dress, dark decorated boots.

CRITICAL MOTION: character is hiding BEHIND an imaginary vertical LEFT desktop border at local x=192 in EVERY cell. Most of her waist, skirt, legs and shoes stay LEFT of this line (will be hidden by software). Only her head, shoulders and a progressively larger PART of the connected waist and skirt lean RIGHT across the line. Do NOT place a complete free-floating standing character to the right. Do NOT simply translate the identical pose. Head leans out first, shoulder rotates, arm bends at elbow around fixed gripping hands, torso twists, waist/skirt follow later with a curved connected silhouette. At maximum lean, some skirt edge comes out but legs and shoes remain hidden on the left. The body still exists behind the imaginary border; never terminate a torso at its waist or draw a detached head.

REGISTRATION: local coordinates measured from each 384x512 cell origin. Imaginary border x=192 fixed for all eight cells. Two small hands grasp that same line around (192,250) and (192,310); hands and grip height stay aligned between cells. Hair top roughly y=30; same head scale about 180 pixels, head bottom around y=250, shoulders y=270, waist y=340, connected skirt y=360..425, both legs/boots mainly x=110..175,y=425..500. Keep each whole pose inside its own cell with 12px safe margins. Nothing overlaps neighboring cells. No wall or line drawn.

Eight DISTINCT physically progressive poses:
1 top-left: shallow curious peek, nearly all torso and skirt behind x192; just right eye/cheek, little hair and gripping fingertips showing on right. Head center ~155.
2: head turns further into view, visible face and shoulder, waist still behind edge. Head center ~180.
3: bent upper body leans out, head center ~205, shoulder and sleeve extend, only a narrow connected waist edge on right.
4 top-right: deeper curious side lean, head center ~230, head slightly tilted, elbow bends and upper torso twists, connected waist and small upper skirt fold cross edge.
5 bottom-left: deepest friendly lean with open green eyes, head center ~245, face tilts gently, a partial skirt edge visibly curves out but legs stay behind edge. Both hands still grip at x192.
6: cheerful closed-eye smile in the deep leaning pose, subtle head nod and shoulder change, matching connected partial skirt, legs remain behind edge.
7: lean begins relaxing, head center ~225, green eyes reopen, torso gently twists back, less skirt visible.
8 bottom-right: returning half peek, head center ~190, shoulder and waist retreat, hands maintain grip, skirt almost entirely behind edge.

Keep identity/proportions/colors perfectly consistent across all cells, eight drawings of the SAME character, varying articulations rather than eight unrelated people. No props, throne, clouds, ground shadows, glow, backdrop, border marks, feet in the exposed right half, missing anatomy, detached limbs, extra hands, text or watermarks. Genuine RGBA transparency including holes in hair.
```

## 图集修正（最终接入）

参考上一步图集。最终原始输出 exec-1a98a438-a8d2-48f9-877b-e63d61a0f7b3.png 保留在默认生成目录。

```text
Use case: precise-object-edit. Edit this EIGHT-FRAME Luo Tianyi peeking animation atlas. Preserve the eight faces, identities, costumes, chronological reveal stages and FOUR-column TWO-row grid, EXACT 1536x1024 PNG. Every cell is 384x512. It is crucial to preserve genuine transparent background; all background and empty margins have alpha=0, all solid character interiors alpha=255, no fog or halo.

FIX ONLY THE LOWER SILHOUETTE AND GRIP REGISTRATION:
The current pictures are cropped through hair and clothing at the bottom of each cell. Repair this: draw a complete continuous waist into the connected skirt with its actual curved skirt hem entirely inside local y=450. No cut through the body on the horizontal y=512 cell boundary. The lower torso bends LEFT behind the invisible vertical edge, so only a partial skirt edge emerges and that edge curves back toward the hidden left half at the bottom. ALL legs and boots stay hidden BEHIND that edge, NEVER appear in the right exposed half. This is a person slowly leaning from BEHIND an edge, not a floating complete body and not a half-body bust ending in midair. Keep long braid and ribbon tips intact before local y=475, leave at least 20 transparent pixels below all visible artwork.
In each frame preserve its current imaginary vertical border x (the cut line where hands grasp), do not draw any actual border or wall. Two gripping hands should have a consistent height: upper hand centered local y=270 and lower hand y=325, while arms and shoulders bend naturally to accommodate the distinct leaning head poses. Head can tilt and shoulders twist, but lower body stays mainly behind the same invisible edge.
Keep shallow stages (1,2,8) showing little/no skirt, intermediate (3,7) a connected sliver of waist/skirt, deepest (4,5,6) more of the connected waist and modest partial skirt. In all frames the lower skirt curves back left behind the edge. The face progression and variety from the original remain: curious, leaning, surprised, smiling, nodding, retreating. No detached body parts, no feet floating below the torso, no horizontal cut through hair, no clouds, ground, throne, props or scenery. Preserve exact grid, character scale and nice anime watercolor artwork. True transparent RGBA with clean cutout.
```

图集有八张不同姿势：浅探头、露肩、侧身露腰、部分裙摆跟随、较深探出、闭眼微笑、回收及浅探头变体。四组动作使用不同顺序和停留时间，每组 11～13 帧引用，约 1.76～2.34 秒；不是骨骼动画。帧内身体大部分藏在边缘外，裙摆逐渐出现，未使用 v0.32 两张完整人物图。图集和裁切视图均计入像素预算，同区域跨动作复用。
