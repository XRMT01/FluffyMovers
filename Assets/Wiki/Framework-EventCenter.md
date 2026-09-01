# 事件中心 EventCenter

事件中心是框架的**模块解耦中枢**，让各 Manager 与业务层之间无需互相硬引用即可通信。

源码：[EventCenter.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/EventCenter/EventCenter.cs)
事件枚举：[E_EventType.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/EventCenter/E_EventType.cs)

## 1. 设计要点

- 继承 `BaseManager<EventCenter>`，纯 C# 单例。
- 内部用 `Dictionary<E_EventType, EventInfoBase>` 维护"事件名 → 委托链"。
- 通过**泛型**区分带参/无参事件，避免装箱，保留类型安全。
- 委托类型使用 `UnityAction`（`UnityEngine.Events`），与 UI 事件风格一致。

## 2. 事件枚举

源码：[E_EventType.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/EventCenter/E_EventType.cs)

| 枚举值 | 携带参数 | 说明 |
| --- | --- | --- |
| `E_Monster_Dead` | — | 怪物死亡事件 |
| `E_Player_GetReward` | `int` | 玩家获得奖励（带奖励 ID） |
| `E_Test` | — | 测试事件 |
| `E_SceneLoadChange` | `float` | 异步场景加载进度（0~1） |
| `E_Input_Skill1` / `Skill2` / `Skill3` | — | 输入系统广播的技能按键 |
| `E_Input_Horizontal` | `float` | 水平轴值 -1~1 |
| `E_Input_Vertical` | `float` | 垂直轴值 -1~1 |

> 新增事件：在 `E_EventType` 中追加枚举即可，无需改动 `EventCenter` 本身。

## 3. 核心 API

```csharp
// 触发（带参）
void EventTrigger<T>(E_EventType eventName, T info);

// 触发（无参）
void EventTrigger(E_EventType eventName);

// 监听（带参）
void AddEventListener<T>(E_EventType eventName, UnityAction<T> func);

// 监听（无参）
void AddEventListener(E_EventType eventName, UnityAction func);

// 移除（带参）
void RemoveEventListener<T>(E_EventType eventName, UnityAction<T> func);

// 移除（无参）
void RemoveEventListener(E_EventType eventName, UnityAction func);

// 清空所有
void Clear();

// 清空指定事件
void Claer(E_EventType eventName);   // 注意：源码中存在拼写 "Claer"
```

> ⚠️ 源码中清空单个事件的方法名拼写为 `Claer`（非 `Clear`）。调用时请用实际拼写，或后续重构修正。

## 4. 用法示例

### 4.1 监听与触发

```csharp
using UnityEngine;

public class RewardUI : MonoBehaviour
{
    void OnEnable()
    {
        EventCenter.Instance.AddEventListener<int>(
            E_EventType.E_Player_GetReward, OnGetReward);
    }

    void OnDisable()
    {
        // 监听与移除必须成对，且委托引用一致
        EventCenter.Instance.RemoveEventListener<int>(
            E_EventType.E_Player_GetReward, OnGetReward);
    }

    void OnGetReward(int rewardId)
    {
        Debug.Log($"获得奖励 {rewardId}");
    }
}

// 触发处（如拾取逻辑）
EventCenter.Instance.EventTrigger(E_EventType.E_Player_GetReward, 1024);
```

### 4.2 场景加载进度监听

```csharp
EventCenter.Instance.AddEventListener<float>(
    E_EventType.E_SceneLoadChange, progress =>
    {
        loadingBar.value = progress;
    });

// SceneMgr.LoadSceneAsyn 内部会持续触发该事件
SceneMgr.Instance.LoadSceneAsyn("Level_02");
```

## 5. 内部数据结构

```csharp
public abstract class EventInfoBase { }              // 多态基类
public class EventInfo<T> : EventInfoBase             // 带参容器
{
    public UnityAction<T> actions;
    public EventInfo(UnityAction<T> action) => actions += action;
}
public class EventInfo : EventInfoBase                // 无参容器
{
    public UnityAction actions;
    public EventInfo(UnityAction action) => actions += action;
}
```

- 同一事件可挂多个监听者（`+=`）。
- 触发时若字典无该键，直接忽略（不会报错）。

## 6. 框架内的使用

- **`InputMgr`**：检测到按键/鼠标后 `EventCenter.Instance.EventTrigger(eventType)` 广播；每帧广播 `E_Input_Horizontal` / `E_Input_Vertical`。
- **`SceneMgr`**：异步加载场景时每帧 `EventTrigger<float>(E_SceneLoadChange, ao.progress)` 上报进度。

## 7. 最佳实践与陷阱

- ✅ 监听/移除成对出现：在 `OnEnable`/`OnDisable` 或 `OnDestroy` 中配对调用，避免悬空委托。
- ✅ 始终通过 `RemoveEventListener` 传入**同一委托实例**移除，匿名 lambda 难以正确移除，应改为具名方法。
- ✅ 事件参数尽量用值类型或不可变对象，避免监听者修改共享引用造成副作用。
- ⚠️ 当前 `EventInfo` 容器在首次 `AddEventListener` 时通过构造函数 `+=` 挂载首个监听者，逻辑等价正常，但阅读时需注意其构造即挂载的设计。
- ⚠️ 切场景/重开游戏时若需重置通信状态，可调用 `EventCenter.Instance.Clear()`，但要确保不会清掉仍需保留的全局监听。
