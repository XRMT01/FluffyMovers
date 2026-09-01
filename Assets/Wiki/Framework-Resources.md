# 资源管理

框架提供**四条资源加载通道**，覆盖从开发期到运行时、从本地到网络的各类需求。

| 通道 | 主类 | 适用场景 | 资源位置 |
| --- | --- | --- | --- |
| Resources | [ResMgr](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Res/ResMgr.cs) | 简单常驻资源（如 UICamera/Canvas/EventSystem 预制体） | `Assets/Resources/` |
| AssetBundle | [ABMgr](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/AB/ABMgr.cs) | 运行时/打包后的主要资源通道 | `StreamingAssets/` |
| AB 封装入口 | [ABResMgr](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/AB/ABResMgr.cs) | 业务层统一调用 AB（可切 Editor 模式） | 同 ABMgr |
| Editor 模式 | [EditorResMgr](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/EditorRes/EditorResMgr.cs) | 开发期免打 AB 直接加载源资产 | `Assets/Editor/ArtRes/` |
| 网络 | [UWQResMgr](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/UWQ/UWQResMgr.cs) | 通过 UnityWebRequest 下载远端资源 | http/ftp/file 协议 |

---

## 1. ResMgr —— Resources 资源管理器

源码：[ResMgr.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Res/ResMgr.cs)

### 1.1 职责

- 封装 `Resources.Load` / `Resources.LoadAsync`。
- 通过 `Dictionary<string, ResInfoBase>` 缓存已加载资源，避免重复加载。
- 维护**引用计数**，支持按需卸载。
- 异步加载时合并并发请求：同一资源被多次异步请求，只走一次实际加载，回调统一触发。

### 1.2 资源标识

资源唯一 ID = `路径 + "_" + 类型名`，例如 `"UI/Canvas_GameObject"`。同一路径不同类型被视为不同资源。

### 1.3 核心 API

```csharp
// 同步加载
T Load<T>(string path) where T : UnityEngine.Object;

// 异步加载（回调式）
void LoadAsync<T>(string path, UnityAction<T> callBack) where T : UnityEngine.Object;

// 卸载（引用计数 -1，归零且 isDel 时真正卸载）
void UnloadAsset<T>(string path, bool isDel = false,
                    UnityAction<T> callBack = null, bool isSub = true);

// 异步卸载未使用资源
void UnloadUnusedAssets(UnityAction callBack);

// 查询引用计数
int GetRefCount<T>(string path);

// 清空缓存字典
void ClearDic(UnityAction callBack);
```

> ⚠️ 源码中还有一个 `[Obsolete]` 的 `LoadAsync(string, Type, UnityAction<Object>)` 重载，作者标注其与泛型版混用易导致同路径资源重复加载，**请使用泛型版本**。

### 1.4 引用计数机制

- 每次 `Load` / `LoadAsync` 成功 → `refCount +1`。
- `UnloadAsset(isSub:true)` → `refCount -1`；若 `refCount < 0` 会打印错误日志，提示使用与卸载不配对。
- `refCount == 0` 且 `isDel == true` 时，从字典移除并调用 `Resources.UnloadAsset`。
- 资源加载完成后若发现 `refCount == 0`（异步期间被提前卸载），会按 `isDel` 决定是否真正卸载，避免内存泄漏。
- 异步加载中途若改用同步 `Load`，会停止协程、补同步加载并触发已挂回调。

### 1.5 用法

```csharp
// 同步
GameObject cam = ResMgr.Instance.Load<GameObject>("UI/UICamera");

// 异步
ResMgr.Instance.LoadAsync<AudioClip>("Audio/beep", clip =>
{
    audioSource.clip = clip;
});

// 卸载（用完调用，与 Load 配对）
ResMgr.Instance.UnloadAsset<GameObject>("UI/UICamera", isDel: true);
```

### 1.6 内部使用方

`UIMgr` 初始化时通过 `ResMgr` 同步加载 `UI/UICamera`、`UI/Canvas`、`UI/EventSystem` 三个常驻预制体。

---

## 2. ABMgr —— AssetBundle 管理器

源码：[ABMgr.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/AB/ABMgr.cs)

### 2.1 职责

- 继承 `SingletonAutoMono<ABMgr>`，自带协程能力。
- 加载主包 → 获取 `AssetBundleManifest` → 解析依赖 → 加载依赖包与目标包。
- 通过 `Dictionary<string, AssetBundle>` 缓存已加载 AB 包，避免重复加载（重复加载会报错）。
- 提供同步/异步两种加载方式（由 `isSync` 参数控制）。

### 2.2 路径与平台

```csharp
private string PathUrl => Application.streamingAssetsPath + "/";

private string MainName =>
#if UNITY_IOS        "IOS"
#elif UNITY_ANDROID  "Android"
#else                "PC"
#endif
```

主包文件名为 `PC` / `Android` / `IOS`，与平台对应。

### 2.3 加载流程（以异步为例）

1. `LoadMainAB()`：加载主包 `StreamingAssets/{MainName}`，取出 `AssetBundleManifest`。
2. `manifest.GetAllDependencies(abName)`：得到目标包的所有依赖包名数组。
3. 逐个加载依赖包：未加载则异步加载，加载中（字典值为 `null`）则 `yield return 0` 等待。
4. 加载目标包（同上逻辑）。
5. 加载包内资源：`isSync` 走 `LoadAsset`，否则 `LoadAssetAsync`，完成后回调。

> 关键技巧：AB 包加载时先在字典写入 `null` 占位，异步完成后替换为真实 `AssetBundle`。其他并发请求通过 `while (abDic[name] == null) yield return 0;` 等待，从而实现"同一包只异步加载一次"。

### 2.4 核心 API

```csharp
// 泛型异步加载
void LoadResAsync<T>(string abName, string resName, UnityAction<T> callBack, bool isSync = false)
    where T : Object;

// Type 异步加载
void LoadResAsync(string abName, string resName, System.Type type,
                  UnityAction<Object> callBack, bool isSync = false);

// 名字异步加载（返回 Object）
void LoadResAsync(string abName, string resName,
                  UnityAction<Object> callBack, bool isSync = false);

// 卸载单个 AB 包（返回是否成功，异步加载中无法卸载）
void UnLoadAB(string name, UnityAction<bool> callBackResult);

// 清空所有 AB 包（会 StopAllCoroutines）
void ClearAB();
```

> 注：同步加载相关的 `LoadRes<T>` 重载在源码中已被注释，目前对外仅暴露异步入口；`isSync=true` 时在异步流程内改用同步 API 实现快速加载。

### 2.5 用法

```csharp
ABMgr.Instance.LoadResAsync<GameObject>("ui", "BeginPanel", obj =>
{
    Instantiate(obj);
});
```

---

## 3. ABResMgr —— AB 加载对外封装（推荐入口）

源码：[ABResMgr.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/AB/ABResMgr.cs)

### 3.1 职责

业务层应**优先通过 `ABResMgr`** 而非直接调用 `ABMgr`，原因：

- 在 `UNITY_EDITOR` 下，可通过 `isDebug` 开关切换为 `EditorResMgr`，**免打 AB 包即可加载资源**，大幅提升开发效率。
- 打包后（非 Editor）统一委托给 `ABMgr`。

### 3.2 开关

```csharp
private bool isDebug = false;   // true: 走 EditorResMgr；false: 走 ABMgr
```

> `isDebug` 当前为私有字段，需在源码中手动修改。后续可考虑暴露为配置或菜单开关。

### 3.3 API

```csharp
void LoadResAsync<T>(string abName, string resName, UnityAction<T> callBack, bool isSync = false)
    where T : Object;
```

### 3.4 框架内使用方

- `UIMgr.ShowPanel<T>` 通过 `ABResMgr` 加载名为 `ui` 的 AB 包中的面板预制体（资源名 = 面板类名）。
- `MusicMgr` 通过 `ABResMgr` 加载 `music` 包（BGM）与 `sound` 包（音效）。

---

## 4. EditorResMgr —— 编辑器资源加载器

源码：[EditorResMgr.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/EditorRes/EditorResMgr.cs)

### 4.1 职责

- 仅在 `UNITY_EDITOR` 下编译有效，打包后所有方法返回 `null`。
- 通过 `AssetDatabase.LoadAssetAtPath` 直接加载源资产，免去打 AB 流程。
- 根类型自动推断后缀：`GameObject→.prefab`、`Material→.mat`、`Texture→.png`、`AudioClip→.mp3`。
- 提供图集（Sprite）加载：`LoadSprite`、`LoadSprites`。

### 4.2 资源根路径

```csharp
private string rootPath = "Assets/Editor/ArtRes/";
```

> 即编辑器模式下，源资产需放在 `Assets/Editor/ArtRes/{abName}/{resName}.{后缀}`，目录结构需与 AB 包名映射一致。

### 4.3 API

```csharp
T LoadEditorRes<T>(string path) where T : Object;            // 路径 = "abName/resName"
Sprite LoadSprite(string path, string spriteName);
Dictionary<string, Sprite> LoadSprites(string path);
```

---

## 5. UWQResMgr —— 网络资源下载器

源码：[UWQResMgr.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/UWQ/UWQResMgr.cs)

### 5.1 职责

- 继承 `SingletonAutoMono<UWQResMgr>`，自带协程。
- 基于 `UnityWebRequest`，按泛型类型分发到合适的请求 API。
- 支持类型：`string` / `byte[]` / `Texture` / `AssetBundle`。
- 同时提供成功与失败回调，请求结束后 `Dispose()` 释放。

### 5.2 API

```csharp
void LoadRes<T>(string path, UnityAction<T> callBack, UnityAction failCallBack) where T : class;
```

### 5.3 用法

```csharp
// 下载远端贴图
UWQResMgr.Instance.LoadRes<Texture>(
    "https://example.com/avatar.png",
    tex => rawImage.texture = tex,
    () => Debug.LogError("下载失败"));
```

> 支持的协议由 `UnityWebRequest` 决定，包括 http(s)、ftp、file 等。

---

## 6. 资源加载链路总览

```
业务代码
   │
   ▼
ABResMgr.LoadResAsync
   ├─ Editor + isDebug ──▶ EditorResMgr.LoadEditorRes   （Assets/Editor/ArtRes/...）
   └─ 运行时/打包    ──▶ ABMgr.LoadResAsync
                            ├─ LoadMainAB()              StreamingAssets/{PC|Android|IOS}
                            ├─ manifest.GetAllDependencies
                            └─ AssetBundle.LoadFromFile(Async)  缓存进 abDic
```

## 7. 资源放置约定

| 资源类型 | 通道 | 位置 | 包名约定 |
| --- | --- | --- | --- |
| UI 摄像机/画布/事件系统 | ResMgr | `Assets/Resources/UI/` | — |
| UI 面板预制体 | ABResMgr | `Assets/Editor/ArtRes/ui/`（编辑器）/ AB `ui` 包 | `ui` |
| 背景音乐 | ABResMgr | `Assets/Editor/ArtRes/music/` / AB `music` 包 | `music` |
| 音效 | ABResMgr | `Assets/Editor/ArtRes/sound/` / AB `sound` 包 | `sound` |
| 对象池预制体 | ResMgr | `Assets/Resources/` | 按预制体路径名 |
| 远端资源 | UWQResMgr | 任意 URL | — |

> 详细命名规范见 [开发约定](./Conventions.md)。
