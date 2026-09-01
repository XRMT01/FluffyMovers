# UI 系统

UI 系统是框架中使用最频繁的子系统，提供层级管理、面板动态加载/复用、控件自动注册与事件绑定。

源码：
- [UIMgr.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/UI/UIMgr.cs)
- [BasePanel.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/UI/BasePanel.cs)

## 1. 架构概览

```
UIMgr (单例)
  │
  ├─ 常驻：UICamera / Canvas / EventSystem   （通过 ResMgr 同步加载，DontDestroyOnLoad）
  │
  ├─ Canvas 四层容器：
  │     Bottom / Middle / Top / System
  │
  └─ panelDic: 面板名 → PanelInfo<T>
        │
        ▼
     BasePanel (业务面板基类)
        │
        ├─ controlDic: 控件名 → UIBehaviour   （Awake 自动收集）
        ├─ ShowMe() / HideMe()                  （子类实现显隐逻辑）
        └─ GetControl<T>(name)                 （获取控件并自动绑定事件）
```

## 2. UI 层级

源码：[UIMgr.cs E_UILayer](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/UI/UIMgr.cs#L10-L28)

```csharp
public enum E_UILayer
{
    Bottom,   // 底层（最下）
    Middle,   // 中层（默认）
    Top,      // 高层
    System,   // 系统层（最上，如弹窗/Loading）
}
```

> Canvas 预制体内需包含名为 `Bottom` / `Middle` / `Top` / `System` 的四个子节点作为层级容器，`UIMgr` 在初始化时通过 `transform.Find` 获取它们。

## 3. BasePanel —— 面板基类

源码：[BasePanel.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/UI/BasePanel.cs)

### 3.1 自动控件收集

在 `Awake()` 中通过 `GetComponentsInChildren<T>(true)` 批量收集以下类型控件，存入 `controlDic`：

`Button`、`Toggle`、`Slider`、`InputField`、`ScrollRect`、`Dropdown`、`Text`、`TextMeshPro`、`Image`。

### 3.2 默认名过滤

部分通用子节点名会被**排除**收集（避免重复挂载事件），见 [BasePanel.cs defaultNameList](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/UI/BasePanel.cs#L18-L31)：

```
Image, Text (TMP), RawImage, Background, Checkmark, Label,
Text (Legacy), Arrow, Placeholder, Fill, Handle, Viewport,
Scrollbar Horizontal, Scrollbar Vertical
```

> ⚠️ 因此设计：业务控件在 Hierarchy 中必须取**有意义的唯一名字**（如 `Btn_Start`、`Btn_Shop`），不能与上述默认名冲突。

### 3.3 自动事件绑定

收集到 `Button` / `Slider` / `Toggle` 时，会自动挂上对应事件，回调到基类的虚方法：

```csharp
protected virtual void ClickBtn(string btnName);                // Button.onClick
protected virtual void SliderValueChange(string name, float v); // Slider.onValueChanged
protected virtual void ToggleValueChange(string name, bool v);  // Toggle.onValueChanged
```

子类 `override` 即可按控件名分发处理逻辑。

### 3.4 核心 API

```csharp
// 必须实现的显隐接口
public abstract void ShowMe();
public abstract void HideMe();

// 获取控件（按控件名）
public T GetControl<T>(string name) where T : UIBehaviour;

// UIMgr 提供的 EventTrigger 自定义事件绑定（静态）
public static void AddCustomEventListener(UIBehaviour control,
    EventTriggerType type, UnityAction<BaseEventData> callBack);
```

## 4. UIMgr —— UI 管理器

源码：[UIMgr.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/UI/UIMgr.cs)

### 4.1 初始化

构造函数（懒加载触发）中：

1. 加载 `UI/UICamera` → 设为 `DontDestroyOnLoad`。
2. 加载 `UI/Canvas` → `worldCamera = uiCamera` → `DontDestroyOnLoad`。
3. 查找 Canvas 下 `Bottom/Middle/Top/System` 四层容器。
4. 加载 `UI/EventSystem` → `DontDestroyOnLoad`。

### 4.2 ShowPanel —— 显示面板

```csharp
public void ShowPanel<T>(
    E_UILayer layer = E_UILayer.Middle,
    UnityAction<T> callBack = null,
    bool isSync = false
) where T : BasePanel;
```

行为：

- 面板名 = `typeof(T).Name`，**类名必须与 AB 资源名（预制体名）一致**。
- 先在 `panelDic` 占位，避免重复加载。
- 通过 `ABResMgr.LoadResAsync<GameObject>("ui", panelName, ...)` 异步加载预制体。
- 加载完成后：实例化到对应层级容器（保留原始缩放）→ `GetComponent<T>()` → 调用 `panel.ShowMe()` → 触发回调。
- **异步期间被 Hide**：加载完成时检测 `isHide`，若已被隐藏则直接移除并丢弃，避免无意义实例化。

### 4.3 HidePanel —— 隐藏面板

```csharp
public void HidePanel<T>(bool isDestory = false) where T : BasePanel;
```

- `isDestory=false`：仅 `SetActive(false)` + 调用 `HideMe()`，下次显示直接复用。
- `isDestory=true`：`Destroy` 游戏对象并从 `panelDic` 移除。
- 若面板尚在异步加载中：设置 `isHide=true` 并清空回调，加载完成时由 `ShowPanel` 流程自动丢弃。

### 4.4 GetPanel —— 获取已存在面板

```csharp
public void GetPanel<T>(UnityAction<T> callBack) where T : BasePanel;
```

- 已加载且未隐藏：直接回调返回。
- 正在异步加载：挂入回调队列，加载完成后一并触发。
- 已隐藏：不回调（视为不可用）。

### 4.5 自定义事件

```csharp
public static void AddCustomEventListener(
    UIBehaviour control, EventTriggerType type,
    UnityAction<BaseEventData> callBack);
```

为控件挂 `EventTrigger` 并注册指定类型事件（如拖拽、按下、抬起、进入、离开等），避免手动加组件。

## 5. 完整用法示例

### 5.1 创建业务面板

```csharp
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BeginPanel : BasePanel
{
    protected override void ClickBtn(string btnName)
    {
        switch (btnName)
        {
            case "Btn_Start":
                SceneMgr.Instance.LoadSceneAsyn("Level_01");
                break;
            case "Btn_Quit":
                Application.Quit();
                break;
        }
    }

    protected override void SliderValueChange(string sliderName, float value)
    {
        if (sliderName == "Slider_Volume")
            MusicMgr.Instance.ChangeBKMusicValue(value);
    }

    public override void ShowMe()
    {
        // 入场动画/初始化
        gameObject.SetActive(true);
    }

    public override void HideMe()
    {
        gameObject.SetActive(false);
    }
}
```

### 5.2 显示/隐藏

```csharp
UIMgr.Instance.ShowPanel<BeginPanel>(E_UILayer.Middle, panel =>
{
    // 面板就绪，可进一步操作
});

// 暂时隐藏（保留实例）
UIMgr.Instance.HidePanel<BeginPanel>();

// 彻底销毁
UIMgr.Instance.HidePanel<BeginPanel>(isDestory: true);
```

### 5.3 直接获取控件

```csharp
var txt = panel.GetControl<TextMeshPro>("Txt_Title");
txt.text = "Fluffy Movers";
```

## 6. 资源约定

| 项 | 约定 |
| --- | --- |
| 预制体名 | 与面板脚本类名完全一致（如 `BeginPanel.prefab` ↔ `BeginPanel.cs`） |
| AB 包名 | 统一为 `ui` |
| 层级容器 | Canvas 预制体内含 `Bottom/Middle/Top/System` 四个空节点 |
| 控件命名 | 业务控件须唯一且不与默认名冲突，便于自动收集 |

## 7. 陷阱与提示

- ⚠️ **类名 = 预制体名**：若二者不一致，`ShowPanel` 无法在 AB 包中找到资源。
- ⚠️ **默认名控件不会被收集**：名为 `Image`、`Label` 等的节点会被忽略，请给业务控件起唯一名。
- ⚠️ **异步期间隐藏**：面板刚 `ShowPanel` 立即 `HidePanel` 会触发"加载完成即丢弃"逻辑，不会产生残留实例。
- ✅ 同一面板重复 `ShowPanel`：已加载则直接 `ShowMe()` + 触发回调；加载中则追加回调。
- ✅ UI 摄像机与 Canvas 一次性常驻，场景切换不丢失。
