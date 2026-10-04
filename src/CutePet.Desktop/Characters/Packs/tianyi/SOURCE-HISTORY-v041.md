# 天依角色素材（0.41.0）

上侧秋千使用 top-sequence-v5.png，内置 imagegen 参考已有 top-sequence-v4.png 生成。原文件 exec-ccc0dafa-e1c5-498d-b778-5531ee5f2402.png 原样复制，透明 alpha 保留，接入 16 个姿势；第一行第 2 格实际是开口笑，用于微笑回应而不用于闭眼过程。

登记头高目标 153 像素、画布 256×352、座椅 Y=242、悬挂 Y=0。额头玉花作为头部配准点，原 PNG 不修改；两张歪头帧按真实 WPF 绘制进行轻微比例校准。smoothFrames 过渡在座椅前衰减，保留固定的座椅和绳子连接。运动时间单位 40 毫秒，目标 25 帧/秒，停留重复使用原绘图。完整提示词、运行限制和验证见仓库 docs/top-smooth.md。

当前共 145 个活跃绘图、28 个动作、439 次帧引用。左右修复继续保留，旧上侧原图与补帧退出活跃运行包，只在匹配源码中留档。上一版本来源完整保存于 SOURCE-HISTORY-v040.md。下面内容为历史记录，计数对应其各自版本。

# 天依角色素材（0.40.0）

左右贴边改用 side-sequence-v6.png。内置 imagegen 参考本包 idle.png 生成；原文件 exec-8372b239-e561-4110-8d02-452408bb15c6.png 原样复制，透明 alpha 保留。16 格中选用前 15 格，最后一格松手并改变服饰，没有接入。

登记头部高度为 200 像素、画布 288×384、双手支撑点 (48,250)，仅修改配置，不修改原 PNG。运动帧 40 毫秒，smoothFrames 在相邻帧间做预乘透明短过渡；不将像素混合算作额外绘图。完整提示词、未选用草稿和限制见仓库 docs/side-smooth.md。登记使用 tools/register-tianyi-sequences.py，开发时依赖 Pillow，桌宠不依赖 Python。

当前全角色 161 个活跃姿势。其他图片来源保持不变，以下保留 0.39.0 生成档案，完整历史另见 SOURCE-HISTORY-v039.md；其中旧左右图集已退出活跃动作，只保留在源码中。

## 0.39.0 历史来源记录

本版是非官方 AI 同人素材，原角色权利归相关权利人；代码的 GPL v3 不授予原角色权利。原形象来源与权利说明见仓库 THIRD_PARTY_NOTICES.md。参考沿用本项目已有 AI 图，未下载或传入新的官方立绘。

保留 0.38.0 的 96 个姿势，新增六张透明图集，从中接入 74 个区域，总计 170 个姿势、28 个动作、445 次帧引用。内置小猫的四组动作保持。原始 PNG 原样复制，运行时先 region 取帧，再按可选 canvas 在 WPF 对齐尺寸、脚底和支撑点；同区域及登记画布跨动作共用冻结缓存。登记工具只读像素和写配置，不编辑原图。

旧素材完整来源与生成提示词保留在匹配源码的 SOURCE-HISTORY-v038.md、SOURCE-HISTORY-v037.md 及更早档案。角色 ZIP 仅含当前引用图片、此说明和许可。完整补帧提示词附下，同时见 docs/inbetween-prompts.md。

AI 帧间的发丝、衣纹、椅饰、表情仍可能变化；素材哈希不同及程序检查通过不能证明达到逐帧手绘的视觉连续性。

# 天依中间帧生成记录（0.39.0）

日期：2026-10-04。使用内置 image_gen.imagegen，全部 transparent_background=true，无 CLI/API key 回退。六张原始 PNG 原样复制入角色包，各有 16 个图格，从中接入 74 个不同区域；另 22 个图格没有引用。配置中的 canvas 在 WPF 加载时统一大小与落点，不改写源 PNG。生成提示词指定的精确表情、曝光比例不等于生成结果已全部满足，接入以实际图片及保存配置为准。

左右两个参考由已存在 edge-sequence-v5.png 的浅探出 / 露肩区域通过 WPF CroppedBitmap 导出，保留 alpha，供生成工具参考；不是最终分发素材。选出的 8 帧插在原第 2、3 姿势之间，回收逆序使用。其他组针对抬手、表情、转头、坐下与随风的相邻姿势插入。

## standing-inbetweens-v1.png

输出：exec-6d31e459-c89e-4b35-a8d5-bfb07d311b8c.png；参考：standing-sequence-v4.png；16 图格，接入 12 个区域。

```text
Use case: identity-preserve. Asset type: transparent animation in-between atlas for an existing WPF desktop pet.
Input image is the exact original standing keyframe sheet, 4x4, numbered 0 to15 row-major. Preserve this exact Luo Tianyi chibi design, pale silver-lavender hair double loop buns, green eyes, blue-white embroidered short dress, jade pendants, blue boots. Same face, anatomy, head size, camera, scale, lighting and clothing details. Do NOT redesign or add accessories.
Generate a NEW 1024x1536 transparent RGBA sprite sheet, EXACTLY 4 columns x4 rows, one intermediate drawing in each cell. Every drawing is the halfway pose between the two specified original keyframes, NOT a copy of either endpoint. This is animation inbetweening, make only the small required pose/eyelid/hand changes. Registered cell size256x384. Character silhouette stays inside cell x20..236 y44..344, soles at344, centered x128, head tops around55. Fully transparent background, generous empty gutters, no text/grid/borders/shadows.
16 cells row-major are halfway between original indices:
row1: 0->5 (hand between lowered and chest-height); 4->5; 5->6 (hand rising to cheek); 6->7 (small wrist rotation).
row2: 0->8 (both hands halfway raising towards chest); 8->9 (small mouth/hand change); 9->10 (eyes halfway closing into happy); 10->11 (eyes halfway reopening).
row3: 0->12 (head halfway turning left); 12->13 (tiny further left turn); 0->14 (head halfway turning right); 14->15 (tiny further right turn).
row4: 0->1 (quarter-closed eyelids); 1->2 (three-quarter-closed eyelids); 0->3 (subtle breathing midpoint); 0->4 (neutral before waving).
Hands and face move incrementally, torso/head/boots remain registered. No ghosting/dissolves/double limbs. Paint true middle poses, no duplicate sprites. Match reference linework and facial identity closely.
```

## throne-inbetweens-v1.png

输出：exec-3222c914-7f11-48c0-9692-b585051f417b.png；参考：throne-sequence-v4.png；16 图格，接入 12 个区域。

```text
Use case: identity-preserve. Produce an animation in-between sprite atlas for the exact attached Luo Tianyi throne sheet. Original attached image contains sixteen keyframes numbered 0..15 row-major in a4x4 grid. Preserve exact character face, silver lilac loop-bun hair, jade accessories, blue-white dress and boots, original jade-gold throne geometry. New 1024x1536 PNG with genuine transparent alpha,4columns4rows, no background/text/shadows/grid. One real painted midpoint in each cell, not duplicated endpoints, not a new redesign.
Each cell256x384. All sprites fit x20..236 y55..350 relative to cell; head top70 and boot bottom344 matching original scale. Throne identical size and registration in every seated frame. Large transparent gutters. Exactly one connected character, two arms/two legs, natural anatomy.
Midpoints in row-major order:
row1: original0->1 (hand halfway raising, faint throne outline);1->2 (hand small wrist turn, slightly stronger magic outline);2->3 (outline becoming translucent gold-jade solid, hand beginning to lower, knees very slightly bending, body still mostly standing);3->4 (knees bending a little farther, hip halfway lowering, hands halfway descending).
row2:4->5 (hip halfway towards seat, knees progressively bending);5->6 (settling lower into seat);6->7 (hands halfway between armrests and lap);7->8 (hands nearly joined on lap).
row3:8->9 (quarter-closed lids);9->10 (three-quarter closed lids);10->11 (eyes reopening halfway);11->8 (neutral settled posture, small mouth relaxation).
row4:8->12 (hand halfway lifting from lap to chest);12->13 (hand rising halfway to cheek);13->14 (small wrist rotation);14->15 (other direction halfway wrist rotation).
Prioritize body pose continuity. Keep face/torso size fixed, boots fully visible and character silhouette same as source. No new character, no labels. This is insertion frames between existing animation images.
```

## cloud-inbetweens-v1.png

输出：exec-f0c3c3cd-c6ce-4f2a-a362-7d46a0ff4d21.png；参考：cloud-sequence-v5.png；16 图格，接入 10 个区域。

```text
Use case: identity-preserve. NEW transparent4x4 sprite sheet1024x1536 for this EXACT chibi Luo Tianyi. Attached existing cloud sheet is character/clothing/scale reference. No background/grid/text/shadow or actual cloud platform. Fixedcamera, identical face/body scale and hairdesign, soles localy344, silhouettex24..232 y70..344, oneframe per256x384cell, generouscleartransparentgutters.
We are drawing subtle INBETWEEN poses, not original endpoints.
row1: neutral eyes25%closed armsdown; neutraleyes75%closed armsdown; headhalfwaylowered tired armsdown; headhalfwayrecovering armsdown.
row2 four continuous wind poses, eyes OPEN and closed mouth, arms relaxed down: hair tips and skirt halfway movingLEFT; hair returning halfway towardCENTER; hair tips halfway movingRIGHT; hair returning halfway towardCENTER. Tiny fabric/strand changes ONLY, headtorso/bootsfixed, no duplicatedstills, no hugewind orhairoutsidecell.
row3: hand halfraisedfromwaisttochest eyesOPEN; handat chest eyes25%closed; handat chest eyes75%closed; handhalfreturningfromchesttowais eyesOPEN.
row4: onehandhalfwayfromneutral tospell atchest, NOglow; handhalfwayfromchesttoshoulder withoneSMALLfaintbluebutterfly; handslightlyloweringfromshoulder tochest withbutterflyfade; handloweringtowais glowgone.
Keep precise continuity and original silhouette proportions. All16are small incremental motions and mustcontaindifferent paintedpixels. Preserve blueandwhite dress embroidery jade accessories and silver hair as reference.
```

## side-inbetweens-v1.png

输出：exec-d4868045-c0d2-4d69-a9e3-bd380088fc4f.png；参考：side-shallow.png + side-shoulder.png；16 图格，接入 8 个区域。

```text
Use case: identity-preserve. Draw a NEW in-between animation sheet connecting the TWO attached endpoint sprites. Image1 is START tiny peek, image2 is END shoulder peek. Same character, exact same chibi linework/face/costume, same scale. Do not copy the endpoint sheet composition; each reference has only ONE character.
1024x1536 transparent RGBA PNG,4columns4rows,16 successive INBETWEEN frames row-major. Each cell256x384. Every frame strictly between START and END, character's head/torso progressively turns and leans into view by a tiny increment. Cell0 only slightly further out thanSTART, cell15 nearlyEND. Both gripping hands stay at x58,y230 and265, invisible boundary cuts atx58. Use full cellheight352 startinglocaly16, actualheadtopsaround30, maxbodybottom350. Keep cleartransparent32pxvertical gutters andwidth256.
Progression MUST be gradual through all16cells: 30%,33%,36%,39%,42%,45%,48%,51%,54%,57%,60%,63%,66%,69%,72%,75% of face visible, shoulder width progressively0,2,4,6,8,10,12,14,16,18,20,22,24,26,28,30px. One eye visible initially, second eye peeks after cell7 then gradually becomes visible. Torso/neck and shoulder are CONNECTED. Hair rotates progressively with head, same head size and details. Mouthclosed neutral gentle smile inALLcells. No openmouth, no fullyclosedeyes, no waving, no thigh/newlegs. NObackground/shadow/grid/labels/borders. All16MUSTdiffer in how muchface/shoulder isvisible, NOT byrandommouth/eyes. True interpolation of pose and occlusion, never duplicate firstframes.
```

## bottom-inbetweens-v1.png

输出：exec-1e766bcf-e41b-43cc-b4f5-9d462a2a4dd1.png；参考：bottom-sequence-v6.png；16 图格，接入 16 个区域。

```text
Use case: identity-preserve. NEW sixteen insertion drawings for existing Luo Tianyi chin-rest bottom-edge animation. Reference is exactcharacter/style/costume.1024x1536 genuine transparent RGBA,4x4 isolated sprites,256x384cells,no background/text/grid/border/shadows. Fix face/head/torso scale, both hands cupping cheeks and both blue-white embroidered sleeves supported at SAMElocaly344, x60 and196. Drawconnected upperbody, nofloatinghead/legs. Entire spritewithin localx25..230 y80..344, generousgutters.
ALL16are small midpoint poses BETWEEN existingkeyposes, not simplyreferencecopies:
row1: neutralto slightlylookingup (headrise2px); neutralto lookingup (headrise4px); neutralto smallsmile (mouthbarelyopen); quarterclosedeyes smallsmile.
row2: eyes25%closed neutralmouth; eyes75%closed neutralmouth; eyes75%closedsoftsmile; eyes25%closedsoftsmile.
row3: headlefttiltONLY3degrees; headlefttiltONLY6degrees; headrighttiltONLY3degrees; headrighttiltONLY6degrees.
row4: chinhalfwayleanleft smallsmile; chinhalfwayleanright smallsmile; headnearlystraight afterlefttilt smallsmile; headnearlystraight afterrighttilt smallsmile.
Both forearms remainanatomicallyconnectedand elbows DONOTlift or moveoffsupportline. Source designfixed and eye/mouth/neck shiftsincrementally. Avoidfullclosedeyes and dramaticposevariation. Same original hairbunsribbonsfacejewelry; match drawn style faithfully.
```

## top-inbetweens-v1.png

输出：exec-c0b798d8-f75d-46b8-bc03-f428c644aa7d.png；参考：top-sequence-v4.png；16 图格，接入 16 个区域。

```text
Use case: identity-preserve. NEW transparentRGBA1024x1536 sprite sheet EXACTLY4x4,16 delicate insertionposes for the attached fairy jade-swing sitting Luo Tianyi. Preserve same chibi identity, silver loop buns, green eyes, blue-white dress, boots and exact green jade curved seat with golden rings and tassels. Do NOT draw ropes/scenery/backdrop/text/grid/shadow. Both hands resting onlap throughout, legs together feetfullyvisible. Camera, body size, headsize and seatregistration FIXED acrosscells, onlytinyeyelid/headchanges. Each256x384cell silhouettex20..236 y70..345, goldenropeattachment rings atlocalx34/222 y270, generous transparentgutter.
row1: tinyneutral breathing midpoint; neutraleyes25%closed; neutraleyes75%closed; neutraleyes25%open withsmallsmile.
row2: smileeyes25%closed; smileeyes75%closed; smileeyes25%open; smilehalfrelaxingto neutralmouth.
row3: headturnedLEFTonly3degrees; headturnedLEFTonly6degrees; headturnedRIGHTonly3degrees; headturnedRIGHTonly6degrees.
row4: headtiltedLEFTonly3degrees smallsmile; headtiltedRIGHTonly3degrees smallsmile; headhalfreturningfromLEFTtilt smallsmile; headhalfreturningfromRIGHTtilt smallsmile.
These are intermediateanimationframes to insertbetweenolderstillimages, not justendpoints. All16mustcontainnewpaintedpixels, no duplicatedstills. Same originalseat ornaments and dress details, no character redesign. Small mouth/smallfacialmovement instead of largeexpressionleaps.
```

## 未采用尝试

- exec-b8651864-e3e9-454b-987a-fec205fa0080.png：整张侧边图集参考导致前几个姿势仍过于接近，未接入。
- exec-c11ea086-66ef-45ec-bb98-ec644650e626.png：改用早期单张立绘，画风、尺寸和透明留白不适合混入现有图集，未接入。
- exec-61629754-f636-44e6-9eb2-dd8595fc2ff8.png：站立细化尝试改到了另一侧手臂，不适合原挥手顺序，未接入。

这些原始输出留在工具生成目录，不进入运行包。图格登记及相邻帧映射见 tools/register-tianyi-inbetweens.py；使用 --check 可检查保存配置，维护者复现依赖 Pillow，但构建、运行、分发打包不需要。
