# 开发约定与最佳实践

本文档汇总 FluffyMovers 的命名、目录、代码组织约定，新增代码前请先阅读。框架既有部分（如方法拼写 `Claer`/`UnLoackValue`）属历史遗留，新代码须遵循本约定。

## 1. 目录约定

- 所有自研内容统一放在 `Assets/_Project/`，与第三方插件（`AssetBundles-Browser-master` 等）隔离。
- 脚本按职责归类到 `_Project/Scripts/` 子目录：

| 目录 | 放置内容 |
| --- | --- |
| `Core/Framework` | 通用框架（与业务无关，可复用） |
| `Core` | 核心层非框架脚本（如全局效果） |
| `Character` | 角色控制 |
| `Gameplay` | 核心玩法（敌人、道具、规则等） |
| `Level` | 关卡逻辑 |
| `Network` | 网络通信 |
| `UI` | 业务 UI 面板（继承 `BasePanel`） |
| `Utils` | 业务专用工具（区别于框架 `Util`） |

- 美术资源放 `_Project/Art/`，按类别分子目录（`UI`、`Characters`、`Effects`、`Environment`、`Textures`、`Materials`、`Cargo`）。
- 场景放 `_Project/Scenes/`，命名 `Level_序号_用途`。

## 2. 命名约定

### 2.1 通用

- **PascalCase**：类名、方法名、公共属性、公共字段、命名空间。
- **camelCase**：局部变量、私有字段。
- **接口** 以 `I` 开头（如 `IPoolObject`）。
- **枚举** 以 `E_` 开头（如 `E_EventType`、`E_UILayer`），枚举值同样 `E_` 前缀（如 `E_Input_Skill1`）。
- 私有静态字段可用 `s_` 前缀，私有实例字段可用 `m_` 前缀（当前框架未严格使用，新代码可自选）。

### 2.2 文件名 = 类名（强制）

- 一个脚本文件一个主类，**文件名与 public 类名完全一致**。
- 现存反例：`CharacterController.cs`（类名 `CharacterCamera`）、`ElasticEffect.cs`（类名 `qt`），属待整改项，**新代码不得再犯**。

### 2.3 UI 面板

- 面板脚本名 = AB 资源中预制体名（如 `BeginPanel.cs` ↔ `BeginPanel.prefab`）。
- 控件命名唯一且不与 `BasePanel.defaultNameList`（`Image`/`Text (TMP)`/`Label` 等）冲突，建议前缀语义化：`Btn_` / `Txt_` / `Sld_` / `Tog_` / `Img_`。

### 2.4 AB 包命名

| 用途 | 包名 |
| --- | --- |
| UI 面板 | `ui` |
| 背景音乐 | `music` |
| 音效 | `sound` |
| 其它 | 按模块小写命名，如 `characters`、`environment` |

### 2.5 管理器

- 管理器类名以 `Mgr` 结尾（`UIMgr`、`PoolMgr`、`TimerMgr`…）。
- 单例私有构造：`private XxxMgr() { }`。
- 不要在构造函数中访问其它单例，启动逻辑放 `Init()` 由入口显式调用。

## 3. 单例选择

详见 [框架总览](./Framework-Overview.md#4-如何选择单例基类)。简记：

- 纯 C# 管理器 → `BaseManager<T>`，需要帧/协程时借 `MonoMgr`。
- 需 MonoBehaviour 但无需 Inspector 配置 → `SingletonAutoMono<T>`（推荐）。
- 需在场景中预配 Inspector → `SingletonMono<T>`。

## 4. 事件使用

- 模块间通信优先用 `EventCenter`，避免相互硬引用。
- 监听与移除**成对**，在 `OnEnable/OnDisable` 或 `Init/Dispose` 中配对。
- 用**具名方法**而非匿名 lambda 注册监听，便于移除。
- 新增事件在 `E_EventType` 加枚举，不要复用已有事件名传不同含义的参数。

## 5. 资源加载

- 业务侧统一走 `ABResMgr.LoadResAsync`（不要直接调 `ABMgr`），便于开发期切 Editor 模式。
- 开发期 `ABResMgr.isDebug = true` 走 `EditorResMgr`，源资产放 `Assets/Editor/ArtRes/{包名}/{资源名}.{后缀}`，目录结构须与运行时 AB 包名映射一致。
- 打包前确保 `Assets/StreamingAssets/` 已包含按平台（`PC`/`Android`/`IOS`）命名的 AB 主包。
- 简单常驻预制体（`UICamera`/`Canvas`/`EventSystem`、对象池预制体）放 `Assets/Resources/`，用 `ResMgr` 或 `PoolMgr` 加载。

## 6. 对象池

- 频繁创建/销毁的 GameObject 与逻辑对象优先走对象池。
- 预制体挂 `PoolObj` 组件声明 `maxNum`。
- 归还前不要 `Destroy`，且确保 `obj.name` 为预制体原名（用于池路由）。
- 退场景或重开游戏调用 `PoolMgr.ClearPool()` 重置。

## 7. 音频

- BGM 用 `MusicMgr.PlayBKMusic`，音效用 `MusicMgr.PlaySound`。
- **退场景/退游戏前调用 `MusicMgr.ClearSound()`**，释放音效载体与 clip 引用。

## 8. 注释与文档

- 公共 API 用 `/// <summary>` XML 注释，IDE 可悬浮显示。
- 复杂逻辑用行内注释说明"为什么"，而非"做什么"。
- 源码文件统一使用 **UTF-8（含 BOM 或无 BOM 均可）**，避免 GBK 导致注释乱码（当前框架部分文件存在此问题）。

## 9. 已知待整改项

| 问题 | 位置 | 建议 |
| --- | --- | --- |
| 方法名拼写 `Claer` | `EventCenter.Claer(E_EventType)` | 重命名为 `Clear`，全局替换调用 |
| 方法名拼写 `UnLoackValue` | `EncryptionUtil.UnLoackValue` | 重命名为 `UnlockValue` |
| 文件名 ≠ 类名 | `CharacterController.cs`(`CharacterCamera`)、`ElasticEffect.cs`(`qt`) | 重命名文件或类 |
| 类名 `qt` 无语义 | `ElasticEffect.cs` | 重命名为 `JellyCharacter` 等 |
| `[SerializeField] private` 不完整 | `CharacterController.cs` | 补全或删除占位 |
| `ABResMgr.isDebug` 私有 | `ABResMgr.cs` | 暴露为菜单开关或配置 |
| 注释乱码（GBK 编码） | 多个框架文件 | 转 UTF-8 重写注释 |

## 10. 提交前自检清单

- [ ] 文件名与 public 类名一致。
- [ ] 新增管理器遵循单例规范（私有构造、不在构造中访问其它单例）。
- [ ] 事件监听有对应移除。
- [ ] 对象池取/还成对，归还前未 Destroy 且 name 未改。
- [ ] 音效场景退出前 `ClearSound()`。
- [ ] 资源路径符合 `Resources` / `Editor/ArtRes` / `StreamingAssets` 约定。
- [ ] 注释为 UTF-8，无乱码。
- [ ] 无 `[Obsolete]` 标注的 API 被新代码调用。
