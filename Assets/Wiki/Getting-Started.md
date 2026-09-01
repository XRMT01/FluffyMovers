# 快速开始

本指南帮助你把 FluffyMovers 工程跑起来，并了解如何调用框架能力。

## 1. 环境要求

- **Unity Editor**：建议使用与 `Packages/manifest.json` 匹配的版本（Cinemachine 2.10.x、Timeline 1.7.x 通常对应 Unity 2021/2022 LTS）。
- **操作系统**：Windows / macOS 均可。
- **IDE**：Visual Studio / Rider / VS Code（需 C# 拓展）。

## 2. 打开工程

1. 使用 Unity Hub → Add → 选择 `FluffyMovers/` 根目录。
2. 选择匹配的 Unity 版本打开，首次会编译 `Library/` 与导入资源，耗时较长。
3. 打开后，框架脚本位于 [Assets/_Project/Scripts/Core/Framework](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework)。

## 3. 首次运行

1. 打开场景 [Assets/_Project/Scenes/Level_01_CharacterController.unity](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scenes/Level_01_CharacterController.unity)。
2. 点击 ▶ 运行。
3. 该场景用于验证角色控制与 Q 弹形变效果。

> 框架管理器（`UIMgr`、`ABMgr`、`TimerMgr` 等）均为懒加载单例：**首次访问 `XxxMgr.Instance` 时自动初始化**，无需在场景中手动挂载（`SingletonAutoMono` 派生类会自动创建 GameObject）。

## 4. 框架调用速查

### 显示一个 UI 面板

```csharp
// 业务面板需继承 BasePanel，类名必须与 AB 包中预制体名一致
UIMgr.Instance.ShowPanel<BeginPanel>(E_UILayer.Middle, panel =>
{
    // panel 为加载完成的面板实例
    panel.ShowMe(); // 通常框架内部已调用，此处仅作示意
});
```

### 监听一个事件

```csharp
// 监听
EventCenter.Instance.AddEventListener<float>(E_EventType.E_SceneLoadChange, OnProgress);
// 触发（某处）
EventCenter.Instance.EventTrigger(E_EventType.E_SceneLoadChange, 0.5f);
// 移除
EventCenter.Instance.RemoveEventListener<float>(E_EventType.E_SceneLoadChange, OnProgress);
```

### 异步加载 AB 资源

```csharp
ABResMgr.Instance.LoadResAsync<GameObject>("ui", "BeginPanel", obj =>
{
    Instantiate(obj);
});
```

> 开发期可在 `ABResMgr` 内将 `isDebug` 置 `true`，改走 `EditorResMgr`，免打 AB 包即可加载（仅 Editor 生效）。

### 创建一个定时器

```csharp
int id = TimerMgr.Instance.CreateTimer(
    isRealTime: false,      // 是否不受 Time.timeScale 影响
    allTime: 5000,           // 总时长 5000ms
    overCallBack: () => Debug.Log("时间到"),
    intervalTime: 1000,     // 每 1000ms 触发一次
    callBack:    () => Debug.Log("滴答")
);
// 暂停 / 继续 / 重置 / 移除
TimerMgr.Instance.StopTimer(id);
TimerMgr.Instance.StartTimer(id);
TimerMgr.Instance.ResetTimer(id);
TimerMgr.Instance.RemoveTimer(id);
```

### 从对象池取/还 GameObject

```csharp
GameObject obj = PoolMgr.Instance.GetObj("Effects/Explosion");
// 使用...
PoolMgr.Instance.PushObj(obj); // 归还（按 obj.name 路由回对应池）
```

> 候选预制体需挂在 `Resources/` 下，并带 `PoolObj` 组件声明 `maxNum`。

## 5. AB 包准备（打包后运行）

运行时 `ABMgr` 从 `Application.streamingAssetsPath` 加载主包，主包名按平台自动切换（`PC` / `Android` / `IOS`）。

1. 使用 `AssetBundles-Browser`（已随工程引入）或 Unity 原生 AB 构建工具打包。
2. 将产出的 AB 包放入 `Assets/StreamingAssets/`。
3. 包名约定：UI 面板统一进 `ui` 包，音频进 `music`/`sound` 包（详见 [资源管理](./Framework-Resources.md)）。

## 6. 下一步阅读

- [整体架构](./Architecture.md) —— 理解模块划分。
- [框架总览](./Framework-Overview.md) —— 单例基类如何选择。
- [开发约定](./Conventions.md) —— 新增代码前请先阅读。
