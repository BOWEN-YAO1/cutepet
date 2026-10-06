# 天依角色素材（0.48.0）

下侧动作共用既有 bottom-sequence-v6.png 的 (0,65,256,352) 睁眼托腮区域；闭眼只在运行时取同图集的 (512,422,256,352) 区域，再裁取两个眼部区域、柔化边缘并随头部运动。原PNG字节保持不变，没有补绘新姿势或导出逐帧图片。下侧人物与托腮双手连续变形，袖口支撑区域固定在工作区下沿；保留任务栏避让和独立额度窗口。

当前85种动作底图、2个额外闭眼视图（上、下侧）、28个动作、177次帧引用。新天依包需要0.48.0；bottom-inbetweens-v1.png 仅保留在源码历史，不编入当前程序或导出角色包。下侧底图与闭眼视图共用同一原图集文件。实现、验证及限制见 docs/native-bottom-animation.md；素材权利沿用下方记录。

# 天依角色素材（0.47.0）

top-rig-v1.png 与 top-eyelids-v1.png 原样复制自v0.46隔离验证的 top-registered-00.png、top-registered-15.png，为既有上侧睁眼与闭眼画面的256×352登记结果。PNG没有修改，也没有补绘新的姿势。人物底图固定，运行时仅裁取闭眼画面的两个眼部区域，使用柔和边缘与连续闭合参数；头部、发梢和小幅腿部运动由网格曲线驱动。座椅区域固定，花藤、绳饰和悬挂动作保持。

当前116种动作底图及1个额外闭眼图层、28个动作、244次帧引用。新包需要0.47.0。历史上侧图集保留在源码，不编入当前应用或导出角色包。来源与权利沿用下方记录；详见 docs/native-top-animation.md。

# 天依角色素材（0.46.0）

side-rig-v1.png 原样复制自v0.45隔离验证的 side-registered-143.png，为既有侧边深探出姿势登记后的288×384画面。本版未补绘或修改这张PNG。双手支点保持(48,260)，采用WPF单贴图网格连续变形，不新增序列图。原素材生成来源与角色权利沿用下方记录；历史侧边图集保留在源码，应用和角色导出仅使用当前引用素材。

天依当前179个不同绘图、28个动作、370次帧引用。新包需要0.46.0。眼睛、嘴巴等未拆层，表情保持基础贴图。详见 docs/native-side-animation.md。

# 天依角色素材（0.45.0）

本版沿用0.44.0的全部源图：144张探出、16张点头、16张轻摇，侧边共176张不同姿势。没有新增源PNG或把实时混合帧计为新绘图。应用内统一预览和桌宠的呈现，添加35毫秒局部头部/纹理稳定过渡，保持手掌固定，结束前恢复精确终点。源图、裁切登记、角色包格式和所有许可沿用以下记录。新播放行为需0.45.0；角色图片包仍兼容0.44.0。源码中的docs/side-playback-stability.md记录实现与验证。

# 天依角色素材（0.44.0）

本版使用内置 imagegen 新绘七张透明 4×4 图集（112 张不同姿势）：side-transition-1-v9.png 至 side-transition-6-v9.png，以及 side-transition-deep-v9.png。原样复制并保留 alpha，未用程序修改源 PNG。参考原探出 A/B 图集；末段追加图以实际 WPF 登记画面的深探出起止姿势为参考。原生成文件依次为 exec-4fb12300-3f66-42ff-9dc7-b677430b8d7f.png、exec-b1ebcab5-dab9-440f-900c-eb681644dec3.png、exec-25f4701c-bf3a-48fb-a19d-8865fe316738.png、exec-a9b9f458-8bed-4bb8-85e9-53346e86a645.png、exec-043a62a2-40d0-4a60-a80e-1a047ff80376.png、exec-130e0e31-7f03-4c0d-9799-c5109766d424.png、exec-edd8d11b-8ffd-4c43-9456-be5b47406b29.png。完整提示词随匹配源码的 docs/side-dense-prompts-v044.md 提供。

旧 32 张探出与新增 112 张交错成为 144 张实际探出图；保留点头和轻摇各 16 张，侧边共 176 张不同绘图。按实际可见虹膜距双手的距离排序；缩回严格逆序，同一张图的重复播放及混合不计新增绘图。头部目标高 198 像素、固定画布 288×384，双手平均扶边点 (48,260)。探出图每张 10 毫秒，点头/轻摇图每张 40 毫秒，屏幕刷新按实际时间采样。探出往返 2.87 秒，缩回再探出 4.31 秒，点头和轻摇各 4.11 秒。PNG 不处理，登记与排序仅写入配置。

全角色 354 个不同绘图、28 个动作、1,720 次帧引用、26 张活跃 PNG。新包需应用 0.44.0；旧包继续兼容。源绘图与登记画布保守共享预算上限为 384 MiB，配置上限 1 MiB、引用上限 2,048；单张 PNG 8 MiB / 2048 边长、单动作 30 秒、ZIP / 解压后 64 MiB 与说明 64 KiB 保持。素材预算不代表程序全部内存。来源权利与同人说明沿用以下历史；代码 GPL 不授予原角色权利。

# 天依角色素材（0.43.0）

侧边采用四张透明4×4图集共64张实际绘图：side-reveal-a-v7.png、side-reveal-b-v8.png、side-nod-v7.png、side-sway-v7.png。两套探出图按实际露出程度交错为32级；点头和轻摇各16张独立绘图，在匹配探出阶段插入后继续探出。回收使用原序列倒放，所有回应起止共用待机图，重复引用与混合不计新增绘图。

使用内置imagegen，参考本项目side-sequence-v6.png。原文件exec-b95c8e02-a84a-40a6-89ce-e17955c2f554.png（探出A）、exec-8cec4d01-bae0-4bde-b50c-81be729affb0.png（探出B）、exec-0ae2e838-4de9-410c-9d43-45129abc88b8.png（点头）、exec-47d8aacf-3e98-4d43-ac2a-bc396a7a293f.png（轻摇）原样复制，alpha保留。初始探出B末格有额外腰侧手臂，经内置imagegen修正为exec-bed7ea6b-4601-4d58-bc45-cf15c178fc9c.png，最终采用side-reveal-b-v8.png；旧v7仅留源码草稿。未下载新的立绘。完整提示词、实际排序、生成结果与目标的差异见docs/side-drawn-transitions.md。

目标头高200、288×384画布、双手平均扶边点(48,260)，额头玉花配准。脚本只读像素并写配置，PNG不修改。运动绘图40毫秒；局部头部位移在上方手掌前归零，保留两只手扶边。上侧64张绘图保持。

全角色242个活跃绘图、28个动作、712次帧引用、19个活跃PNG。图集、不同区域与登记画布保守合计67,403,375像素/257.12 MiB，统一上限288 MiB。配置约364,002字节，上限512 KiB，新包需应用0.43.0，旧包继续兼容。旧侧边v6退出活跃资源，历史仍随匹配源码提供。

原角色权利及同人说明沿用下述历史，代码GPL不授予角色权利。上一版本来源完整保存在SOURCE-HISTORY-v042.md；以下历史计数对应各自版本。

# 天依角色素材（0.42.0）

上侧改为四张透明4×4图集，共64张实际绘图：top-blink-v6.png、top-left-v6.png、top-right-v6.png、top-smile-v6.png。内置imagegen参考已有中性坐姿的实际登记画面生成，原PNG原样复制，透明alpha保留；每组16张按实际姿势顺序接入，重复引用、倒放和混合不计为新绘图。

原图对应：exec-73753084-3068-41ee-a60e-625d7083531d.png（闭眼）、exec-9ac2fd44-a78f-42ed-ae88-32826f658329.png（左转）、exec-909be7ff-2dfe-4334-a528-2fe562b74a67.png（右转）、exec-04d6711e-275a-4803-b6fd-a6f988f88397.png（微笑）。未采用的48格初稿实际生成50格，未接入。完整提示词、原图路径与筛选见仓库docs/top-drawn-transitions.md。

统一登记头高153、256×352画布、座椅Y=242.241、悬挂Y=0，额头玉花配准。只写配置，不修改图像。运动绘图40毫秒，回收使用同组原路倒放，所有回应从同一待机姿势出发并返回。原绘图预览不经过混合，可独立检查实际中间姿势。

全角色193个活跃绘图、28个动作、506次帧引用；16张活跃PNG及不同区域/画布保守合计53,952,042像素，约205.81 MiB。共享上限224 MiB，新包需应用0.42.0。旧上侧v5退出活跃运行包，历史仍随匹配源码提供。原角色权利及同人说明沿用下述记录，代码GPL不授予原角色权利。

上一版来源完整保存于SOURCE-HISTORY-v041.md。以下是历史记录，其中计数与运行行为对应各自版本。

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
