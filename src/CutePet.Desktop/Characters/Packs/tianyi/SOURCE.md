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

## 历史素材与角色权利

本图集和此前所有素材均为非官方 AI 同人表现，角色权利仍属于各自权利人。代码 GPL v3 不授予原角色权利，完整说明见 LICENSE.txt 与仓库 THIRD_PARTY_NOTICES.md。

0.33 及此前的逐次来源和完整提示词保留在源码的 SOURCE-HISTORY-v033.md，也可查阅 https://github.com/BOWEN-YAO1/cutepet/blob/38ddee355da564068fe1501198bb199e3230d835/src/CutePet.Desktop/Characters/Packs/tianyi/SOURCE.md 。历史提示词文件包括 PROMPTS.md、THRONE-PROMPTS.md、CLOUD-PROMPTS.md、REST-SMOOTH-PROMPTS.md、SEATED-PROMPTS.md、EDGE-PROMPTS.md、VERTICAL-EDGE-PROMPTS.md、TOP-EDGE-PROMPTS.md、SWING-PROMPTS.md、JADE-SWING-PROMPTS.md、ORNAMENT-PROMPTS.md、SCENERY-PROMPTS.md 和 FULL-SIDE-PROMPTS.md，均随匹配源码 ZIP 分发。既有站姿、挥手、王座、坐姿、云层、秋千、挂饰与配景继续沿用；未重新生成。原生花藤与彩花由 WPF 几何绘制。

为继续保留角色包说明文件的 64 KiB 上限，导出 SOURCE.md 汇总原角色来源、历史来源链接和当前两份图集完整提示词；历史详细记录保留在源码归档，不提高导入大小上限。

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

# 秋千坐姿动作提示词（0.35.0）

工具：内置 image_gen.imagegen，transparent_background=true。参考现有 swing-jade-v2.png 补绘八种完整坐姿，再修整留白；最终 top-sequence-v2.png 为 1536×1024 RGBA 原始输出，原样复制至角色包，运行时按四列两行、384×512 区域取帧。双手仍在膝上，完整裙摆、腿、鞋和青玉坐板保留；改变眼神、表情和头部姿态。三组回应共用八张姿势，有限帧并非骨骼动画，少量绘制细节会变化。

## 生成记录

- v1 草稿：exec-9be90f0c-5df2-4498-9ba8-794a67ec9df2.png
- v2 接入：exec-8b406ff0-5a27-4268-b48e-28282b05a9b9.png

姿势序号 0–7 分别为基础微笑、半闭眼、闭眼微笑、左看、右看、左倾闭眼笑、右倾睁眼笑、半闭眼回落。基础帧 edgeTopAnchorY=40/512，topSwing.seatAnchorY=355/512，seatHalfWidth=0.33；逐帧 swingSeatAnchorY 对齐原图坐板玉环高度，上排 355/512，下排 323/512 或 324/512，逐帧 edgeAnchorY 补偿差值。坐板距虚拟悬挂点始终为 315/512，固定绳顶，绳底跟随真实变换后的坐板。绘制位移只注册素材，整体摆动沿用秋千。

原图、旧秋千及 v1 草稿留在源码；角色包仅导出实际引用图集。原角色权利说明沿用 SOURCE.md / LICENSE.txt；历史完整来源档案 SOURCE-HISTORY-v033.md 在匹配源码包保留。

## 初始提示词

Edit target / character and swing style reference: attached Luo Tianyi chibi girl seated on a pale jade swing with gold borders and two jade ring tassels. Make an animation sprite atlas, not a scene: 1536x1024 transparent RGBA PNG with exactly 4 columns and 2 rows, eight equal384x512 cells. Each cell contains the SAME full seated character including full knees, legs, shoes, same pale jade horizontal swing seat, same jade rings and short tassels. Keep identical dress, silver-gray hair, green eyes, blue ribbons, twin braids, all consistent proportions and illustration rendering. No ropes, vines, flowers, scenery, labels, borders, background, shadows or floor. CRITICAL sprite layout: every silhouette fits entirely within cell-local x=45..339, y=64..480; leave generous transparent gutters between all sprites, NO fragments crossing cells. Identical scale and seat placement for all eight poses. Swing top edge is cell y=340 with rope attachment points x=68 and x=316. Seat center x=192. Hands stay together on lap in every frame. Lower body, knees, shoes, braids below shoulder and jade seat should be visually identical across all frames; animate only head, gaze and expression with tiny upper-body changes. Reading order: 0 relaxed seated neutral open-eyed smile; 1 slight lowered eyelids beginning a blink with neutral head; 2 closed eyes peaceful smile neutral head; 3 open-eyed looking gently to viewer's left, head slight left; 4 open-eyed looking gently to viewer's right, head slight right; 5 modest head tilt left with warm closed-eye smile; 6 modest head tilt right with warm open-eyed smile; 7 half-lidded soft returning smile, neutral head. Natural neck and torso connections, head tilt only 5 degrees maximum. The seat must never tilt or move within any cell; original reference seat stays same in all frames. Preserve true transparency. These eight poses will play sequentially on a desktop swing, so consistent anatomy and registration matter more than extra detail.

## 留白修正提示词

Edit target: this eight-pose transparent seated-swing sprite atlas. Preserve all eight poses, character identity, anatomy, lap hands, costume, jade seats and gold trim, and exact 1536x1024 grid (4 columns, 2 rows, each384x512 cell). Layout repair ONLY: reduce each COMPLETE character plus jade seat and tassels uniformly to 80% of its present scale, keep centered at cell x192, and translate within each cell so seat rope connection rings have centers at cell y340. Every sprite must now have at least60px completely transparent padding LEFT and RIGHT, at least75px empty above the highest hair, and shoes end by y470 with empty space beneath. No pixel, strand or fragment from another cell. Keep seats pixel-consistent in location, width and shape across the eight frames; exactly same horizontal seat top and same two ring connection positions. Do not crop anything. Keep expressions distinct in the same order. No ropes, background, gradients or checkerboard. True transparent RGBA.
