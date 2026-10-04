# 角色素材记录

程序代码许可证见 LICENSE（GNU GPL v3）。

0.36.0 新增的内置角色台词为项目编写的本地文本，随各自角色配置和 GPL v3 源码分发。本版未生成或替换图片，原角色与素材权利说明继续适用。便携包许可证位于 licenses/，匹配源码与提交记录位于 source/。

0.37.0 调整自动活动时序和角色 / 云层位移过渡，沿用现有素材，未生成或替换角色图集。新增收云 GIF 来自项目 WPF 离屏渲染，额度为演示内容；原角色与素材权利说明继续适用。

应用图标（0.29.0）：本项目 XAML 矢量绘制的小猫与云，源文件为 `src/CutePet.Desktop/Assets/Brand.xaml`；`tools/CutePet.IconBuilder` 生成多分辨率 ICO 和 PNG。随项目按 GPL v3 分发，不是洛天依或其他角色的官方标识，也未使用外部图标素材。

小猫：由本项目 XAML 矢量路径绘制，0.8.0 导出为透明帧，0.8.1 整理到 `src/CutePet.Desktop/Characters/Packs/cat/`，随项目按 GPL v3 分发。角色包包含完整许可证和原始矢量源码链接。

洛天依同人素材：`src/CutePet.Desktop/Characters/Packs/tianyi/idle.png`，2026-10-01 使用内置 imagegen 生成；同目录下的 `blink.png`、`greeting.png`、`low.png` 于 2026-10-02 使用同一工具编辑原图生成。均为 AI 生成的非官方同人表现，未复制下载的官方立绘，也不是官方授权素材。洛天依名称及原角色形象的相关权利仍属于其各自权利人；本项目的代码许可证不授予这些角色权利。项目不声称与 Vsinger 或角色权利人存在合作、认可或授权关系。

生成工具、提示词及接入方式见 docs/character-assets.md。预览中的额度是演示数据。

用户导入的角色只存于本机，不提交到仓库。导出时保留包内的许可与来源说明；未知素材许可不自动套用项目代码的 GPL。

0.9.0 新增的天依六张动作图由内置 image_gen.imagegen 编辑已有项目同人图生成，原始四张图保留；与原图采用相同角色权利说明。完整提示词与画布修正记录位于天依包 SOURCE.md / PROMPTS.md，随源码分发，SOURCE.md 随角色包导出。

0.10.0 王座和五张休息动作帧由内置 image_gen.imagegen 编辑项目现有同人图生成，王座为新增 AI 设计；人物仍适用上述原角色权利说明。完整提示词在天依 THRONE-PROMPTS.md / SOURCE.md，原有素材未覆盖。

## 0.11.0 乘云飘动

2026-10-02：新增天依独立云层（cloud-v1.png）、施法召唤和短距离横向飘动、自动开关与交互暂停。角色素材保留原有脸、服装和动作；小猫保留原行为。云由内置 image_gen.imagegen 生成，1774×887 原始透明 PNG，完整提示词位于角色包 CLOUD-PROMPTS.md 及 SOURCE.md。运动与验证边界详见 docs/cloud-drift.md；属于非官方同人设计，原角色权利说明继续适用。

## 0.12.0 王座动作衔接

2026-10-02：新增 sit-prepare-v2.png、sit-lower-v2.png、sit-near-seat-v2.png 三张过渡帧，召唤 / 坐下 8 帧、起身 9 帧，按当前帧进度反向起身。格式继续为 1，像素限额调整到 33,554,432；旧包保留兼容。新图由内置 image_gen.imagegen 编辑，原图保留，完整提示词位于 REST-SMOOTH-PROMPTS.md 与 SOURCE.md。动作及验证边界详见 docs/rest-smooth.md，原角色权利说明继续适用。


## 0.13.0 坐姿互动

2026-10-02：新增 sit-blink-v1.png、sit-happy-v1.png、sit-wave-v1.png 三张坐姿动作图，来自内置 image_gen.imagegen 对 sit-v1.png 的编辑，保留原图。公共播放器区分坐姿和站姿回应；CharacterPresenter 保留自动休息并触发坐姿微笑。天依共 13 种动作、20 张人物图和 1 张云层，像素限额保持不变。完整提示词在 SEATED-PROMPTS.md / SOURCE.md，原角色权利说明继续适用。行为和 348 项窗口 / 功能验证见 docs/seated-actions.md。

## 0.16.0 屏幕边缘互动

2026-10-02：新增 edge-v1.png 和 edge-smile-v1.png，均由内置 image_gen 工具编辑项目现有同人图生成，原图保留。新素材为 1024×1535 透明 PNG，原样纳入角色包；右侧仅在运行时镜像。完整提示词位于 EDGE-PROMPTS.md / SOURCE.md，原角色权利说明继续适用。行为和验证边界见 docs/screen-edge.md。

## 0.17.0 上下边缘素材

2026-10-02：新增 edge-top-v1.png、edge-top-smile-v1.png、edge-bottom-v1.png、edge-bottom-smile-v1.png，由内置 image_gen 工具编辑项目现有同人图生成；原图保留，四张图均为 1024×1535 透明 PNG，原样纳入角色包。完整提示词在 VERTICAL-EDGE-PROMPTS.md / SOURCE.md，原角色权利说明继续适用。验证边界见 docs/screen-edge.md。

## 0.32.0 完整左右贴边素材

2026-10-03：内置 image_gen 编辑现有同人图，补绘完整裙摆、双腿及鞋，并生成匹配的微笑帧。新增 edge-full-v2.png / edge-full-smile-v2.png 均为 1024×1535 透明 PNG，原样接入，旧图保留。提示词与生成记录位于天依包 FULL-SIDE-PROMPTS.md / SOURCE.md，原角色权利说明继续适用。

## 0.33.0 逐帧左右探出素材

2026-10-03：内置 image_gen 参考原同人图制作八姿势图集，最终 edge-sequence-v3.png 为 1536×1024 RGBA 原始输出，原样复制，运行时取区域帧。提示词及修正记录位于天依包 SIDE-SEQUENCE-PROMPTS.md / SOURCE.md，旧图保留、原角色权利说明继续适用。

## 0.34.0 下沿托腮素材

2026-10-04：内置 image_gen 编辑原下沿同人素材，生成八种托腮姿势并修整分格留白。最终 bottom-sequence-v4.png 为 1536×1024 RGBA 原始输出，原样复制，运行时按八个 384×512 区域取帧。v2 / v3 草稿和旧素材留在源码，角色包只导出实际引用图集。完整提示词和原始输出记录在天依包 BOTTOM-SEQUENCE-PROMPTS.md / SOURCE.md；此前完整来源档案保留为 SOURCE-HISTORY-v033.md，纳入匹配源码 ZIP。原角色权利说明继续适用。

## 0.35.0 秋千坐姿素材

2026-10-04：内置 image_gen 编辑现有青玉秋千坐姿，生成八姿势图集并修整留白。最终 top-sequence-v2.png 为 1536×1024 RGBA 原始输出，原样接入，运行时按八个 384×512 区域取帧。旧图和 v1 草稿留在源码，导出的角色包仅包含实际引用素材；完整提示词和生成记录见天依包 TOP-SEQUENCE-PROMPTS.md / SOURCE.md。原角色权利说明继续适用，SOURCE-HISTORY-v033.md 随匹配源码 ZIP 保留。
