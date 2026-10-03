# 完整左右贴边提示词 · v0.32.0

2026-10-03，使用内置 image_gen 编辑透明角色素材。新素材原样复制，均为 1024×1535 RGBA；保留旧上半身图作为历史素材，不再被内置动作引用。右侧由 WPF 镜像，不另生成。角色权利说明见 LICENSE.txt / SOURCE.md。

## edge-full-v2.png

参考：idle.png（完整人物）与 edge-v1.png（旧贴边姿势）。原始输出：exec-05051664-4a45-47d9-8ad8-8cce28ba7cb3.png，保留在 image_gen 默认生成目录。

```text
Use case: identity-preserve edit. Asset: transparent PNG full-body LEFT desktop-border peeking animation base frame. Image 1 (idle.png) is the reference for COMPLETE anatomy, dress and shoes. Image 2 (edge-v1.png) is the existing peeking pose to repair. Preserve the same chibi Luo Tianyi identity, silver-lavender looped hair, long braids, green eyes, green clover ornament and jade earrings, blue ribbons, detailed blue-white Chinese fantasy dress, dark blue decorated ankle boots, polished anime watercolor style. CRITICAL FIX: draw the ENTIRE connected character from hair to skirt hem, BOTH thighs and lower legs, and BOTH complete boots. Do not terminate the body at the waist, do not hide the anatomy by leaving it transparent, do not crop any part of the character off the sprite canvas. The image must contain a complete lower body even when it will later be masked by the desktop border. Natural upright cling-and-peek pose: both small hands grasp the same imaginary vertical line near the LEFT of the canvas at x=70, at two heights around y=680 and y=850. Head and upper torso lean slightly RIGHT into view, hips stay nearer the left line and legs continue naturally below the skirt, knees relaxed, boots close together. This should read as a whole small character holding a border, not a detached upper-body sticker. Open green eyes, gentle curious smile. Composition: keep the reference canvas EXACTLY 1024 x 1535, portrait RGBA with true transparent background. Fit the complete figure with safe transparent margins: hair top around y=40, head/face roughly y=150..660, skirt around y=870..1110, two legs and boots around y=1090..1480; shoes fully visible before y=1490. Retain chibi head/body proportion and visual identity but make room for the complete legs; no giant head that leaves no room for the lower body. All physical connections coherent: neck to torso to waist to skirt to both legs to both boots, continuous hair. No drawn wall, vertical line, frame, floor, throne, cloud, shadow, text, watermark, extra limbs, or background. The imaginary edge is NOT drawn. Keep alpha transparency.
```

## edge-full-smile-v2.png

参考：edge-full-v2.png，仅修改眼睛与嘴巴。原始输出：exec-202810e3-75db-4797-abcf-7205502b952b.png，保留在 image_gen 默认生成目录。

```text
Use case: precise expression-only identity-preserve edit. Asset: transparent PNG matched facial-expression frame for a CutePet desktop animation. Input is the EXACT full-body peeking base image. Change ONLY both eyes and the little mouth: green open eyes become gentle cheerful closed curved eyelids, and the small mouth opens into a modest happy smile. Preserve EVERY OTHER PIXEL POSITION and silhouette as closely as possible: same head angle, hair loops and long braids, green ornaments, two hands grasping imaginary LEFT edge at their current positions, same connected torso, entire skirt, BOTH thighs and lower legs, and BOTH complete dark-blue boots at the bottom. The entire new lower body is essential and must remain intact and anatomically connected; do not revert to a cropped upper-body portrait. Absolutely no body movement, no resizing the character, no cropping, no changed hand positions, no new accessories. Keep the EXACT 1024 x 1535 portrait canvas and true RGBA alpha transparency. No actual border, no wall, no ground, no clouds, no throne, no shadows, no text or watermark. This sprite will alternate with the input, so geometric registration and unchanged body pixels are critical.
```

## 接入

五个左右动作共用这两张完整人物图；edgeAnchorX 为 0.095。单次向外探出最多 2 DIP，缩回再探出的回缩部分保留，左右轻摇和表情保留。当前引用的 PNG 数和总解码像素不变，角色包导入 / 导出随动作引用包含新图和 SOURCE.md 中的提示词记录。
