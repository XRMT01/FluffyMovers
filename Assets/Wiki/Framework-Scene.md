# 场景管理 Scene

场景管理器封装 Unity 场景加载，提供同步与异步两种方式，异步加载时通过事件中心上报进度。

源码：[SceneMgr.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Scene/SceneMgr.cs)

## 1. 核心 API

```csharp
// 同步加载
void LoadScene(string name, UnityAction callBack = null);

// 异步加载（通过 EventCenter 上报进度）
void LoadSceneAsyn(string name, UnityAction callBack = null);
```

## 2. 异步加载流程

源码：[SceneMgr.ReallyLoadSceneAsyn](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Scene/SceneMgr.cs#L30-L45)

```csharp
AsyncOperation ao = SceneManager.LoadSceneAsync(name);
while (!ao.isDone)
{
    EventCenter.Instance.EventTrigger<float>(E_EventType.E_SceneLoadChange, ao.progress);
    yield return 0;   // 每帧上报一次
}
EventCenter.Instance.EventTrigger<float>(E_EventType.E_SceneLoadChange, 1);  // 确保最终上报 1
callBack?.Invoke();
```

## 3. 用法示例

### 3.1 同步切场景

```csharp
SceneMgr.Instance.LoadScene("Level_02", () =>
{
    Debug.Log("场景已加载");
});
```

### 3.2 异步切场景 + 进度 UI

```csharp
// 监听进度
EventCenter.Instance.AddEventListener<float>(
    E_EventType.E_SceneLoadChange, p => loadingBar.value = p);

// 触发
SceneMgr.Instance.LoadSceneAsyn("Level_02", () =>
{
    EventCenter.Instance.RemoveEventListener<float>(
        E_EventType.E_SceneLoadChange, p => loadingBar.value = p);
    UIMgr.Instance.HidePanel<LoadingPanel>(isDestory: true);
});
```

## 4. 注意事项

- ✅ 异步加载期间 UI 仍可响应，适合做 Loading 面板。
- ⚠️ `EventTrigger` 上报的 `ao.progress` 在 Unity 中异步加载完成前最大到 `0.9`，需手动补 `1`（源码已补）。
- ⚠️ 监听 `E_SceneLoadChange` 的回调应在切换完成后移除，避免重复挂载。
- ⚠️ 同步 `LoadScene` 会立即销毁当前场景对象，回调中若引用当前场景物体会失效；建议回调逻辑只做不依赖旧场景对象的初始化。
