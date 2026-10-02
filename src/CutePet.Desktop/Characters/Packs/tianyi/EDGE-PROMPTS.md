# 屏幕边缘动作提示词 · v0.16.0

使用内置 image_gen 工具，透明背景。引用现有 idle.png 保持角色形象，保留原图；第二张引用 edge-v1.png，仅改变表情。所有输出原样复制进角色包，不裁剪、不重绘。

## edge-v1.png

Use case: identity-preserve.
Asset type: transparent PNG animation frame for the CutePet desktop character pack.
Input image: reference for exact character identity, clothing, painting style, and head size; preserve these invariants.
Create a new natural LEFT SCREEN EDGE peeking pose of this same chibi Luo Tianyi: silver-lavender looped hair and long braids, green eyes, blue ribbons and green clover ornament, blue-white Chinese fantasy dress. She is upright behind an imaginary vertical border on the LEFT, leaning her head and shoulders gently toward the RIGHT into view, with two small anatomically correct hands holding the imaginary left vertical edge. Her lower torso and legs remain concealed to the left, not a full standing body. Curious calm expression, open eyes, subtle smile. No throne, no cloud.
Composition: retain the exact reference canvas size 1024 x 1535 and transparent background. Keep the face approximately the same pixel size as the reference (about 550px wide). Hair top near y=140, face center near x=500 y=650, hands along x=35 to 140 at y=850 and 1100. Visible body/hair end by y=1400. Leave alpha-transparent space around the entire silhouette. Only the character is drawn: the imaginary vertical edge, wall, screen and any supporting structure are NOT drawn. Pose must read as peeking around a vertical edge, not standing with arms open, not a sideways-rotated character.
Constraints: same detailed polished anime watercolor shading and clean edges as reference, single character, no text, no watermark, true RGBA alpha transparency, no checkerboard painted in image, no duplicate limbs.

## edge-smile-v1.png

Use case: identity-preserve.
Asset type: second animation frame of the same transparent CutePet screen-edge sprite.
Input image: EXACT base-frame edit target. Change ONLY the facial expression: gently close both eyes into happy curved eyelids and open her little mouth into a modest cheerful smile. Keep the entire pose, hand shapes and locations, tilted head angle, outline, hairstyle, clothing, proportions, and every part's pixel position unchanged. Same hands holding an imaginary LEFT vertical edge, same silver-lavender hair, green ornaments and blue-white dress. Do not lean farther, add objects or draw the border. Match the exact 1024 x 1535 canvas and RGBA transparency of the input. No throne, no cloud, no text, no watermark, no checkerboard, no extra limbs. This will alternate with the base frame, so geometric consistency is crucial.

