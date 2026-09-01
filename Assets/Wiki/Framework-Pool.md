# 对象池 Pool

对象池用于复用频繁创建/销毁的对象，降低 GC 与实例化开销。框架提供两种池：**GameObject 池**与**逻辑对象池（纯 C# 对象）**。

源码：
- [PoolMgr.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Pool/PoolMgr.cs)
- [PoolObj.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Pool/PoolObj.cs)

## 1. 架构概览

```
PoolMgr (单例)
  │
  ├─ poolDic: 预制体名 → PoolData        （GameObject 池）
  │     └─ PoolData
  │           ├─ dataStack:  空闲对象栈
  │           ├─ usedList:   使用中对象列表
  │           └─ maxNum:     同屏最大数（来自 PoolObj.maxNum）
  │
  └─ poolObjectDic: 池名 → PoolObject<T>  （逻辑对象池）
        └─ Queue<T>  where T : IPoolObject, new()
```

## 2. GameObject 池

### 2.1 工作流程

**取对象 `GetObj(name)`**：

1. 若池不存在或池空且未达上限：`Instantiate(Resources.Load(name))`，重命名为 `name`。
2. 若池中有空闲对象：`Pop()` 出栈，`SetActive(true)`，记录到 `usedList`。
3. 若池空但已达上限：取出 `usedList[0]`（最久未用的），移到队尾复用。

**还对象 `PushObj(obj)`**：

1. `SetActive(false)`，挂回 `poolObj`（根容器）。
2. 入栈 `dataStack`，从 `usedList` 移除。

> 还对象时按 `obj.name` 路由回对应池，因此**取对象时 `obj.name` 必须保持为预制体原名**，不可随意改名。

### 2.2 上限控制

每个预制体需挂 [PoolObj](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Pool/PoolObj.cs) 组件：

```csharp
public class PoolObj : MonoBehaviour
{
    public int maxNum;   // 该预制体同屏最大存在数
}
```

- 池不存在时首次 `GetObj` 会读取 `PoolObj.maxNum` 作为上限。
- 未达上限时优先新建；达上限后从 `usedList` 回收最久未用对象。

> ⚠️ 若预制体未挂 `PoolObj`，`PoolData` 构造时会打印错误：`"因为使用没有挂载该PoolObj脚本的预制体 池子无法初始化上限数"`。

### 2.3 层级布局

```csharp
public static bool isOpenLayout = true;
```

- `true`（默认）：池根节点 `Pool` 下按预制体名分子节点，Hierarchy 清晰，便于调试。
- `false`：不创建/挂载父节点，节省一层 transform 操作。

## 2.4 API

```csharp
GameObject GetObj(string name);               // 取
void PushObj(GameObject obj);                 // 还（按 obj.name 路由）
void ClearPool();                             // 清空所有池
```

### 2.5 用法

```csharp
// 取
GameObject fx = PoolMgr.Instance.GetObj("Effects/Explosion");
fx.transform.position = hitPoint;

// 还（用完归还，不要 Destroy）
PoolMgr.Instance.PushObj(fx);
```

> 候选预制体需放在 `Assets/Resources/` 下，路径即为 `GetObj` 的 `name`。

## 3. 逻辑对象池

适用于频繁创建/销毁的**纯 C# 数据对象**（如 `TimerItem`）。

### 3.1 接口约束

```csharp
public interface IPoolObject
{
    void ResetInfo();   // 入池前调用，清空状态
}
```

入池对象须实现 `IPoolObject` 并具有无参构造。

### 3.2 API

```csharp
T GetObj<T>(string nameSpace = "") where T : class, IPoolObject, new();
void PushObj<T>(T obj, string nameSpace = "") where T : class, IPoolObject;
```

- 池名 = `nameSpace + "_" + typeof(T).Name`，`nameSpace` 用于区分同名不同用途的对象。
- 取：有缓存则出队，无则 `new T()`。
- 还：调用 `obj.ResetInfo()` 后入队。

### 3.3 用法

```csharp
public class BulletData : IPoolObject
{
    public int damage;

    public void ResetInfo() => damage = 0;
}

var b = PoolMgr.Instance.GetObj<BulletData>();
b.damage = 10;
// 使用...
PoolMgr.Instance.PushObj(b);
```

### 3.4 框架内使用方

- `TimerMgr` 的 `TimerItem` 实现了 `IPoolObject`，定时器对象通过对象池获取/归还。

## 4. 注意事项

- ✅ 归还前**不要 Destroy**对象，否则池中会出现空引用。
- ✅ GameObject 取出后若需改名用于显示，归还前需改回预制体原名，否则无法路由回正确池。
- ✅ 切场景或重开游戏时调用 `ClearPool()` 重置，避免跨场景残留。
- ⚠️ `PoolData.Pop` 在达上限时会复用 `usedList[0]`——若该对象仍被外部引用且未"释放"，复用可能造成逻辑混乱。使用方应在还入池后**立即放弃对该对象的引用**。
- ⚠️ 池内空闲对象处于 `SetActive(false)` 状态，其身上的 `Update` 不会执行，注意不要依赖空闲对象的帧逻辑。
