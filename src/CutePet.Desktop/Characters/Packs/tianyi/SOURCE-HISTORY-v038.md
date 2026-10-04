# 天依角色素材（0.38.0）

本版是非官方 AI 同人素材，原角色权利归相关权利人；代码的 GPL v3 不授予原角色权利。原形象来源与权利说明见仓库 THIRD_PARTY_NOTICES.md。参考沿用项目已有 AI 站立、王座、左右、托腮和秋千图片，没有下载或传入新的官方立绘。

日期：2026-10-04。工具：内置 image_gen.imagegen，transparent_background=true，未用 CLI / API key 回退。本版补绘并接入六张 1024×1536 透明图集，各 16 个不同姿势，共 96 个真实区域；28 个动作、240 帧引用。云层、秋千挂饰和花园装饰沿用旧素材。小猫保留原矢量导出的四组动作。

PNG 均原样复制。运行时 region 取帧，普通动作 240×360，贴边 256×352；实际行距通过配置登记，未用脚本编辑图片。上下沿接触点与左右扶边按逐帧锚点登记，重复区域只解码一次。tools/register-tianyi-sequences.py 只读取像素、复现配置；不会修改 PNG。

历史素材与完整来源档案保留在匹配源码中的 SOURCE-HISTORY-v037.md 以及早期档案。运行程序和角色 ZIP 仅含实际引用素材。AI 补绘仍可能在发丝、衣纹或椅饰上存在轻微变化，属于逐帧图集而非骨骼动画。

# 天依连续姿势图集生成记录（0.38.0）

工具：内置 `image_gen.imagegen`，所有调用 `transparent_background=true`。未使用 API key 或 CLI 回退。PNG 均原样复制，未用脚本修改像素。生成图集虽按四列四行排布，实际行距有偏差，因此运行时使用已登记的 `region`，而不直接按 384 像素等分切片。普通动作区域为 240×360，贴边为 256×352。

初稿参考原站立、王座、左右、下沿和秋千 PNG，补绘站立互动、施法、下降、坐姿、乘云、渐进探头、托腮和秋千表情。六张初稿都保留在生成工具原始目录；未接入初稿不随运行包分发。以下是最终留白修正调用的完整提示词，修正前图集即对应首轮输出。

## standing-sequence-v4.png

- 初稿：exec-57060f6a-b736-4212-ab1c-8311ea631049.png
- 接入输出：exec-84ff66a4-8cd7-4c58-8641-aa3b3dcaf6d7.png
- 每张 1024×1536 RGBA，16 个不同姿势。

```text
Layout repair ONLY for this exact 16-pose transparent animation atlas. Preserve all sixteen distinct expressions and poses, same fairy identity, costume and chronological4by4 order. Exact1024x1536 RGBA,4columns4rows,256x384cells. CRITICAL: REDUCE each complete sprite to70percent its present scale inside ITS OWN CELL. Every full silhouette including all long hair/ribbons/boots must fit ONLY within localx40..216,y48..336. Center every figure atx128, feetliney336. At least40 fully transparent pixels left/right and at least40 above/below each figure. All pixels outside these individual boxes alpha=0. No neighboring hair fragments, no clipping, no backgrounds, no haze, no text or lines. Do not crop limbs; draw full bodies. Keep scale and registration consistent. Wide blank transparent gutters are mandatory even if fine detail becomes smaller.
```

## throne-sequence-v4.png

- 初稿：exec-62be31f0-0a1b-4419-832a-b2aca2510f6b.png
- 接入输出：exec-e7c164af-a606-4ffe-a9d0-dab42743c2c0.png
- 每张 1024×1536 RGBA，16 个不同姿势。

```text
Precise layout repair ONLY of this sixteen-pose transparent animation atlas. Keep all16 distinct chronological expressions/poses, exact fairy identity, outfits and4by4 reading order. Exact1024x1536 RGBA,4columns4rows,256x384cells. REDUCE each entire sprite to70percent its current scale WITHIN ITS OWN CELL. All full figures and chair/sparkles must fit localx40..216,y48..336; keep bootscomplete, centerx128, feetliney336. Chair seat fixed near localy258. Preserve progressive spell, gradual knee bending and lowering, seated blink and seatedwave. Mandatory wide transparent gutters: no pixel outside its own safe box, no neighboring hair fragments, no clipping, alpha=0 around each silhouette. Every cell is an isolated sprite, not a continuous scene. Uniform scale, preserve face/detail. No text, grid, backdrop, haze, floor or lines.
```

## cloud-sequence-v5.png

- 初稿：exec-25a5fd9a-713e-4e7e-b1e3-15149c5ef5f6.png
- 留白修正中间输出：exec-51c56873-1da4-4448-963d-90b8e15a7cf7.png
- 每张 1024×1536 RGBA，16 个不同姿势。

```text
Precise layout repair ONLY of this sixteen-pose transparent animation atlas. Keep all16 distinct chronological expressions/poses, exact fairy identity, outfits and4by4 reading order. Exact1024x1536 RGBA,4columns4rows,256x384cells. REDUCE each entire sprite to70percent its current scale WITHIN ITS OWN CELL. All complete figures including braids/ribbons/sparkles must fit localx40..216,y48..336, centerx128 and complete boot solesy336. Preserve tired, flying breeze, flyingblink, and spellcastrows. Mandatory wide transparent gutters: no pixel outside its own safe box, no neighboring hair fragments, no clipping, alpha=0 around each silhouette. Every cell is an isolated sprite, not a continuous scene. Uniform scale, preserve face/detail. No text, grid, backdrop, haze, floor or lines.
```

### 乘云发丝串帧修正（最终接入）

最终输出：exec-376fba0d-b7eb-4fc3-96a4-7d76593de292.png，原样复制为 cloud-sequence-v5.png。清理第三个乘云姿势跨到邻格的长发，保留姿势变化。v4 中间图留在生成工具目录，未分发。

```text
Edit this exact transparent16-pose fairy animation atlas, layout repair of WIND HAIR ONLY. Preserve all16 bodies/eyes/expressions/scale/coordinates,1024x1536 fourcolumnsfourrows. Important row2 column3 (zero-basedframe6) currently has a VERY LONG horizontal gray hair strand extending LEFT across its own cell boundary and contaminating neighboring frame5. REDRAW that wind strand as a shorter gently curved LEFTWARD braid/ribbon tail staying strictly INSIDE ITS OWN CELL. It must curve back/down near her left shoulder, never straight across neighboring cell. All hair and ribbons in EVERY sprite must lie only in localx28..228. REMOVE every stray hair/alpha fragment crossing columns, no clipped hair tips at x0 or256. Keep EACH complete character silhouette visually intact with at least24 transparentpixels on BOTH sides of each256px-wide cell. Keep all existing head/boot vertical positions and four rowpositions unchanged; do not recomposewholeatlas or scale bodies. Preserve flyingwind differences betweenrow2sprites, but windhairextensionis modest and stayswithinowncell. The little detached gray streak seen on farleft/right of some crops must be removed. No new props/background/grid/text/glowhaze. True transparentalpha aroundsprites. Sixteen isolated complete coherent portraits fullbodyboots, same clothingandface.
```

## edge-sequence-v5.png

- 初稿：exec-e230b21c-7759-47d1-b647-0a03d3f23112.png
- 接入输出：exec-6e67d934-81f7-476e-bda9-aa92cb535db0.png
- 每张 1024×1536 RGBA，16 个不同姿势。

```text
Precise layout repair ONLY of this sixteen-pose transparent animation atlas. Keep all16 distinct chronological expressions/poses, exact fairy identity, outfits and4by4 reading order. Exact1024x1536 RGBA,4columns4rows,256x384cells. REDUCE each entire sprite to70percent its current scale WITHIN ITS OWN CELL. All visible figure parts must fit localx64..216,y48..336. Gripping line atx64, upper hand center y208, lower hand centery248, identical acrossframes. Preserve 8 progressive leaning depths, partial coherent waist/skirt and nod/gaze responses. Fix lower silhouette to curve LEFT back behind invisible edge beforey330; no horizontal cut through skirt or hair, no visible boots or separate legs. Body remains mostly behind imaginary edge. Deepest pose shows continuous attached sliver of waist/skirt, not fullyfloatingbody. Mandatory wide transparent gutters: no pixel outside its own safe box, no neighboring hair fragments, no clipping, alpha=0 around each silhouette. Every cell is an isolated sprite, not a continuous scene. Uniform scale, preserve face/detail. No text, grid, backdrop, haze, floor or lines.
```

## bottom-sequence-v6.png

- 初稿：exec-8507b063-7989-4098-98d3-1e3f7a3fe43a.png
- 接入输出：exec-267ca896-9206-4a76-95ea-ecc11b803484.png
- 每张 1024×1536 RGBA，16 个不同姿势。

```text
Precise layout repair ONLY of this sixteen-pose transparent animation atlas. Keep all16 distinct chronological expressions/poses, exact fairy identity, outfits and4by4 reading order. Exact1024x1536 RGBA,4columns4rows,256x384cells. REDUCE each entire sprite to70percent its current scale WITHIN ITS OWN CELL. All complete visible heads/braids/ribbons/elbows/sleeves must fit localx40..216,y48..336. Two elbow sleeve ends contact SAME support line localy330 atx84andx172. Preserve distinct headlift, blinksmile, gaze and tilt poses with natural arm/neck continuity. Mandatory wide transparent gutters: no pixel outside its own safe box, no neighboring hair fragments, no clipping, alpha=0 around each silhouette. Every cell is an isolated sprite, not a continuous scene. Uniform scale, preserve face/detail. No text, grid, backdrop, haze, floor or lines.
```

## top-sequence-v4.png

- 初稿：exec-21ffbed7-a863-45f6-b570-1a965e135f0c.png
- 接入输出：exec-8d677212-9e5b-40db-8f4e-b03931dbe9a1.png
- 每张 1024×1536 RGBA，16 个不同姿势。

```text
Precise layout repair ONLY of this sixteen-pose transparent animation atlas. Keep all16 distinct chronological expressions/poses, exact fairy identity, outfits and4by4 reading order. Exact1024x1536 RGBA,4columns4rows,256x384cells. REDUCE each entire sprite to70percent its current scale WITHIN ITS OWN CELL. All complete full seated figures INCLUDING bothboots and jade seat/tassels must fit localx40..216,y48..336. Seat rope connection rings atx64andx192,y258 in EVERYframe, centerx128. Fixed seat and full connected lower body; preserve breathing, blink, gaze and tilt variations. No ropes or backgroundscenery. Mandatory wide transparent gutters: no pixel outside its own safe box, no neighboring hair fragments, no clipping, alpha=0 around each silhouette. Every cell is an isolated sprite, not a continuous scene. Uniform scale, preserve face/detail. No text, grid, backdrop, haze, floor or lines.
```


## 未接入的进一步探头修正

尝试输出：exec-194a28ac-2c54-46f0-9915-14d9113fafbb.png。前几格曝光变化仍不够均匀，未替代本版 v5 图集；原始输出保留，未分发。浅探头到露肩仍需后续单帧细化。

```text
Precise animation repair of this exact16-pose transparent LEFT EDGE peeking fairy atlas. Preserve character face/outfit/style, actual4columns4rows and1024x1536 RGBA. Keep rows3and4 nod/gaze expressions unchanged, retain wide transparent gutters. RE-DRAW FIRST EIGHT FRAMES (rows1and2) as an EVENLY PROGRESSIVE sequence, current first3 are too identical and next is sudden. Her imaginary vertical hiding edge atlocalx75 in EACH256x384cell, gripping hands atx75 aroundlocaly250 and285. Do not draw edge. Frame0 one eye and a narrow sliver of cheek only. Frame1 eye PLUS nose edge and part of mouth become visible, head leans right a little further. Frame2 full first eye/nose/mouth plus inner corner of second eye visible, first small shoulder sliver. Frame3 BOTH eyes visible but only part of far cheek; more shoulder and top sleeve naturally emerge. Frame4 full face visible with modest lean, attached chest and a sliver of waist visible. Frame5 head leans further RIGHT, connected torso and small waist/dress hem begin to show. Frame6 more side lean with broader connected waist/skirt. Frame7 maximum modest peek with continuous waist/skirt, no full legs/boots. EIGHT truly different incremental depth/exposure stages, NOT first3 repeated slivers then suddenly full face. Same head scale and body proportion throughout. Face center shifts gradually right about8pixels per successive frame; arms hinge around same left gripping line and upper hand height. Long braids/torso curve LEFT back behind imaginary edge at bottom with naturally complete hem, never horizontal-cut bust or disconnected parts. Every figure entirely in cellx52..224,y52..336; wide transparent padding above/below and between sprites, no stray fragments. Tiny anatomy changes support each leaning stage. No background/line/table/cloud/text/grid/numbers.
```
