# 定时器 Timer

定时器基于协程实现，支持**受/不受 `Time.timeScale` 影响**两种模式，并提供间隔回调、总时长结束回调、暂停/继续/重置等完整生命周期控制。

源码：
- [TimerMgr.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Timer/TimerMgr.cs)
- [TimerItem.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Timer/TimerItem.cs)

## 1. 架构概览

```
TimerMgr (BaseManager 单例)
  │
  ├─ 构造时启动两条协程（借 MonoMgr）：
  │     ├─ timer    : 受 timeScale 影响   (WaitForSeconds)
  │     └─ realTimer: 不受影响            (WaitForSecondsRealtime)
  │
  ├─ timerDic       : keyID → TimerItem   （受影响）
  ├─ realTimerDic   : keyID → TimerItem   （不受影响）
  └─ delList        : 本轮待删除的 TimerItem（统一在循环末尾回收）
        │
        ▼
     每 0.1s tick 一次：
       ├─ 累减 intervalTime（间隔回调）
       └─ 累减 allTime（结束回调 → 进 delList → 回收进对象池）
```

## 2. 关键常量

```csharp
private const float intervalTime = 0.1f;   // tick 间隔 0.1s = 100ms
```

- 内部所有时间字段以**毫秒**为单位（`1s = 1000ms`）。
- 每个 tick 扣减 `intervalTime * 1000 = 100ms`。

## 3. TimerItem

源码：[TimerItem.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Timer/TimerItem.cs)

```csharp
public class TimerItem : IPoolObject   // 走逻辑对象池
{
    public int keyID;                  // 唯一 ID
    public UnityAction overCallBack;   // 总时长结束回调
    public UnityAction callBack;       // 每次间隔回调
    public int allTime;                // 剩余总时长(ms)
    public int maxAllTime;             // 初始总时长（重置用）
    public int intervalTime;           // 当前距下次间隔回调(ms)
    public int maxIntervalTime;        // 初始间隔(ms)
    public bool isRuning;               // 是否在计时

    public void InitInfo(int keyID, int allTime, UnityAction overCallBack,
                         int intervalTime = 0, UnityAction callBack = null);
    public void ResetTimer();          // 回到初始时长，恢复运行
    public void ResetInfo();           // IPoolObject 实现，入池前清空回调
}
```

> `TimerItem` 通过 `PoolMgr` 获取/归还，避免反复 `new`。

## 4. 核心 API

```csharp
// 创建定时器，返回唯一 ID
int CreateTimer(
    bool isRealTime,           // 是否不受 Time.timeScale 影响
    int allTime,               // 总时长(ms)
    UnityAction overCallBack,  // 结束回调
    int intervalTime = 0,       // 间隔回调周期(ms)，0 表示不触发间隔回调
    UnityAction callBack = null // 间隔回调
);

void RemoveTimer(int keyID);    // 移除（回收）
void ResetTimer(int keyID);     // 重置回初始时长并恢复运行
void StartTimer(int keyID);     // 继续（isRuning=true）
void StopTimer(int keyID);      // 暂停（isRuning=false）

void Start();                   // 启动两条 tick 协程（构造时已调用）
void Stop();                    // 停止 tick 协程
```

## 5. 用法示例

### 5.1 一次性倒计时

```csharp
int id = TimerMgr.Instance.CreateTimer(
    isRealTime: false,
    allTime: 5000,
    overCallBack: () => Debug.Log("5 秒到！"));
```

### 5.2 周期性心跳（每秒一次）

```csharp
int id = TimerMgr.Instance.CreateTimer(
    isRealTime: false,
    allTime: 60000,           // 60 秒后结束
    overCallBack: () => Debug.Log("结束"),
    intervalTime: 1000,      // 每 1000ms
    callBack:    () => Debug.Log("滴答"));
```

### 5.3 暂停 / 继续 / 重置 / 移除

```csharp
TimerMgr.Instance.StopTimer(id);     // 暂停
TimerMgr.Instance.StartTimer(id);    // 继续
TimerMgr.Instance.ResetTimer(id);    // 重置为初始时长
TimerMgr.Instance.RemoveTimer(id);   // 彻底移除
```

### 5.4 不受慢动作影响的真实时间定时器

```csharp
// 游戏开启 slow motion（timeScale<1）时仍按真实时间计时
int id = TimerMgr.Instance.CreateTimer(isRealTime: true, allTime: 3000, ...);
```

## 6. 设计细节

### 6.1 为何使用 0.1s tick 而非每帧？

- 统一调度：所有定时器共用两条协程，避免每个定时器开一条协程带来的开销。
- 精度足够：0.1s 误差对大多数游戏逻辑（UI 倒计时、技能冷却）可接受。

### 6.2 删除时机

- 总时长结束 → 触发 `overCallBack` → 加入 `delList`。
- 在本轮 tick 结束后统一 `timerDic.Remove(keyID)` 并 `PoolMgr.PushObj(item)` 归还对象池。
- 不能在遍历中直接删除字典，故引入 `delList` 缓冲。

### 6.3 借助 MonoMgr

`TimerMgr` 是纯 C# 单例（`BaseManager`），自身无协程能力，启动时通过 `MonoMgr.Instance.StartCoroutine(...)` 驱动。这也是框架"纯 C# 单例借 Mono 获得帧/协程"模式的典型示例。

## 7. 注意事项

- ✅ 监听结束/间隔的回调内不要做重逻辑（阻塞 tick 协程），耗时操作应异步化。
- ✅ 创建后保存 `keyID`，必要时主动 `RemoveTimer`，避免对象长期驻留。
- ⚠️ `intervalTime = 0` 时不会触发间隔回调（`item.callBack != null` 仍为前提，但 `intervalTime <= 0` 的判断在 `callBack` 之后，逻辑上 `intervalTime=0` 时每 tick 都会触发——实际使用请传 `>0` 值以符合预期）。
- ⚠️ 回调捕获外部变量时注意生命周期：定时器结束后对象可能已被销毁，回调内访问 Unity 对象前判空。
- ⚠️ `ResetInfo()` 在归还对象池时清空 `overCallBack` 与 `callBack`，但**不会清空调用方持有的 `keyID` 引用**——重置/移除时仍需传入有效 ID。
