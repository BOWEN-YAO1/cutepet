# 上侧绘图过渡加密（0.42.0）

0.41.0 把上侧来源统一为 16 张，但闭眼、张望、微笑仍共用这 16 张。尺寸登记和帧间混合无法补足实际动作姿势。0.42.0 为四个过程分别补绘 16 张，共 64 张实际源绘图，替换活跃的旧 16 张；重复引用、倒放和混合像素不计入这个数。

| 图集 | 实际绘图 | 播放过程 |
| --- | ---: | --- |
| top-blink-v6.png | 16 | 从睁眼逐步闭眼，原路睁开 |
| top-left-v6.png | 16 | 中性到轻微向左张望，原路回正 |
| top-right-v6.png | 16 | 中性到轻微向右张望，原路回正 |
| top-smile-v6.png | 16 | 小幅歪头和温柔微笑，原路回正 |

每张原 PNG 是 1024×1536、4列×4行，透明 alpha 原样保留。使用已有中性坐姿的实际 WPF 登记图作为参考，没有下载新立绘。姿势变化幅度以生成结果为准，提示词中的角度和坐标是目标，不是生成精度保证。闭眼后段主要是闭眼状态的细微变化。

## 登记与播放

登记脚本只读像素以测量头部、额头玉花和座椅玉环，写入 region/canvas/锚点；不修改 PNG。全 64 张采用同一目标头高 153、画布 256×352、座椅 Y=242.241、悬挂 Y=0。座椅固定，头部额头标记供已有短过渡对齐，避免闭眼时丢失配准依据。

左转第5、7、8格在实际 WPF 缩放后，下巴与颈部的窄色块连接使测量头高略增；依据实际登记画面的156/158像素结果作约2–3%的比例校准，继续保持座椅落点。检查实际渲染后的头高差，不仅检查配置中相同的数字。

生成器的闭眼图格顺序存在小幅开合反转，按实际虹膜可见面积调整为零起始索引0、2、1、3、4、5、9、7、8、6、10、11、13、14、12、15，使闭合过程不出现中途重新睁开。64张都被采用，PNG本身不修改。

运动绘图每张 40 毫秒，即目标每秒 25 个源帧时间段。闭眼有 31 次引用（16张闭合+15张逆序睁开），左右张望共 65 次，微笑歪头 33 次。三组均从同一待机画面出发并返回。静止待机保留一个中性图，秋千摆动由连续时钟推进。

完整天依包现在有 193 个不同绘图、28 个动作、506 次帧引用、16 张活跃 PNG。原 PNG、不同区域与登记画布保守累计 53,952,042 像素，约 205.81 MiB BGRA。新绘图使原 175 MiB 上限不足，统一解码预算改为 58,720,256 像素 / 224 MiB；图片、区域、画布、云层与装饰都计入同一预算。该数不是应用全部内存。PNG 单张 8 MiB / 2048边长、ZIP及解压 64 MiB、768次引用、256 KiB配置、单动作30秒限制保持。新版天依包需要应用 0.42.0；旧包继续兼容。

## 验证与预览

隔离窗口验证检查 64 张实际加载绘图及像素哈希、每组至少16张源绘图、闭眼序列严格原路回收、实际头高差、座椅可见接点与绳索、统一起止画面、菜单暂停、快速点击及导入导出。资源边界测试按当前共享预算构造超限素材，不能因提高上限而失去超限检查。

闭眼验证同时测量实际眼睛区域的绿色虹膜，检查至少八档可见开合程度和相邻变化幅度，避免只用图片数量证明动作连续。

本版通过14项核心协议检查与1,963项隔离窗口检查。实际64张登记画面的头高为152–154像素；闭眼虹膜计数依序为101、92、88、76、76、73、72、58、55、48、22、6、3、2、1、0。窗口检查与预览不是实际多屏长期验收。

- top-drawn-keyframes.gif：直接按40毫秒播放登记后的绘图，不经过帧间混合或秋千变换，供单独检查实际补绘。
- top-edge-actions.gif：实际 WPF 窗口按20毫秒采样，包含已有混合、秋千和装饰。
- top-edge-contact-sheet.png：窗口内三组动作的九个采样画面。

预览采用演示额度，不连接账号。25帧时间安排不保证每台设备实际帧率；更多源绘图也不能完全消除生成素材中的衣纹、发丝细微变化。选择内置天依即可体验；已导入的旧包不会自动覆盖，需要重新导入新版角色包。

## 原图与筛选记录

使用内置 imagegen，以 work/desktop-verification-v0.41-final/top-registered-00.png 为参考。生成图原样复制到 src/CutePet.Desktop/Characters/Packs/tianyi/，未使用图像编辑脚本。

| 原文件 | 仓库文件 |
| --- | --- |
| exec-73753084-3068-41ee-a60e-625d7083531d.png | top-blink-v6.png |
| exec-9ac2fd44-a78f-42ed-ae88-32826f658329.png | top-left-v6.png |
| exec-909be7ff-2dfe-4334-a528-2fe562b74a67.png | top-right-v6.png |
| exec-04d6711e-275a-4803-b6fd-a6f988f88397.png | top-smile-v6.png |

原图位于本机 .codex/generated_images/01a0f608-7491-76a1-9748-25b0b48972f3/。最初尝试48格综合图，exec-1538f744-c2b7-40cc-8e42-257df2b5b7af.png 实际生成10×5格，布局和动作分组不符合要求，未接入。随后拆为单过程4×4图集，额外生成独立右转组，最终采用上述64格。

原角色权利归相关权利人；本包为 AI 生成同人素材，代码的 GPL v3 不授予原角色权利。历史素材留在匹配源码，活跃包只带当前使用的四张新上侧图。

## 完整生成提示词

### blink

```text
Create ONE homogeneous 16-frame animation atlas, exactly FOUR COLUMNS and FOUR ROWS, portrait1024x1536, eachcell256x384. TRUE TRANSPARENT alpha background. Reference is a SINGLE neutral seated character, copy her head diameter, face proportions, white/navy dress, silver loop-bun hair, jade forehead flower, green eyes, jade swing board, complete knees/legs/shoes and anime painting style EXACTLY. Identical camera/scale in all16 cells. All visible pixels stay inside local x22..234,y22..358, generous transparent gutters including last row and outer columns. Jade ring attachment centers fixed at local (34,266) and (222,266), headtop y30, facechin y195, shoe bottoms y354, never resize/reposition torso, palms, knees or board. ONLY a tiny continuous face/head movement specified below. Each adjacent frame must change less than1/15 of the whole movement; DO NOT create16 arbitrary expressions, do not alternate between extremes. Source frames are actual chronological animation drawings, no labels, borders, ropes, effects or background. Costume details and body silhouette stay as close to a traced registration as possible, not redrawn from another camera. Focus on continuity, NOT variety.
ALL16 ROW-MAJOR drawings form ONE slow eyelid closing movement. Head is straight and motionless, mouth remains the SAME tiny closed-mouth smile. Frame0 fully OPEN eyes;1 eyelids6%closed;2 13%closed;3 20%closed;4 27%closed;5 33%closed;6 40%closed;7 47%closed;8 53%closed;9 60%closed;10 67%closed;11 73%closed;12 80%closed;13 87%closed;14 93%closed;15 fullyclosed relaxed smile. The green iris is progressively occluded by the downward lid, not suddenly replaced by a smiling arc. Preserve eye width/position in EVERYcell. ONLY close the lids by a tiny increment between neighboring cells. No wink, open mouth, head tilt, sudden early closure or reopening. This is NOT a full blink cycle; runtime will play this exact sequence in reverse to reopen.
```

### left

```text
Create ONE homogeneous 16-frame animation atlas, exactly FOUR COLUMNS and FOUR ROWS, portrait1024x1536, eachcell256x384. TRUE TRANSPARENT alpha background. Reference is a SINGLE neutral seated character, copy her head diameter, face proportions, white/navy dress, silver loop-bun hair, jade forehead flower, green eyes, jade swing board, complete knees/legs/shoes and anime painting style EXACTLY. Identical camera/scale in all16 cells. All visible pixels stay inside local x22..234,y22..358, generous transparent gutters including last row and outer columns. Jade ring attachment centers fixed at local (34,266) and (222,266), headtop y30, facechin y195, shoe bottoms y354, never resize/reposition torso, palms, knees or board. ONLY a tiny continuous face/head movement specified below. Each adjacent frame must change less than1/15 of the whole movement; DO NOT create16 arbitrary expressions, do not alternate between extremes. Source frames are actual chronological animation drawings, no labels, borders, ropes, effects or background. Costume details and body silhouette stay as close to a traced registration as possible, not redrawn from another camera. Focus on continuity, NOT variety.
ALL16 ROW-MAJOR drawings form ONE gradually turning head/gaze LEFT from straight ahead to a maximum FIVE degrees left. BOTH eyes fully OPEN throughout, identical tiny closed-mouth smile. Frame0 straight ahead; eachnextframe turns another1/3degree left and shifts pupils a fraction toward left; frame15 exactly5degrees left. Frames1..14 are evenly spaced incremental in-between poses, never suddenly far-left. Head rotates smoothly at the neck while torso/board/knees/hands are COMPLETELY stationary. No blink, wink, smile change, head tilt, mouth opening or turning back right. Hair follows rotation a tiny amount. This is NOT a left-right loop; runtime will reuse the exact frames in reverse for the return.
```

### smile

```text
Create ONE homogeneous 16-frame animation atlas, exactly FOUR COLUMNS and FOUR ROWS, portrait1024x1536, eachcell256x384. TRUE TRANSPARENT alpha background. Reference is a SINGLE neutral seated character, copy her head diameter, face proportions, white/navy dress, silver loop-bun hair, jade forehead flower, green eyes, jade swing board, complete knees/legs/shoes and anime painting style EXACTLY. Identical camera/scale in all16 cells. All visible pixels stay inside local x22..234,y22..358, generous transparent gutters including last row and outer columns. Jade ring attachment centers fixed at local (34,266) and (222,266), headtop y30, facechin y195, shoe bottoms y354, never resize/reposition torso, palms, knees or board. ONLY a tiny continuous face/head movement specified below. Each adjacent frame must change less than1/15 of the whole movement; DO NOT create16 arbitrary expressions, do not alternate between extremes. Source frames are actual chronological animation drawings, no labels, borders, ropes, effects or background. Costume details and body silhouette stay as close to a traced registration as possible, not redrawn from another camera. Focus on continuity, NOT variety.
ALL16 ROW-MAJOR drawings form ONE slowly warming smile and gradually tilting head RIGHT, from neutral to a maximum FIVE degrees. Eyes fully OPEN and green throughout, never closed. Frame0 same neutral tiny closed-mouth smile and straight head. Eachnextframe tilts another1/3degree RIGHT and makes the closed-mouth smile a tiny bit warmer, frame15 exactly5degrees RIGHT. Head height/width remain constant; connected neck bends a tiny increment, torso/hands/seat/knees COMPLETELY stationary. No open mouth, blink, sudden full grin, left tilt, hair identity change or return movement. Precisely monotonic16 drawings, same tilt direction all16. This is NOT an expression sampler; runtime will play reverse frames to return gently to neutral.
```

### right

```text
Create ONE homogeneous 16-frame animation atlas, exactly FOUR COLUMNS and FOUR ROWS, portrait1024x1536, eachcell256x384. TRUE TRANSPARENT alpha background. Reference is a SINGLE neutral seated character, copy her head diameter, face proportions, white/navy dress, silver loop-bun hair, jade forehead flower, green eyes, jade swing board, complete knees/legs/shoes and anime painting style EXACTLY. Identical camera/scale in all16 cells. All visible pixels stay inside local x22..234,y22..358, generous transparent gutters including last row and outer columns. Jade ring attachment centers fixed at local (34,266) and (222,266), headtop y30, facechin y195, shoe bottoms y354, never resize/reposition torso, palms, knees or board. ONLY a tiny continuous face/head movement specified below. Each adjacent frame must change less than1/15 of the whole movement; DO NOT create16 arbitrary expressions, do not alternate between extremes. Source frames are actual chronological animation drawings, no labels, borders, ropes, effects or background. Costume details and body silhouette stay as close to a traced registration as possible, not redrawn from another camera. Focus on continuity, NOT variety.
ALL16 ROW-MAJOR drawings form ONE gradually turning head/gaze RIGHT from straight ahead to a maximum FIVE degrees right. BOTH eyes fully OPEN throughout, identical tiny closed-mouth smile. Frame0 straight ahead; eachnextframe turns another1/3degree right and shifts pupils a fraction toward right; frame15 exactly5degrees right. Frames1..14 are evenly spaced incremental in-between poses, never suddenly far-right. Head rotates smoothly at the neck while torso/board/knees/hands are COMPLETELY stationary. No blink, wink, smile change, head tilt, mouth opening or turning back right. Hair follows rotation a tiny amount. This is NOT a right-right loop; runtime will reuse the exact frames in reverse for the return.
```

### 未接入的48格初稿

```text
ONE animation sprite atlas, TRUE TRANSPARENT background, LANDSCAPE 1536x1024. EXACTLY EIGHT COLUMNS and SIX ROWS: 48 separate full seated sprites. This 8-by-6 grid is mandatory; do not make a16-frame4x4 sheet. Reference: single seated chibi Luo Tianyi on jade swing board, silver loop-bun hair, jade forehead flower, green eyes, white/navy outfit, hands on lap, complete connected knees, lower legs and shoes. Copy this character/outfit/art style; use the SAME orthographic camera, head and body size, seat width and vertical position in EVERY sprite. Each cell192x170.67, each sprite fits centered local x38..154,y6..162. Head INCLUDING BUNS is exactly76px wide,70px high; headtop y9 and chin y79. Jade seat ring centers local (43,116) and (149,116) are FIXED across all48. Hands/torso/knees/seat/feet do NOT move between frames, only tiny head/face/eye changes with minute hair followthrough. No rescaling. All cells are separated by transparent gutters, no numbers, labels, grid lines, ropes, decorations, backgrounds or glow.
Three chronologically ordered 16-drawing clips, each occupying EXACTLY TWO COMPLETE ROWS, read left-to-right:
TOP TWO ROWS =16 actual gradual blink drawings. Frame0 eyes fully open gentle tiny closed-mouth smile. Frames1..7 progressively lower eyelids in seven tiny steps (approximately12%,25%,37%,50%,62%,75%,90%closed), frame8 fully closed relaxed, frames9..15 progressively reopen (90%,75%,62%,50%,37%,25%,fully open). Head angle and mouth DO NOT CHANGE during blink. Every adjacent pair differs only a TINY amount, no open-mouth pose interrupting blink.
MIDDLE TWO ROWS =16 gradual looking drawings, eyes OPEN throughout. Positions row-major: centered; look left1degree; left2;left3;left4;left5;left3;left1;center;right1;right2;right3;right4;right5;right2;center. Mouth same small smile throughout. Turn only the head and pupils, maximum5degrees, no gesture/hand/body change. Never jump from a left extreme directly to a right extreme.
BOTTOM TWO ROWS =16 gradual cheerful head-tilt drawings, eyes OPEN throughout. Closed-mouth smile slowly becomes slightly warmer (no big open mouth, no blink). Head tilt row-major:0degrees;-1;-2;-3;-4;-5;-3;-1;0;+1;+2;+3;+4;+5;+2;0. This is a dense sequence of tiny consecutive posture differences, not16 arbitrary expressions. Torso, hands and seat stay IDENTICAL. Same head diameter, face shape, clothing folds and shoe size all48. Continuity matters more than variety. Soft detailed anime shading matching reference, clean consistent alpha cutout edges. Need48 drawings (8columns x6rows), not duplicates of a16-sprite layout.
```

