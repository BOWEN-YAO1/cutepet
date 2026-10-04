# 侧边连续过渡（0.40.0）

修复侧边探出时突然放大、切回和跳动。0.39.0 将旧图和另一套补绘图交替播放，整体身高登记不能约束头部比例，许多原帧还持续 80 毫秒。

左右回应现在统一采用 side-sequence-v6.png 中 15 个连续探出姿势。原图有 16 格，最后一格松手并改变服饰，没有接入。旧左右图集保留在源码历史，不嵌入程序或活跃角色包。

登记脚本测量最大肤色连通区域的脸颊下端和发顶，将头部高度统一为 200 像素，透明画布 288×384。双手支撑坐标固定 (48,250)，换帧不移动锚点。PNG 原样保留；脚本只读像素并写配置。

四种回应的运动帧均为 40 毫秒，目标每秒 25 个源帧时间段。待机不需要不断换图。探出 0→14，回收使用同组图片逆序；回应只走相邻姿势。探头 1160 毫秒，缩回再探出 1560 毫秒，轻摇 1560 毫秒，点头 1320 毫秒。

可选 smoothFrames 仅用于有登记画布、一致双手锚点的左右图集。每帧记录始终可见的右侧绿瞳中心 headAnchorX / headAnchorY。绘制先把两帧眼睛移到共同的插值位置，上半身局部位移在靠近双手前 64 像素逐渐归零，双手保持固定；再做预乘 alpha 混合，随 WPF 绘制回调采样。这是局部位置配准和像素混合，不是光流或骨骼补绘；不把插值算作额外手绘姿势。复杂结构仍可能有轻微重影、衣纹变化；目标帧率不是所有设备的最低帧率保证。

插值器只保留两份源像素缓冲和一份可复用输出。其他动作和旧角色包默认保留原路径。隐藏、关闭时取消绘制订阅，换角色时释放缓存。菜单暂停、反复点击、低额度、角色切换和解除贴边继续同步处理。

全角色 161 个活跃绘图、28 个动作、459 次帧引用。14 个活跃 PNG、裁切及登记画布保守累计 43,875,986 像素 / 167.37 MiB，保持原 175 MiB 素材上限；此数不含其他内存和少量插值缓冲。旧来源档案保存在 SOURCE-HISTORY-v039.md。

验证包含实际渲染头高差不超过 5 个源像素、固定支撑点、40 毫秒时长、透明插值、导入导出、暂停和边界复位。侧边 GIF 每 20 毫秒采样实际 WPF 窗口，包含中间过渡；不代表屏幕录像或人工验收。实际体验请选择内置天依或导入新版角色包。

## 选用生成记录

使用内置 imagegen，参考 idle.png 的人物身份和服饰。原文件 exec-8372b239-e561-4110-8d02-452408bb15c6.png 原样复制为 side-sequence-v6.png，透明度保留。

```text
Make ONE precisely staged animation sprite sheet on a TRUE TRANSPARENT background, 1024 by 1536 pixels. EXACTLY four columns and four rows = 16 sprites. Reference is solely the character identity, costume and illustration style. Do not copy a prior atlas layout. Each cell is 256x384. Same chibi Luo Tianyi: silver loop-bun hair, jade flower and navy bows, green eyes, white/blue dress. Two hands grip an invisible vertical screen border, at local x=48 with upper hand y=235 and lower hand y=271, NEVER moving these grip points. Each frame is from the SAME orthographic camera, head INCLUDING hair buns is consistently 156px wide and 205px tall, hair top around y=30, face chin near y=220. The character leans her connected torso progressively out to the RIGHT of the invisible border, exposing part of the shoulder, waist and dress down to y=356. Each cell fits entirely x=28..236,y=18..370, with transparent gutters, NO glow backdrop, no drawn border. All eyes stay open and gentle smile stays the same for all 16. This sheet contains ONLY a single continuous leaning-out movement, not expressions, blink, looking around or a pose collection. Numbered frames are descriptive ONLY; DO NOT DRAW NUMBERS OR LABELS: 0 is shallow one-eye peek. 1 is just 1/15 more revealed, 2 is 2/15, 3 is 3/15, 4 is 4/15, 5 is 5/15, 6 is 6/15, 7 is 7/15, 8 is 8/15, 9 is 9/15, 10 is 10/15, 11 is 11/15, 12 is 12/15, 13 is 13/15, 14 is 14/15, 15 is fully leaned out with both eyes, shoulder and waist visibly joined. The hidden half of the character must be clipped behind the invisible border, while head, shoulder and hip articulate progressively: NOT rigidly translating the whole body. Frame 3 must still be only 20% of the lean-out, not fully revealed. Frame 7 around half lean. Frame 11 around three quarters. NO jump from a shallow peek to a fully revealed face in row one. EVERY adjacent pair is separated by only a tiny evenly spaced drawing change. Same head scale, face shape, outfit details, hand registration and transparent boundary in ALL 16 cells. Clean consistent anime shading matching the reference. No ropes, throne, cloud, standalone detached heads, extra legs or text.
```

## 未选用草稿

exec-7416dd7a-ea52-448f-97fd-bcb1dfa26e90.png 未采用：要求 24 格但排版不符，浅探到全露出的间隔仍大。草稿未嵌入运行包。

```text
Create a production sprite atlas for a Windows desktop pet. Reference image is the identity, outfit, rendering style reference ONLY; discard its individual pose sequence. Character: cute chibi Luo Tianyi, silver-gray hair with two loop buns and jade flower blue bows, bright green eyes, white and navy Chinese-inspired dress. Actual alpha transparent background. ONE homogeneous continuous animation sheet: EXACTLY 24 cells, four columns by six rows, read left-to-right then top-to-bottom. Canvas 1024x1536. Every cell 256x256. Fixed orthographic camera, EXACT SAME head diameter, limb thickness, face and outfit proportions throughout. Head is 108px wide, 115px tall including hair buns, on ALL cells; don't enlarge her head in middle frames. Continuous leaning-out from the left screen edge; she is attached to an invisible vertical boundary at x=48 in each cell, two small gripping hands stay at x=48, y=154 and y=182 in every cell. Her hair top stays y=18 (+/-2), torso connected, dress extends naturally to y=238, never floating disconnected head. No painted vertical line, no border, labels or grid. Consistent empty gutters inside every cell; all visible pixels inside local x=24..232,y=12..244. Frame 0: very shallow peek one eye visible, most face/body hidden behind the invisible left boundary, fingers around it. Frames 1 through 15: sixteen evenly spaced incremental articulated poses gradually rotating and bending shoulder and torso into view while palms stay fixed, progressively exposing second eye, neck, shoulder, waist, skirt; this is connected torso leaning, no entire-sprite slide. No jumps between neighboring frames, stable head and eye size. Frame 15: deepest lean, both eyes open, gentle smile, visible connected shoulder/waist/skirt. Frames 16,17,18,19: same deeply exposed body, progressive subtle half-close to close to half-open to open blink with slight gentle nod. Frames 20,21,22,23: same deep lean, four smooth small head-tilt poses (-3,-1,+1,+3 degrees), no big motion, anchored hands unchanged. All 24 cells have the SAME SCALE and the same grip coordinate and same camera. Hair, bows, skirt should overlap smoothly between neighboring poses. No diffuse background or glow outside silhouette. No throne, swing, effects, props. High-detail clean anime illustration, crisp cutout alpha edges. This will be displayed small at 148px character height, so precision in silhouette and fixed hand/head registration is critical.
```

