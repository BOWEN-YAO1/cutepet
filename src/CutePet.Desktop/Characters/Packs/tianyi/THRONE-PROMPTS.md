# 王座休息动作生成记录（0.10.0）

日期：2026-10-02。工具：内置 image_gen.imagegen 编辑模式，transparent_background=true；未使用 CLI / API key。保留项目原始服装与鞋子，原图未覆盖。新增五张 1024×1535 透明 PNG，坐姿与过渡姿势基于同一王座帧编辑。王座设计为本次 AI 生成，天依人物仍为非官方同人形象，参见 LICENSE.txt 与仓库 THIRD_PARTY_NOTICES.md。

## magic-v1.png

输入：idle.png

```text
Use case: identity-preserve. Image 1 is the edit target: existing transparent full-body chibi Luo Tianyi fan-art sprite for a Windows desktop pet. Produce exactly ONE animation frame, not a sheet. Canvas must be exactly 1024x1535 pixels, identical to input. Keep original character identity, silver hair, jade green eyes, hair ornaments, blue-white modest Chinese-inspired costume, dark heeled boots, clean anime line art and colors. Preserve full-body scale, head position and feet baseline unless explicitly changed. Genuine transparent alpha background, including all hair-loop openings. Entire hair, footwear, effects and furniture must fit inside the canvas. No scenery, floor, cast shadow, text, rune lettering, watermark or extra characters. Change only her viewer-left hand: lift it slightly in a small spellcasting gesture with relaxed open fingers, and add a small delicate cyan-jade ring of magic with three tiny sparkles near that hand. Keep her standing upright with original gentle expression. The magic stays close to her hand, no large aura. All other character placement and body geometry remain registered to input.
```

## throne-standing-v1.png

输入：idle.png

```text
Use case: identity-preserve. Image 1 is the edit target: existing transparent full-body chibi Luo Tianyi fan-art sprite for a Windows desktop pet. Produce exactly ONE animation frame, not a sheet. Canvas must be exactly 1024x1535 pixels, identical to input. Keep original character identity, silver hair, jade green eyes, hair ornaments, blue-white modest Chinese-inspired costume, dark heeled boots, clean anime line art and colors. Preserve full-body scale, head position and feet baseline unless explicitly changed. Genuine transparent alpha background, including all hair-loop openings. Entire hair, footwear, effects and furniture must fit inside the canvas. No scenery, floor, cast shadow, text, rune lettering, watermark or extra characters. Keep the standing character in exactly the original pose and registered position. Add ONE elegant small royal throne directly behind her, facing forward: ivory frame, cyan-blue upholstery, jade accents, curved winglike top, two simple armrests and four small legs. Same cute anime style; graceful and readable at tiny size, no crown or new head ornament. Throne back rises just behind her head to no higher than her hair; chair seat behind hips; both armrests visible beside waist. Character remains in foreground; chair fits behind her existing width/silhouette, feet stay at the original baseline. No glow or magic on this fully materialized frame.
```

## throne-form-v1.png

输入：throne-standing-v1.png

```text
Use case: identity-preserve. Image 1 is the edit target, the existing transparent Luo Tianyi character with an ivory/cyan/jade throne behind her. Produce exactly ONE desktop-pet animation frame on exactly the same 1024x1535 canvas. Preserve throne design, all throne pixel positions, size and legs. Preserve character identity, gray hair, green eyes, hairstyle, ornaments, blue-white modest costume, dark heeled boots and anime art style. Entire character and throne must remain visible inside the canvas. Genuine transparent alpha background including openings in hair and furniture. No scenery, floor, cast shadow, text, rune lettering, new props, crown or watermark. Change ONLY the throne: show the exact same chair materializing at about 40 percent opacity, with a thin cyan-jade shimmer along its outer contours and a few tiny sparkles. Character remains fully opaque in exactly the input standing pose and location, expression and boots baseline. Retain actual partial alpha on the throne so this is a transparent animation transition, not a chair drawn against a background.
```

## sit-mid-v1.png

输入：throne-standing-v1.png

```text
Use case: identity-preserve. Image 1 is the edit target, the existing transparent Luo Tianyi character with an ivory/cyan/jade throne behind her. Produce exactly ONE desktop-pet animation frame on exactly the same 1024x1535 canvas. Preserve throne design, all throne pixel positions, size and legs. Preserve character identity, gray hair, green eyes, hairstyle, ornaments, blue-white modest costume, dark heeled boots and anime art style. Entire character and throne must remain visible inside the canvas. Genuine transparent alpha background including openings in hair and furniture. No scenery, floor, cast shadow, text, rune lettering, new props, crown or watermark. Change ONLY the character pose to the midway stage of sitting down onto this throne. Bend both knees slightly and lower hips toward the seat, torso upright, hands reaching toward both existing armrests. Feet stay near the original horizontal positions and boots soles remain at y=1510 approximately; original character scale and head proportions unchanged, head lowers naturally with torso. Keep knees together, modest skirt intact, hair drapes naturally. The throne stays in its exact input position. This pose must be visually BETWEEN standing and fully seated and will also be used backwards when rising.
```

## sit-v1.png

输入：throne-standing-v1.png

```text
Use case: identity-preserve. Image 1 is the edit target, the existing transparent Luo Tianyi character with an ivory/cyan/jade throne behind her. Produce exactly ONE desktop-pet animation frame on exactly the same 1024x1535 canvas. Preserve throne design, all throne pixel positions, size and legs. Preserve character identity, gray hair, green eyes, hairstyle, ornaments, blue-white modest costume, dark heeled boots and anime art style. Entire character and throne must remain visible inside the canvas. Genuine transparent alpha background including openings in hair and furniture. No scenery, floor, cast shadow, text, rune lettering, new props, crown or watermark. Change ONLY character pose to fully seated comfortably on the throne's existing cushion: hips supported by seat, torso upright, hands resting lightly on the existing two armrests, knees together, boots and heel soles resting naturally near the bottom center of the canvas. Modest dress covers upper legs; calm gentle smile, both eyes open. Retain exact original full-body character scale and huge chibi head proportions; lower head naturally with the seated torso, do not enlarge or shrink the character. Hair falls around the arms and chair naturally without hiding the boots. The throne stays in precisely the original position, unchanged.
```
