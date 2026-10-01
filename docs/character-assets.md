# 角色素材与接入（0.5.0）

角色选择：右键或托盘 → 角色选择 → 小猫 / 洛天依（同人）。选择立即生效，保存在个人 settings.json 中。旧配置继续采用小猫。

小猫继续使用现有矢量绘制与眨眼动画。洛天依为单帧透明 PNG，支持现有轻微浮动与文字气泡；本版未生成眨眼、挥手或疲惫帧。两种角色共用额度条、拖动、缩放和详情模式。

## 素材来源

- 文件：src/CutePet.Desktop/Assets/tianyi-fanart-v1.png。
- 日期：2026-10-01。
- 工具：内置 image_gen.imagegen，默认工具模式，transparent_background=true。未使用 CLI / API key 回退。
- 人物设计参考：洛天依灰发、绿瞳及辫发形象，参考 [官方 VOCALOID 产品介绍](https://www.vocaloid.com/products/show/v5l_tianyi)。生成时没有下载或传入官方立绘。
- 本图为非官方 AI 同人图，角色权利说明见 [THIRD_PARTY_NOTICES.md](../THIRD_PARTY_NOTICES.md)。

## 实际生成提示词

```text
Use case: stylized-concept. Asset type: transparent PNG character sprite for a tiny Windows desktop pet. Primary request: a cute chibi fan-art interpretation of Luo Tianyi (洛天依), recognizable gray/silver hair, jade green eyes, looped braided hair buns and short side braids, jade hair ornaments, classic blue-and-white Chinese-inspired modest short dress with blue decorative trim, dark boots. Full-body front view, warm gentle smile, relaxed standing pose, hands held softly beside body. Super-deformed 2.5-head proportions, large expressive head, clean anime line art with restrained cel shading, readable silhouette at 150px tall. Single character only, centered, entire hair and shoes visible, very little transparent padding, no scenery, no ground shadow, no frame, no text, no logos, no watermark. Genuine transparent alpha background. This is an unofficial fan-art sprite, not a UI mockup. No extra characters or props.
```

## 接入方式

CharacterCatalog 定义稳定角色枚举、菜单名称与缓存图片，Preferences 保存并校验 Character 值。PNG 以 WPF Resource 嵌入程序，用 pack URI 加载，解码后冻结并复用。MainWindow 在同一个 PetArt 容器中切换矢量小猫与图片，因此额度条布局与公共浮动动画沿用现有实现。两个菜单都从同一枚举生成选项。

新增图片角色可扩展枚举和目录，加入嵌入资源与显示分支；当前没有外部素材导入、热加载或多帧播放器。

## 验证

51 项界面检查中含角色默认值兼容、无效角色恢复、缓存素材加载、透明像素检查、切换保留原设置与额度、重启恢复、切回小猫。离屏渲染包括两种角色和新角色的四方向及较大尺寸；预览为模拟额度。未据此声称真实菜单鼠标操作和显示器 DPI 切换已验收。
