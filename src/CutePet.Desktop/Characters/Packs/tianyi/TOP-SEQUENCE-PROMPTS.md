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
