# 侧边绘图过渡加密（0.43.0）

原来四种侧边回应共用15张探出绘图，轻摇和点头主要通过相同画面的往返来表示。0.43.0采用64张实际源绘图：32张探出过程、16张点头过程、16张轻摇过程。左右两侧共用同一套绘图并由窗口镜像，图片混合、逆序和重复引用不计为新增绘图。上侧64张补绘保持。

| 图集 | 绘图数量 | 实际采用方式 |
| --- | ---: | --- |
| side-reveal-a-v7.png | 16 | 与第二套按实际露出程度交错排序 |
| side-reveal-b-v8.png | 16 | 与第一套合成32级探出，原路回收 |
| side-nod-v7.png | 16 | 单独点头过程与逆序抬头 |
| side-sway-v7.png | 16 | 单独轻微歪头过程与逆序回正 |

每张 PNG 为1024×1536、4列×4行，透明alpha保留，原图不修改。生成目标要求两套分别覆盖浅探与深探，但实际有重叠；未直接串接导致突然退回。登记脚本读取实际右侧虹膜与双手位置，以眼睛相对扶边点的距离排序32张。相邻测量距离增量受头高8%约束，不能只凭文件名推断连续性。

点头与轻摇的实际探出深度较浅，分别插入最接近的探出阶段，完成后继续探出、再沿原路回收，避免最深探出直接切入较浅姿势。生成中的点头伴随轻微眼睑变化，以实际画面为准，不宣称严格达到提示词中的角度。

## 尺寸、扶边与时长

全64张目标头高200像素、登记画布288×384、双手平均扶边点(48,260)。测量两只手的实际肤色区域与额头玉花；额头标记在点头时仍可见。仅写region/canvas/锚点，原PNG不处理。

已有帧间短过渡继续使用。侧边双手锚点是两只手的中点，头部位移在上方10%画布高度之前衰减至零，避免上方手掌被头部配准带动。上侧仍按秋千座椅固定。测试使用实际上、下两只手标记检查此行为。

运动源绘图每张40毫秒，目标每秒25个时间段。探出63次引用/2520毫秒，缩回再探出95次/3800毫秒，点头与轻摇各94次/3760毫秒。每组起止共用同一张浅探待机图；回收与缩回直接使用同组倒放。静止待机不反复换图。

全角色242个不同绘图、28个动作、712次帧引用、19张活跃PNG。原图、不同区域、登记画布保守累计67,403,375像素，257.12 MiB BGRA。新增实际绘图超出原224 MiB预算，统一上限改为75,497,472像素/288 MiB。配置364,002字节，上限改为512 KiB；768次引用、单张8 MiB/2048边长、ZIP及解压64 MiB、单动作30秒仍保持。预算不是应用全部内存。新版角色包需要0.43.0；旧包继续兼容。

## 验证和体验

验证实际加载64张绘图、32张探出严格原路倒放、点头及轻摇各16张独立姿势、固定起止图、冻结缓存、不同像素哈希、实际头高差、两只手的可见像素、左右镜像、快速点击、菜单暂停、屏幕区域变化、低额度退出以及导入导出。共享素材预算与配置字节数边界仍有超限检查，使用当前限制构造输入。

- side-drawn-keyframes.gif直接播放登记绘图，不经过混合或窗口位移。
- side-edge-actions.gif按40毫秒采样实际 WPF 窗口中的左右四种动作，检查本身仍每20毫秒推进。
- side-edge-stages.png展示逐渐露出头、肩、腰和裙摆的窗口画面。

预览使用演示额度，不连接账号；不等同实际屏幕录像或多屏长期验收。更多绘图不能完全消除生成素材的衣纹和发丝差异。选择内置天依体验，旧导入包保留原动作；可另行导入新版包。

最终素材通过14项核心协议检查与2,127项隔离窗口检查。实际64张登记画面的头高为199–204像素，两只扶边手掌在所有姿势中均有可见像素；原绘图与窗口GIF均完整覆盖四种侧边回应。

## 生成来源

使用内置imagegen，参考本项目side-sequence-v6.png的身份、服装与扶边姿势；没有下载新的外部立绘。生成文件原样复制进src/CutePet.Desktop/Characters/Packs/tianyi/。

| 原文件 | 仓库文件 |
| --- | --- |
| exec-b95c8e02-a84a-40a6-89ce-e17955c2f554.png | side-reveal-a-v7.png |
| exec-bed7ea6b-4601-4d58-bc45-cf15c178fc9c.png | side-reveal-b-v8.png |
| exec-0ae2e838-4de9-410c-9d43-45129abc88b8.png | side-nod-v7.png |
| exec-47d8aacf-3e98-4d43-ac2a-bc396a7a293f.png | side-sway-v7.png |

原图在本机.codex/generated_images/01a0f608-7491-76a1-9748-25b0b48972f3/。实际排列与来源分别记录于tools/register-tianyi-side.py和本包SOURCE.md；旧记录完整保存在SOURCE-HISTORY-v042.md及匹配源码ZIP。原角色权利及同人说明保持，代码GPL不授予原角色权利。

初始第二套图exec-8cec4d01-bae0-4bde-b50c-81be729affb0.png保存在side-reveal-b-v7.png草稿中：末格出现额外腰侧手臂，未用于最终运行包。内置imagegen修正后生成exec-bed7ea6b-4601-4d58-bc45-cf15c178fc9c.png，采用修正后的16格v8。修正格的实际深度也重新测量排序，未强制放在末尾。

当前Windows工作文件364,002字节；LF规范化配置350,451字节，均在512 KiB限制内。

## 完整提示词

### reveal-a

```text
Create ONE production sprite atlas on true TRANSPARENT alpha background, exactly 1024x1536 pixels, EXACTLY FOUR columns and FOUR rows, sixteen cells256x384 read row-major. Reference image is the existing chibi character identity, outfit, watercolor-anime style, and shallow/full border-gripping poses; ignore the old atlas arrangement. Silver-gray loop-bun hair, forehead jade flower/navy bows, green eyes, white/navy Chinese-inspired dress. This must be sixteen NEW painted intermediate poses of ONE SINGLE small continuous action, NOT a collection of expressions. Fixed orthographic camera and CONSTANT character HEAD SCALE and costume proportions in ALL cells: head height including bun to face chin about190px, head widthabout174px when fully seen, the hidden half is merely occluded by the invisible border, never shrunk. Invisible vertical screen boundary localx42. Two small hands ALWAYS grip this exact boundary, upper hand center(42,235), lower hand center(42,276), fingers complete and NEVER released, clothing sleeves attached. Parts behind border are hidden, so ALL visible art including hands lies localx28..246,y14..370, generous transparent cell gutters, no spill into neighboring cells. No actual drawn border or vertical stripe. Head aroundy20..210, hands/torso stay fixed with connected shoulder, waist and dress continuing down tolocaly366, no floating head or disconnected legs. All eyes open, gentle CLOSED-MOUTH smile, no blinking, mouth opening, new props, magic or expression changes. Hair and cloth have tiny consistent follow-through without detail changes. Accurate fixed grip points and stable head proportions are paramount. No numbers, text, grid, background, glow haze, clouds, chair or ropes.
ONLY FIRST HALF of a gradual lean-out from the LEFT edge toward the RIGHT. Sixteen tiny stages of ONE connected torso leaning movement: stage0 very shallow peek, only one green eye and one cheek visible beyond the boundary, most face/shoulder/waist still hidden, as in reference upper-left pose. Stage15 EXACTLY half-way leaned out, second eye just becomes visible, part of shoulder visible, waist still mostly behind border. Stages1..14 linearly interpolate body lean in tiny even steps between these endpoints, head retains identical height. DO NOT reach the fully exposed face or torso in this sheet. Across row1 reveal0..10%, row2~13..23%, row3~27..37%, row4~40..50%. Incrementally articulate shoulder/neck/waist around the FIXED two gripping hands, not moving whole sprite. The lower dress progressively follows naturally, every adjacent pose just a tiny change. The first four poses must visibly differ in occlusion rather than being duplicate stills. Preserve neckline, bun, ribbons and sleeves.
```

### reveal-b

```text
Create ONE production sprite atlas on true TRANSPARENT alpha background, exactly 1024x1536 pixels, EXACTLY FOUR columns and FOUR rows, sixteen cells256x384 read row-major. Reference image is the existing chibi character identity, outfit, watercolor-anime style, and shallow/full border-gripping poses; ignore the old atlas arrangement. Silver-gray loop-bun hair, forehead jade flower/navy bows, green eyes, white/navy Chinese-inspired dress. This must be sixteen NEW painted intermediate poses of ONE SINGLE small continuous action, NOT a collection of expressions. Fixed orthographic camera and CONSTANT character HEAD SCALE and costume proportions in ALL cells: head height including bun to face chin about190px, head widthabout174px when fully seen, the hidden half is merely occluded by the invisible border, never shrunk. Invisible vertical screen boundary localx42. Two small hands ALWAYS grip this exact boundary, upper hand center(42,235), lower hand center(42,276), fingers complete and NEVER released, clothing sleeves attached. Parts behind border are hidden, so ALL visible art including hands lies localx28..246,y14..370, generous transparent cell gutters, no spill into neighboring cells. No actual drawn border or vertical stripe. Head aroundy20..210, hands/torso stay fixed with connected shoulder, waist and dress continuing down tolocaly366, no floating head or disconnected legs. All eyes open, gentle CLOSED-MOUTH smile, no blinking, mouth opening, new props, magic or expression changes. Hair and cloth have tiny consistent follow-through without detail changes. Accurate fixed grip points and stable head proportions are paramount. No numbers, text, grid, background, glow haze, clouds, chair or ropes.
ONLY SECOND HALF of a gradual lean-out from the LEFT edge toward the RIGHT. Sixteen tiny stages of ONE connected torso leaning movement: stage0 EXACTLY half-way leaned out, second eye just becomes visible, part of shoulder visible, waist still mostly behind the border. Stage15 deepest leaned-out peek, both eyes and complete face visible, connected shoulder/chest/waist and part of skirt out to the right, two palms still gripping boundaryx42. Stages1..14 evenly interpolate from HALF to FULL exposure; row1 reveal50..60%, row2~63..73%, row3~77..87%, row4~90..100%. Same head size as reference, NEVER enlarging head to fill cell; only occlusion and torso articulation change. Do not restart from the shallow one-eye pose. Gradually angle connected torso/shoulder out around anchored hands, not sliding the full sprite. Hand points remain exact and neck remains connected throughout. The endpoint is like reference bottom-row third character, not bottom-right released-hand pose.
```

### nod

```text
Create ONE production sprite atlas on true TRANSPARENT alpha background, exactly 1024x1536 pixels, EXACTLY FOUR columns and FOUR rows, sixteen cells256x384 read row-major. Reference image is the existing chibi character identity, outfit, watercolor-anime style, and shallow/full border-gripping poses; ignore the old atlas arrangement. Silver-gray loop-bun hair, forehead jade flower/navy bows, green eyes, white/navy Chinese-inspired dress. This must be sixteen NEW painted intermediate poses of ONE SINGLE small continuous action, NOT a collection of expressions. Fixed orthographic camera and CONSTANT character HEAD SCALE and costume proportions in ALL cells: head height including bun to face chin about190px, head widthabout174px when fully seen, the hidden half is merely occluded by the invisible border, never shrunk. Invisible vertical screen boundary localx42. Two small hands ALWAYS grip this exact boundary, upper hand center(42,235), lower hand center(42,276), fingers complete and NEVER released, clothing sleeves attached. Parts behind border are hidden, so ALL visible art including hands lies localx28..246,y14..370, generous transparent cell gutters, no spill into neighboring cells. No actual drawn border or vertical stripe. Head aroundy20..210, hands/torso stay fixed with connected shoulder, waist and dress continuing down tolocaly366, no floating head or disconnected legs. All eyes open, gentle CLOSED-MOUTH smile, no blinking, mouth opening, new props, magic or expression changes. Hair and cloth have tiny consistent follow-through without detail changes. Accurate fixed grip points and stable head proportions are paramount. No numbers, text, grid, background, glow haze, clouds, chair or ropes.
ONLY ONE DOWNWARD HEAD NOD at the DEEPEST leaned-out border-gripping pose. In ALL sixteen frames both green eyes, face, connected shoulder/chest/waist and skirt visible to right of boundary, as in reference bottom-row THIRD pose with BOTH hands gripping border. The lean depth, shoulder position, torso and hands do NOT change. Stage0 neutral tiny smile head facing forward. Stages1..15 progressively lower chin and rotate head DOWN by only ~0.4degree perstep to total6degrees atstage15; neck articulates naturally, head is not shrinking, rising or suddenly flipping. Eyebrows and jade forehead flower follow the head. Green eyes stay OPEN and look slightly downward gradually. Hair and bows have only tiny follow-through, no different facial expressions. All sixteen poses show a smoothly increasing tiny nod; this will be replayed backwards to raise her chin. Do not blink or lean in/out. Every cell SAME fully exposed posture with anchored hands and body, only head nod changes.
```

### sway

```text
Create ONE production sprite atlas on true TRANSPARENT alpha background, exactly 1024x1536 pixels, EXACTLY FOUR columns and FOUR rows, sixteen cells256x384 read row-major. Reference image is the existing chibi character identity, outfit, watercolor-anime style, and shallow/full border-gripping poses; ignore the old atlas arrangement. Silver-gray loop-bun hair, forehead jade flower/navy bows, green eyes, white/navy Chinese-inspired dress. This must be sixteen NEW painted intermediate poses of ONE SINGLE small continuous action, NOT a collection of expressions. Fixed orthographic camera and CONSTANT character HEAD SCALE and costume proportions in ALL cells: head height including bun to face chin about190px, head widthabout174px when fully seen, the hidden half is merely occluded by the invisible border, never shrunk. Invisible vertical screen boundary localx42. Two small hands ALWAYS grip this exact boundary, upper hand center(42,235), lower hand center(42,276), fingers complete and NEVER released, clothing sleeves attached. Parts behind border are hidden, so ALL visible art including hands lies localx28..246,y14..370, generous transparent cell gutters, no spill into neighboring cells. No actual drawn border or vertical stripe. Head aroundy20..210, hands/torso stay fixed with connected shoulder, waist and dress continuing down tolocaly366, no floating head or disconnected legs. All eyes open, gentle CLOSED-MOUTH smile, no blinking, mouth opening, new props, magic or expression changes. Hair and cloth have tiny consistent follow-through without detail changes. Accurate fixed grip points and stable head proportions are paramount. No numbers, text, grid, background, glow haze, clouds, chair or ropes.
ONLY ONE SIDEWAYS HEAD TILT at the DEEPEST leaned-out border-gripping pose. In ALL sixteen frames both green eyes, face, connected shoulder/chest/waist and skirt visible to right of boundary, as in reference bottom-row THIRD pose with BOTH hands gripping border. Lean depth, shoulder position, torso and hands do NOT change. Stage0 neutral head upright tiny smile. Stages1..15 progressively tilt head CLOCKWISE (viewer right) by only ~0.4degree perstep to total6degrees atstage15. Head diameter is constant, do NOT alternate directions, nod, blink, shift torso or release hands. Eyes OPEN and gentle closed-mouth smile constant. Neck articulates, jadeflower/buns/bows follow tiny head tilt, hair has subtle consistent follow-through. This will be replayed backwards to return upright. All sixteen poses show small continuous increments of the SAME clockwise head tilt, not separate expressions. Fixed gripping hands and stable body proportions in allcells.
```



### 最后姿势的手臂修正

```text
EDIT the attached existing sprite atlas, not a new pose layout. Preserve EXACT1024x1536 canvas, FOUR columns FOUR rows, sixteen sprites, existing character/style/transparentalpha and existing poses. Surgical correction ONLY in BOTTOM-RIGHT cell, row4 col4 (index15): this last sprite mistakenly has a THIRD hand hanging at her right hip beneath the blue bow. REMOVE that extra hip hand AND its extra arm/sleeve; retain the connected dress and skirt with the same costume as adjacent bottom-row THIRD cell. She must have EXACTLY TWO arms and EXACTLY TWO hands, BOTH hands gripping the invisible vertical border at the left edge of her sprite, one above the other, same upper/lower positions and fingers as before. No right-side hanging hand, no released arm, no third limb. Keep head size/tilt/face/eye expression, bun loops, forehead jadeflower, bows, entire neck/shoulder/waist connection, hair and the two existing gripping palms unchanged. Bottom-right remains the deepest small lean-out pose, almost identical depth to adjacent bottom-row third cell, only the erroneous extra limb is repaired. Leave the other FIFTEEN cells as unchanged as possible. Do not redesign costume or replace layout. Empty space between sprites must stay real transparent alpha, no background, no grid/labels/numbers or painted border. Identity is the same chibi silver-haired green-eyed Luo Tianyi with white/navy Chinese-inspired dress. Output the corrected FULL16-cell atlas.
```

## 最终采用的探出顺序

零起始索引，按测量深度交错，A为side-reveal-a-v7.png，B为side-reveal-b-v8.png：

B0 → A4 → A1 → A3 → A2 → A0 → B1 → B2 → B3 → A5 → A6 → A7 → B4 → A8 → A9 → A10 → A11 → B5 → B6 → A12 → B7 → A13 → A15 → A14 → B8 → B10 → B9 → B11 → B12 → B15 → B13 → B14。

点头在第23阶段插入，轻摇在第22阶段插入（阶段从1起计）；完成后继续剩余探出并逆序回收。登记可由tools/register-tianyi-sequences.py --check复现。
