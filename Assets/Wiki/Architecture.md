# 整体架构与目录结构

本文档描述 FluffyMovers 的工程布局、模块划分与运行时架构，帮助新成员快速建立全局认知。

## 1. 工程目录结构

工程根目录：`FluffyMovers/`

```
FluffyMovers/
├── Assets/                          # Unity 资产根目录
│   ├── _Project/                    # 项目实际资产（自管理目录，区别于第三方插件）
│   │   ├── Scripts/                 # 全部 C# 脚本
│   │   │   ├── Core/                # 核心层
│   │   │   │   ├── Framework/       # 自研框架（详见下文）
│   │   │   │   └── ElasticEffect.cs  # Q 弹形变 + 角色移动效果
│   │   │   ├── Character/            # 角色控制业务
│   │   │   │   └── CharacterController.cs
│   │   │   ├── Gameplay/            # 玩法（占位，待实现）
│   │   │   ├── Level/               # 关卡（占位，待实现）
│   │   │   ├── Network/             # 网络（占位，待实现）
│   │   │   ├── UI/                  # 业务 UI 面板（占位，待实现）
│   │   │   └── Utils/               # 业务工具（占位，待实现）
│   │   ├── Art/                     # 美术资源
│   │   │   ├── UI/                  # UI 预制体（如 BeginPanel.prefab）
│   │   │   ├── Materials/           # 材质（Black.mat / Red.mat）
│   │   │   ├── Cargo/               # 货物（.meta 占位）
│   │   │   ├── Characters/          # 角色
│   │   │   ├── Effects/             # 特效
│   │   │   ├── Environment/         # 环境
│   │   │   └── Textures/            # 贴图
│   │   └── Scenes/                  # 场景
│   │       └── Level_01_CharacterController.unity
│   ├── Resources/                  # Resources 系统资源（ResMgr 使用）
│   ├── AssetBundles-Browser-master/ # 第三方：AB Browser 工具
│   └── ...                          # Unity 默认目录
├── Packages/                        # Unity 包清单
│   └── manifest.json                # 依赖声明
├── ProjectSettings/                 # Unity 工程设置
└── Library/                         # Unity 本地缓存（不入库）
```

> **约定**：所有自研内容统一放在 `_Project/` 下，与第三方插件（如 `AssetBundles-Browser-master`）严格隔离，便于迁移与版本管理。

## 2. 脚本目录职责

| 目录 | 职责 | 现状 |
| --- | --- | --- |
| `_Project/Scripts/Core/Framework` | 自研通用框架，与具体业务无关 | 完善 |
| `_Project/Scripts/Core` | 核心层非框架脚本（如 `ElasticEffect`） | 实验中 |
| `_Project/Scripts/Character` | 角色控制器 | 早期 |
| `_Project/Scripts/Gameplay` | 核心玩法 | 占位 |
| `_Project/Scripts/Level` | 关卡逻辑 | 占位 |
| `_Project/Scripts/Network` | 网络通信 | 占位 |
| `_Project/Scripts/UI` | 业务 UI 面板脚本（继承 `BasePanel`） | 占位 |
| `_Project/Scripts/Utils` | 业务专用工具 | 占位 |

## 3. 框架模块一览

框架位于 [Assets/_Project/Scripts/Core/Framework](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework)，按子系统分目录：

```
Framework/
├── Singleton/      # 单例基类（BaseManager / SingletonMono / SingletonAutoMono）
├── Mono/            # MonoMgr —— 帧事件分发中枢
├── EventCenter/     # 事件中心 + 事件枚举 E_EventType
├── Res/             # ResMgr —— Resources 资源加载（带引用计数）
├── AB/              # ABMgr / ABResMgr —— AssetBundle 加载与对外封装
├── EditorRes/       # EditorResMgr —— 编辑器模式资源加载（仅 Editor）
├── UWQ/             # UWQResMgr —— UnityWebRequest 通用资源下载
├── UI/              # UIMgr / BasePanel —— UI 层级与面板基类
├── Pool/            # PoolMgr / PoolData / PoolObj —— 对象池
├── Timer/           # TimerMgr / TimerItem —— 定时任务
├── Input/           # InputMgr / InputInfo —— 输入监听与重绑
├── Music/           # MusicMgr —— 背景音乐与音效
├── Scene/           # SceneMgr —— 场景切换
└── Util/            # EncryptionUtil / MathUtil / TextUtil
```

## 4. 运行时架构

### 4.1 分层

```
┌──────────────────────────────────────────────┐
│            业务层 (Character / Gameplay ...)   │
├──────────────────────────────────────────────┤
│   框架层 (Framework: UI / Pool / Timer ...)   │
├──────────────────────────────────────────────┤
│        基础设施 (Singleton / Mono / Event)     │
├──────────────────────────────────────────────┤
│             Unity 引擎 API                     │
└──────────────────────────────────────────────┘
```

### 4.2 核心运行时关系

- **MonoMgr** 是框架的"心跳"：它持有唯一的 `MonoBehaviour` 单例，对外提供 `Update / FixedUpdate / LateUpdate` 事件订阅。`InputMgr`、`MusicMgr`、`TimerMgr` 等纯 C# 单例通过它获得帧驱动与协程能力。
- **EventCenter** 是模块间的"总线"：`InputMgr` 检测到输入后通过 `EventCenter.EventTrigger` 广播；`SceneMgr` 异步加载场景时通过事件上报进度；业务层监听所需事件即可。
- **Singleton 体系**：纯 C# 管理器继承 `BaseManager<T>`；需要帧/协程的管理器继承 `SingletonAutoMono<T>`（自动创建 GameObject）或 `SingletonMono<T>`（需手动挂载）。

### 4.3 资源加载链路

```
业务代码
   │
   ▼
ABResMgr.LoadResAsync  ──(Editor+isDebug)──▶ EditorResMgr   （开发期免打 AB）
   │
   └─(运行时 / 打包后)──▶ ABMgr.LoadResAsync
                              │
                              ├─ LoadMainAB()         主包
                              ├─ manifest.GetAllDependencies()  依赖解析
                              └─ AssetBundle.LoadFromFile(Async)  加载并缓存
```

> `ResMgr` 是独立的 `Resources` 资源通道，带引用计数与异步合并加载，与 AB 体系并存。`UIMgr`、`MusicMgr` 等 UI/音频资源走 AB 体系；`UIMgr` 初始化所需的 `UICamera/Canvas/EventSystem` 走 `ResMgr`。

## 5. Unity 包依赖

来自 [Packages/manifest.json](file:///e:/Project/FluffyMovers/FluffyMovers/Packages/manifest.json)：

| 包 | 版本 | 用途 |
| --- | --- | --- |
| `com.unity.cinemachine` | 2.10.7 | 摄像机系统 |
| `com.unity.textmeshpro` | 3.0.7 | 文本渲染 |
| `com.unity.timeline` | 1.7.7 | 时间线 |
| `com.unity.ugui` | 1.0.0 | UI 系统 |
| `com.unity.visualscripting` | 1.9.4 | 可视化脚本 |
| `com.unity.feature.development` | 1.0.1 | 开发工具集 |
| `com.unity.modules.*` | 1.0.0 | 引擎模块（physics/audio/assetbundle 等） |

## 6. 当前阶段说明

- 框架已具备完整能力，可支撑后续业务开发。
- 业务侧目前仅角色控制与 Q 弹效果有实现，`Gameplay / Level / Network / UI / Utils` 为占位空目录，待按规范填充。
- 部分脚本命名与文件名不完全一致（如 `CharacterController.cs` 内类名为 `CharacterCamera`、`ElasticEffect.cs` 内类名为 `qt`），属实验性代码，后续需按 [开发约定](./Conventions.md) 整理。
