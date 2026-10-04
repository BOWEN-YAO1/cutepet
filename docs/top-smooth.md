# 秋千表情连续过渡（0.41.0）

上侧原来混用 top-sequence-v4.png 与 top-inbetweens-v1.png，待机还轮换数张不同坐姿。不同来源的头部比例、表情、座椅位置不完全一致，待机进入回应时也可能直接切回第一张。提高播放速度不能消除这些跳变。

现在统一使用 top-sequence-v5.png 的 16 个姿势，保留闭眼微笑、左右张望和歪头微笑。待机使用同一个中性姿势，秋千、绳饰、花藤继续在独立的连续时钟上摆动。每种回应都从同一张中性姿势开始并回到它；回收复用相同姿势逆序，静止停留重复引用同一绘图，不当成新增绘图。两张闭眼歪头实际朝向两边，分别经过中性闭眼姿势，不直接交替切换。

登记脚本只读原 PNG，写入 region/canvas 配置：画布 256×352、目标头高 153 像素、座椅 Y=242、上沿悬挂 Y=0。额头的玉花作为始终可见的头部配准点，闭眼时不需要猜测眼睛位置。每个运动段时间单位 40 毫秒，目标 25 帧/秒；相邻姿势之间随 WPF 绘制回调采样过渡。待机本身无需每秒替换 25 张图片。

连续过渡对齐两帧头部标记再混合透明像素，局部位移在座椅前 64 像素内逐渐归零，座椅和绳子连接点保持固定。左右动作仍以双手为固定点。smoothFrames 对旧角色包默认关闭；新上侧配置需要配套 topSwing、登记画布及固定悬挂和座椅锚点。同动作的头部标记必须全部提供或全部省略，循环动作最后一帧可过渡回第一帧。

这不是光流或骨骼动画，25 帧/秒也不是所有设备的最低帧率保证。复杂衣纹和发丝仍可能轻微变化。预览由实际 WPF 窗口按 20 毫秒采样生成，不代表桌面录像或人工验收。

验证覆盖实际绘制后的头高差、座椅可见连接点、头部插值与固定座椅像素、回应起止姿势、菜单暂停、快速重复点击、工作区域变化、低额度和角色包导入导出。请选择内置天依或重新导入新版角色包体验。

当前全角色为 145 个活跃绘图、28 个动作、439 次帧引用。13 个活跃 PNG、裁切与登记画布的保守预算 40,895,387 像素 / 156.00 MiB，保持原 175 MiB 上限；不含其他内存与少量插值缓冲。旧上侧 32 个混合来源姿势由新 16 个一致来源姿势替换。旧素材保留在源码历史，不嵌入活跃运行包。

## 生成记录

使用内置 imagegen，参考本项目已有 top-sequence-v4.png；原图 exec-ccc0dafa-e1c5-498d-b778-5531ee5f2402.png 原样复制为 src/CutePet.Desktop/Characters/Packs/tianyi/top-sequence-v5.png，透明 alpha 保留。没有 CLI/API key 回退。

提示词的精确坐标是生成目标，运行时采用实际图片测量值。第一行第 2 格未形成半闭眼，改用于轻微微笑；眨眼采用实际半闭眼和闭眼格，避免插入开口笑造成抽搐。

```text
Create ONE production sprite sheet on TRUE TRANSPARENT background, exactly 1024x1536, EXACTLY 4 columns and 4 rows, 16 cells each256x384. REFERENCE is the seated character identity, white/navy costume, jade swing seat, silver loop buns, green eyes and painterly chibi illustration style. Discard source atlas arrangement. ALL 16 sprites have IDENTICAL camera, head diameter, costume proportions, hair volume, knee size and jade seat size. Character sits straight on the SAME jade green gently curved swing seat, hands relaxed on lap, knees together, connected complete lower legs and shoes visible. Every sprite fits local x22..234,y22..356. Seat attachment jade rings centered local (30,262) and (226,262) in EVERY cell, seat never moves or scales, same width. Head including buns width156 and height166, topy35,chiny201 in EVERYcell. Jade flower at forehead centered (128,76), no size jump, no upright torso rescaling. Very subtle facial animation, no dramatically different poses. 16 cells row-major: cell0 neutral open green eyes tiny smile facecentered;1 eyelids slightly lowered 20%;2 eyelids halfclosed50%;3 eyelids threequartersclosed75%;4 fully closed eyes soft smile;5 neutral open eyes looking a LITTLE left only pupils small headrotation-2deg;6 looking left a little more headrotation-4deg;7 farthest gentle left headrotation-6deg;8 neutral open eyes looking a LITTLE right headrotation+2deg;9 looking right more+4deg;10 farthest gentle right+6deg;11 neutral open eyes tiny smile, headtilt+1deg;12 smile slightly wider headtilt+2deg;13 soft warm smile headtilt+3deg;14 relaxed eyes smile headtilt+4deg;15 gentle halfclosed smile headtilt+5deg. Small evenly spaced differences between cells in each action, SAME SIZE and SAME jade seat and static hands/feet. The decorative flower and buns follow only tiny head tilt. No numbering, grid, labels, background, glow backdrop, swing ropes, vines, extra props or disconnected limbs. Transparency preserved between every sprite, generous cell gutters. This replaces jumpy mixed animation atlases: stable shape/scale is paramount. Copy the reference character's face/outfit faithfully, with identical head scale across every cell.
```
