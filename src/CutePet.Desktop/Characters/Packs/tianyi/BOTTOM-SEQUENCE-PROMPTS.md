# 下沿托腮动作提示词（0.34.0）

使用内置 image_gen 工具，以现有 edge-bottom-v1.png 为参考补绘八种托腮姿势，再修整图集留白。最终 bottom-sequence-v4.png 为 1536×1024 RGBA 原始输出，原样复制；运行时按 4 列 2 行、384×512 区域取帧。依次为基础托腮、轻抬头、抬头张嘴、左看、右看、闭眼歪头笑、右倾微笑、半闭眼回落。三组回应共用八张姿势，以不同顺序和停留组成动作。每帧 edgeAnchorY 按实际两侧袖底标定，固定接触线，无额外身体拉伸。AI 绘制的细节和姿势间仍可能有轻微变化。

原角色权利说明沿用 SOURCE.md。旧素材和未接入的 v2 / v3 草稿留在源码；导出的角色包只带最终引用图集。历史提示词全文另见 SOURCE-HISTORY-v033.md；该档案在匹配源码包中保留，SOURCE.md 嵌入本版全部提示词，便于角色包导出。

## 原始输出

- v2 草稿：exec-5179f8d3-5af2-4c29-8684-dfe8494989ee.png
- v3 草稿：exec-b686137f-153c-4c1c-864b-b6ebb502ec3d.png
- v4 接入：exec-c63c2032-6a21-4714-8f23-0c1fcce38098.png

## 初始提示词

Use case: identity-preserve game animation atlas. The supplied image is the exact Luo Tianyi desktop pet chin-on-hands reference. Create EIGHT distinct matched animation poses in a transparent PNG atlas, EXACT 1536x1024, FOUR columns TWO rows, each cell EXACT 384x512. Chronological cells read left to right then top row to bottom row. Same silver-gray looped buns, braids, emerald green eyes, clover jade jewel, blue ribbons, blue-white Chinese fantasy dress, painterly detailed chibi anime style as reference.

Scene: girl leaning on the TOP edge of an invisible taskbar, head supported in two small palms, BOTH elbows / sleeve bottoms resting on a horizontal line at local y=465. No desk, surface, edge, line, border or props painted. The application will align that contact line with the taskbar. Keep elbow support contact almost perfectly stationary across frames; upper arms bend, palms follow cheeks, shoulders subtly twist and head raises or tilts. Do NOT translate or vertically stretch an identical whole sprite. There must be distinct anatomical articulations. Body continues coherently below the shoulders behind the imaginary edge, not a detached head. No full standing legs.

Registration: same face size roughly 200px wide, head top local y=40..80 depending on raising, head/chin y=230..340. Torso and two bent forearms extend to y465. Elbow centers local x=100 and x=284, both touching local y465. Keep clothing, face scale, braid and jewel position coherent. All artwork remains inside its cell x16..368,y20..492. Keep 16px transparent gutters at LEFT/RIGHT so hair and ribbons cannot be clipped or invade adjacent cells. Do not crop head loops, sleeves or hair horizontally. Empty background alpha=0, solid character interiors alpha=255, clean cutout without fog or glow.

Eight poses:
0 top-left: normal open-eyed chin resting on both palms, gentle small smile, original neutral pose.
1: a small intermediate upward look, chin and palms lift slightly but elbows stay on y465, curious eyes.
2: head raised a bit more, eyes look upward with small curious mouth, forearms hinge naturally at the same fixed elbows.
3 top-right: head and gaze turned gently LEFT about 8 degrees, left cheek rests a little more heavily in palm, other palm remains against cheek, elbows fixed.
4 bottom-left: mirrored gentle RIGHT glance, both hands and elbows still supported, face/shoulder subtly turn rather than pupils only.
5: gentle LEFT head tilt about 6 degrees with cheerful CLOSED eyes and a sweet small smile, one cheek presses into palm, elbows fixed.
6: gentle RIGHT head tilt about 6 degrees, cheerful open green eyes and happy smile, arms articulate to support cheeks, elbows fixed.
7 bottom-right: small transitional return to straight head, relaxed half-lidded eyes with tiny smile, chin settling into palms, elbows fixed.

No new outfits, no extra hands, no distorted anatomy, no separate flying head, no background, no gradient backdrop, no haze, shadows, glow, cloud, throne, text, cell markers or watermark. Preserve genuine transparent RGBA. Eight safely separated coherent sprites of ONE same character, with natural head/neck/shoulder/forearm changes and fixed support contact.

## 第一轮修正

Use case: precise-object-edit for a transparent 8-frame Luo Tianyi animation atlas. Keep the SAME eight poses, faces and costumes, SAME 1536x1024 canvas and 4-column 2-row grid with each cell 384x512. Improve SPRITE ISOLATION, not create new actions.
CRITICAL: current sprites touch the left/right boundaries of their cells; a few stray hair/ribbon fragments spill into the next cell and appear as detached floating specks when cropped. Repair this cleanly. In EVERY cell uniformly shrink that entire sprite slightly around its center, to occupy ONLY local x=32..352 and y=32..480. Make all character hair, ribbons and sleeve edges intact within that box. There must be at least 24 completely transparent alpha-zero pixels of separation around each cell. REMOVE any stray disconnected fragments from neighboring characters. No clipping of hair loops, hair tips or sleeves at cell edges.
Set BOTH elbow / sleeve-bottom contact points to the SAME horizontal support line at local y=475 in EACH cell. Normal chin rest, lifted intermediate, raised head, left gaze, right gaze, left tilted closed-eye smile, right tilted open-eye smile, settling half-lidded return. Keep both elbows truly supported at the same height while head and neck change; adjust bent forearms naturally, no detached hands. Faces and heads retain the matched style and current expressions. No added legs or torso under the support line, no prop or table drawn.
Background and all gutters must have true alpha=0; character interiors alpha=255, no haze, glow, gradient backdrop, shadows, outlines, text or markers. Preserve exact 1536x1024 RGBA file size and 384x512 grid. Eight clean isolated coherent sprites, with safely fitted complete silhouettes and fixed elbow baseline.

## 最终图集留白修正

Edit target: the attached eight-pose Luo Tianyi chibi chin-rest sprite atlas. Keep the exact eight expressions, outfit, hands supporting the cheeks, two elbows with wide sleeves, anime illustration style, and transparent alpha. Recompose the sheet for slicing: exactly 4 columns and 2 rows in a 1536x1024 PNG. CRITICAL: zoom out each of the eight characters to ONLY 65 percent of its current scale within its own 384x512 cell, centered horizontally. Each silhouette must fit in a 240px-wide by 350px-tall rectangle, leaving at least 65px completely transparent gutters on BOTH left and right of every cell, and at least 65px empty above and below each character. Nothing crosses any cell boundary; no hair, ribbon, fragments or antialiasing outside the middle rectangle. Put BOTH sleeve elbow floors at the same cell-relative y=415 for all poses. Maintain coherent identical character proportions between the poses. The small central sprites and WIDE EMPTY transparent gutters are mandatory, even if this means reducing illustrated detail. No backdrop, no gradient, no checkerboard, no panels or borders, no labels. Keep all eight unique poses in the original reading order. This is a sprite-sheet layout repair, not a new scene. True transparent background.
