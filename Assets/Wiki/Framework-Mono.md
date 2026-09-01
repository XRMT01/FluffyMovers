# Mono 管理

`MonoMgr` 是框架的**心跳中枢**：它是一个常驻 `MonoBehaviour` 单例，对外分发 `Update / FixedUpdate / LateUpdate` 帧事件，并提供协程宿主。所有纯 C# 单例（`BaseManager` 派生类）需要帧驱动或协程时，都通过它间接获得能力。

源码：[MonoMgr.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Mono/MonoMgr.cs)

## 1. 设计动机

`BaseManager` 派生的管理器是纯 C# 对象，无法使用 `Update`、`StartCoroutine` 等 `MonoBehaviour` 能力。若每个管理器都做成 `MonoBehaviour` 单例会导致场景中大量常驻 GameObject。`MonoMgr` 作为**唯一帧入口**，统一驱动所有需要帧逻辑的纯 C# 管理器。

## 2. 核心 API

```csharp
void AddUpdateListener(UnityAction updateFun);         // 订阅 Update
void RemoveUpdateListener(UnityAction updateFun);

void AddFixedUpdateListener(UnityAction updateFun);    // 订阅 FixedUpdate
void RemoveFixedUpdateListener(UnityAction updateFun);

void AddLateUpdateListener(UnityAction updateFun);     // 订阅 LateUpdate
void RemoveLateUpdateListener(UnityAction updateFun);
```

> 还可借助 `MonoBehaviour` 的协程能力，通过 `MonoMgr.Instance.StartCoroutine(...)` / `StopCoroutine(...)` 启停协程。

## 3. 内部实现

```csharp
private event UnityAction updateEvent;
private event UnityAction fixedUpdateEvent;
private event UnityAction lateUpdateEvent;

private void Update()       => updateEvent?.Invoke();
private void FixedUpdate()  => fixedUpdateEvent?.Invoke();
private void LateUpdate()   => lateUpdateEvent?.Invoke();
```

使用 `event` 关键字，外部只能 `+=` / `-=`，无法直接赋值或触发。

## 4. 框架内使用方

| 管理器 | 用途 |
| --- | --- |
| [InputMgr](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Input/InputMgr.cs) | 构造时 `AddUpdateListener(InputUpdate)`，每帧检测输入 |
| [MusicMgr](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Music/MusicMgr.cs) | 构造时 `AddFixedUpdateListener(Update)`，物理帧检查音效回收 |
| [TimerMgr](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Timer/TimerMgr.cs) | 通过 `MonoMgr.Instance.StartCoroutine` 启动 tick 协程 |
| [ResMgr](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Res/ResMgr.cs) | 异步加载协程通过 `MonoMgr.Instance.StartCoroutine` 启动 |
| [SceneMgr](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Scene/SceneMgr.cs) | 异步场景加载协程同上 |

## 5. 用法示例

```csharp
public class EnemySpawner : BaseManager<EnemySpawner>
{
    private EnemySpawner() { }

    public void Init()
    {
        MonoMgr.Instance.AddUpdateListener(OnUpdate);
    }

    private void OnUpdate()
    {
        // 每帧生成/移动逻辑
    }

    public void Shutdown()
    {
        MonoMgr.Instance.RemoveUpdateListener(OnUpdate);
    }
}
```

## 6. 注意事项

- ✅ 监听与移除成对：在 `Init`/`Dispose` 或 `OnEnable`/`OnDisable` 中配对，避免内存泄漏与悬空回调。
- ✅ 长生命周期监听优先用**具名方法**而非匿名 lambda，便于移除。
- ⚠️ `MonoMgr` 自身继承 `SingletonAutoMono`，首次访问时自动创建常驻 GameObject，场景切换不丢失。
- ⚠️ 帧回调内避免重逻辑（每帧执行），耗时操作应放入协程或异步。
