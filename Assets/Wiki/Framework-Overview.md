# 框架总览与单例系统

框架位于 [Assets/_Project/Scripts/Core/Framework](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework)，是一套与具体业务解耦的通用 Unity 基础设施。本文档先介绍整体设计，再聚焦**单例基类**这一框架的基石。

## 1. 设计理念

- **Manager 单例 + 事件中心**：各子系统以 Manager 形式提供，单例之间通过 `EventCenter` 通信，避免相互硬引用。
- **纯 C# 与 MonoBehaviour 两种单例**：不需要帧/协程的管理器做成纯 C# 单例（继承 `BaseManager`）；需要帧驱动或协程的做成 `MonoBehaviour` 单例（继承 `SingletonAutoMono` / `SingletonMono`）。
- **懒加载**：所有单例首次访问 `.Instance` 时才创建，无需在场景中预挂载。
- **引用计数与缓存**：`ResMgr`、`ABMgr`、`PoolMgr` 等对加载/复用对象做引用计数与字典缓存，避免重复加载。

## 2. 框架模块速查

| 模块 | 主类 | 基类 | 职责 |
| --- | --- | --- | --- |
| Mono | [MonoMgr](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Mono/MonoMgr.cs) | `SingletonAutoMono` | 帧事件分发、协程宿主 |
| 事件 | [EventCenter](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/EventCenter/EventCenter.cs) | `BaseManager` | 事件订阅/派发 |
| 资源(Resources) | [ResMgr](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Res/ResMgr.cs) | `BaseManager` | Resources 加载 + 引用计数 |
| 资源(AB) | [ABMgr](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/AB/ABMgr.cs) | `SingletonAutoMono` | AssetBundle 加载、依赖解析 |
| 资源(AB封装) | [ABResMgr](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/AB/ABResMgr.cs) | `BaseManager` | 对外 AB 加载入口（可切 Editor 模式） |
| 资源(Editor) | [EditorResMgr](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/EditorRes/EditorResMgr.cs) | `BaseManager` | 编辑器免打 AB 加载 |
| 资源(网络) | [UWQResMgr](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/UWQ/UWQResMgr.cs) | `SingletonAutoMono` | UnityWebRequest 下载 |
| UI | [UIMgr](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/UI/UIMgr.cs) | `BaseManager` | UI 层级、面板显隐 |
| 对象池 | [PoolMgr](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Pool/PoolMgr.cs) | `BaseManager` | GameObject 池 + 逻辑对象池 |
| 定时器 | [TimerMgr](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Timer/TimerMgr.cs) | `BaseManager` | 定时任务生命周期 |
| 输入 | [InputMgr](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Input/InputMgr.cs) | `BaseManager` | 键鼠监听、可重绑按键 |
| 音频 | [MusicMgr](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Music/MusicMgr.cs) | `BaseManager` | BGM + 音效 |
| 场景 | [SceneMgr](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Scene/SceneMgr.cs) | `BaseManager` | 同步/异步切场景 |
| 工具 | [EncryptionUtil](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Util/EncryptionUtil.cs) / [MathUtil](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Util/MathUtil.cs) / [TextUtil](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Util/TextUtil.cs) | 静态类 | 加密/数学/文本 |

## 3. 单例基类

单例基类位于 [Singleton/](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Singleton)，提供三种基类以适配不同场景。

### 3.1 `BaseManager<T>` —— 纯 C# 单例

源码：[BaseManager.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Singleton/BaseManager.cs)

适用于**不需要 MonoBehaviour 生命周期**的管理器（如 `EventCenter`、`ResMgr`、`PoolMgr`、`TimerMgr`、`InputMgr`、`MusicMgr`、`SceneMgr`、`ABResMgr`）。

- 泛型约束 `where T : class`。
- 通过反射调用**私有无参构造函数**实例化，强制子类把构造函数设为私有（`private XxxMgr() {}`）。
- 双检锁 + `lockObj` 保证线程安全。
- 子类不能 `new`，只能通过 `Instance` 访问。

> 注意：纯 C# 单例本身没有帧/协程能力，需要这些能力时通过 `MonoMgr.Instance.AddUpdateListener(...)` / `StartCoroutine(...)` 间接获取。例如 `TimerMgr`、`InputMgr`、`MusicMgr` 都这样做。

#### 用法

```csharp
public class MyManager : BaseManager<MyManager>
{
    private MyManager() { }   // 必须私有无参构造

    public void DoSomething() { /* ... */ }
}

// 调用
MyManager.Instance.DoSomething();
```

辅助成员：

- `protected bool InstanceIsNull`：判断当前是否尚未创建实例。
- `protected static readonly object lockObj`：子类可复用的锁对象。

### 3.2 `SingletonAutoMono<T>` —— 自动挂载式 Mono 单例（推荐）

源码：[SingletonAutoMono.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Singleton/SingletonAutoMono.cs)

适用于需要 `MonoBehaviour` 能力（帧、协程、Inspector）但**不想在场景中手动挂载**的管理器（如 `MonoMgr`、`ABMgr`、`UWQResMgr`）。

- 泛型约束 `where T : MonoBehaviour`。
- 首次访问 `Instance` 时：`new GameObject()` → `AddComponent<T>()` → `DontDestroyOnLoad`。
- GameObject 以类名为名，便于在 Hierarchy 识别。

#### 用法

```csharp
public class MyMonoManager : SingletonAutoMono<MyMonoManager>
{
    // 无需写 Awake 单例逻辑，基类已处理
    private void Start() { /* ... */ }
}
```

> 推荐优先使用本基类而非 `SingletonMono`，省去手动挂载步骤，且能保证跨场景唯一。

### 3.3 `SingletonMono<T>` —— 手动挂载式 Mono 单例

源码：[SingletonMono.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Singleton/SingletonMono.cs)

适用于需要在场景/Prefab 中预先配置 Inspector 字段的 Mono 单例。

- 泛型约束 `where T : MonoBehaviour`。
- **必须由外部在场景中创建并挂载脚本**，`Awake` 中 `instance = this as T` 并 `DontDestroyOnLoad`。
- 若已存在实例，新的重复挂载会 `Destroy(this)` 自我销毁，保证唯一性。

#### 用法

```csharp
public class GameManager : SingletonMono<GameManager>
{
    [SerializeField] private int configValue; // 可在 Inspector 配置
}

// 需在场景中创建 GameObject 并挂载 GameManager
```

## 4. 如何选择单例基类

```
是否需要 MonoBehaviour 能力（帧/协程/Inspector）？
│
├─ 否 ──▶ BaseManager<T>           （纯 C#，如 EventCenter）
│            └─ 如需帧/协程：借 MonoMgr 间接获得
│
└─ 是 ──▶ 是否需要在 Inspector 预先配置字段？
              │
              ├─ 否 ──▶ SingletonAutoMono<T> （自动创建，推荐）
              └─ 是 ──▶ SingletonMono<T>      （场景中手动挂载）
```

## 5. 常见陷阱

- **忘记私有构造**：`BaseManager` 子类若不写 `private` 无参构造，反射会失败并打印 `"没有找到对应的无参构造函数"`。
- **构造期访问其它单例**：单例初始化顺序不保证。若在 `XxxMgr` 构造函数里访问 `YyyMgr.Instance`，可能触发链式初始化或循环。建议把"启动逻辑"放到独立的 `Init()` 方法，由入口按顺序调用。
- **`SingletonMono` 未挂载**：`SingletonMono` 派生类若忘记在场景挂载，`Instance` 会一直为 `null`。这也是为何新管理器优先选 `SingletonAutoMono`。
- **跨场景销毁**：三种基类都做了 `DontDestroyOnLoad`，单例 GameObject 会常驻，注意不要在场景切换时误删。
