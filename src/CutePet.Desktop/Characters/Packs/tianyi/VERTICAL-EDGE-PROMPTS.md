# 上下边缘动作提示词 · v0.17.0

使用内置 image_gen 工具，透明背景。现有 idle.png 用于保持角色形象；表情变化引用各自的基础姿势。四张新图原样复制到角色包，不裁剪、不重绘，原素材保留。

## edge-top-v1.png

Use case: identity-preserve.
Asset type: transparent CutePet desktop animation frame.
Input image: character identity and painting-style reference. Preserve the exact chibi Luo Tianyi identity, silver-lavender looped hair and long braids, green eyes and green clover hair ornament, navy ribbons, blue-white Chinese fantasy dress, face proportions and polished anime watercolor shading.
Primary request: a NEW upright hanging / peeking pose for the TOP edge of the desktop. She reaches both arms up and holds an imaginary horizontal upper border with two little hands, while her head and upper body hang naturally BELOW her hands, looking gently downward toward the viewer with curious open green eyes and a small smile. Keep head upright, naturally bend the arms, draw exactly two hands and anatomically plausible shoulders. Show only head, arms and upper torso, lower body concealed, no legs. No sideways or upside-down rotation.
Composition: exact 1024 x 1535 RGBA transparent canvas. The two hands touch the imaginary horizontal top edge around y=90, one at x=220 and one at x=800. Hair starts below that near y=150; face approximately 550 pixels wide, face center near x=512 y=700; blue-white sleeves frame the face. Body and long braids extend downward naturally and end before y=1450. Centered balanced silhouette. Keep a small transparent outer margin. Do NOT draw the imaginary edge, a wall, a platform, cloud or throne. Only the character cutout.
Constraints: preserve reference design and fine details, single character, true alpha transparency, no painted checkerboard, no text, no watermark, no extra limbs.

## edge-bottom-v1.png

Use case: identity-preserve.
Asset type: transparent CutePet desktop animation frame.
Input image: reference for exact character identity, clothing, head proportions and detailed polished anime watercolor style.
Primary request: a NEW BOTTOM-edge chin-rest pose of the same chibi Luo Tianyi, silver-lavender looped hair and braids, green eyes, green clover ornament, navy ribbons, blue-white Chinese fantasy dress. She leans forward comfortably over an imaginary horizontal ledge, rests BOTH elbows on that ledge, and cups her cheeks / chin gently in both hands. Curious relaxed open-eye expression with a small smile, looking at the viewer. Visible head, shoulders and sleeves only; lower torso hidden below the imaginary ledge. No full standing body, no legs, no throne or cloud.
Composition: exact 1024 x 1535 RGBA transparent canvas. Head approximately 550 pixels wide and face centered near x=512 y=700. Hands cup her chin around x=330 and x=690, y=1080; elbows and the bottoms of the two blue-white sleeves sit on an imaginary straight horizontal support around y=1370 (89.3% canvas height). Long braids remain above or finish at that support. Keep the silhouette centered with outer transparent margins. The ledge, desk, taskbar and border are NOT drawn; only the character cutout is drawn. No art extends beneath the elbow contact line except tiny natural sleeve folds.
Constraints: preserve detailed character design and identity, natural symmetrical two-hand chin-rest pose, true alpha transparency, no checkerboard, no text, no watermark, no extra hands or limbs.

## edge-top-smile-v1.png

Use case: identity-preserve. Asset type: transparent CutePet animation expression frame. Input image is the EXACT base-frame edit target.
Change ONLY her facial expression: gently closed eyes with happy curved eyelids and a modest cheerful open-mouth smile. Preserve ALL geometric positions: head angle, hair outline, hands, fingers, arms, body, clothing, ornaments, silhouette and contact line. Preserve the exact 1024 x 1535 RGBA transparent canvas and detailed anime watercolor shading. No head movement or repositioning; this will alternate with the base frame, geometric registration is crucial. No text, watermark, painted checkerboard or new objects. Keep both raised hands holding the imaginary TOP border in exactly the same places.

## edge-bottom-smile-v1.png

Use case: identity-preserve. Asset type: transparent CutePet animation expression frame. Input image is the EXACT base-frame edit target.
Change ONLY her facial expression: gently closed eyes with happy curved eyelids and a modest cheerful open-mouth smile. Preserve ALL geometric positions: head angle, hair outline, hands, fingers, arms, body, clothing, ornaments, silhouette and contact line. Preserve the exact 1024 x 1535 RGBA transparent canvas and detailed anime watercolor shading. No head movement or repositioning; this will alternate with the base frame, geometric registration is crucial. No text, watermark, painted checkerboard or new objects. Keep BOTH hands cupping her cheeks / chin and BOTH elbows resting on the imaginary BOTTOM support in exactly the same places.
