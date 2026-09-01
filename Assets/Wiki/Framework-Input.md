# 输入管理 Input

输入管理器提供**按键/鼠标的可重绑监听**与**一次性按键捕获**，通过事件中心将输入广播给业务层，避免业务代码直接耦合 Unity Input。

源码：
- [InputMgr.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Input/InputMgr.cs)
- [InputInfo.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Input/InputInfo.cs)

## 1. 设计理念

- 输入**键位 → 事件名**映射，业务层只监听事件名，不关心具体按键。
- 支持运行时重绑按键（如"技能1"默认绑 Q 键，可改为鼠标侧键）。
- 通过 `EventCenter` 广播，与 [事件中心](./Framework-EventCenter.md) 联动。
- 借 `MonoMgr` 的 Update 驱动检测。

## 2. InputInfo

源码：[InputInfo.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Input/InputInfo.cs)

```csharp
public class InputInfo
{
    public enum E_KeyOrMouse { Key, Mouse }     // 键盘 或 鼠标
    public enum E_InputType    { Down, Up, Always }  // 按下 / 抬起 / 持续按住

    public E_KeyOrMouse keyOrMouse;
    public E_InputType inputType;
    public KeyCode key;        // 键盘键码
    public int mouseID;        // 鼠标按键 0/1/2

    public InputInfo(E_InputType inputType, KeyCode key);
    public InputInfo(E_InputType inputType, int mouseID);
}
```

## 3. 核心 API

### 3.1 开关

```csharp
void StartOrCloseInputMgr(bool isStart);   // true 开始监听，false 停止
```

### 3.2 注册/修改绑定

```csharp
// 键盘
void ChangeKeyboardInfo(E_EventType eventType, KeyCode key, InputInfo.E_InputType inputType);

// 鼠标
void ChangeMouseInfo(E_EventType eventType, int mouseID, InputInfo.E_InputType inputType);

// 移除某事件绑定
void RemoveInputInfo(E_EventType eventType);
```

### 3.3 一次性捕获

```csharp
// 等待下一次任意输入（键或鼠标），通过回调返回 InputInfo（可为 null）
void GetInputInfo(UnityAction<InputInfo> callBack);
```

> 用于"请按下你想绑定的按键"这类设置界面。下一帧开始监听，捕获到 `Input.anyKeyDown` 后构造 `InputInfo` 回调，并自动停止捕获。

## 4. 广播规则

`InputUpdate`（每帧）行为：

1. 若处于一次性捕获状态：检测 `Input.anyKeyDown`，遍历所有 KeyCode 与 3 个鼠标键，找出按下者，构造 `InputInfo` 回调并关闭捕获。
2. 若 `isStart == false`：直接返回（不处理常规监听）。
3. 遍历 `inputDic`，按 `Down/Up/Always` 检测对应按键，命中则 `EventCenter.Instance.EventTrigger(eventType)`（无参事件）。
4. **每帧固定广播轴值**：
   - `EventTrigger<float>(E_Input_Horizontal, Input.GetAxis("Horizontal"))`
   - `EventTrigger<float>(E_Input_Vertical,   Input.GetAxis("Vertical"))`

## 5. 用法示例

### 5.1 注册技能键并监听

```csharp
// 绑定：技能1 = KeyCode.Q，按下触发
InputMgr.Instance.ChangeKeyboardInfo(
    E_EventType.E_Input_Skill1, KeyCode.Q, InputInfo.E_InputType.Down);

// 开启输入
InputMgr.Instance.StartOrCloseInputMgr(true);

// 业务层监听
EventCenter.Instance.AddEventListener(E_EventType.E_Input_Skill1, () =>
{
    Debug.Log("释放技能1");
});
```

### 5.2 监听移动轴

```csharp
EventCenter.Instance.AddEventListener<float>(
    E_EventType.E_Input_Horizontal, h => moveX = h);
EventCenter.Instance.AddEventListener<float>(
    E_EventType.E_Input_Vertical,   v => moveZ = v);
```

### 5.3 重绑按键

```csharp
// 把技能1 改为鼠标右键（0=左，1=右，2=中），抬起触发
InputMgr.Instance.ChangeMouseInfo(
    E_EventType.E_Input_Skill1, 1, InputInfo.E_InputType.Up);
```

### 5.4 设置界面捕获按键

```csharp
InputMgr.Instance.GetInputInfo(info =>
{
    if (info == null) { Debug.Log("未捕获"); return; }
    // 用 info.key 或 info.mouseID 更新绑定
});
```

## 6. 注意事项

- ✅ 业务层**不要**直接调 `Input.GetKeyXxx`，统一走 `InputMgr + EventCenter`，便于将来支持多套输入方案（手柄、虚拟摇杆）。
- ⚠️ 一次性捕获 (`GetInputInfo`) 会在下一帧才开始（协程 `yield return 0`），调用方需注意时序。
- ⚠️ `InputUpdate` 末尾的轴广播是**每帧无条件触发**（即使 `isStart=false` 也会触发，因为轴广播在 return 之后？实际源码中轴广播在 `isStart` 判断之后——见下）。请以源码实际顺序为准，避免依赖轴在关闭输入时是否仍更新。
- ⚠️ 重绑同一事件会覆盖原 `keyOrMouse`，键盘↔鼠标切换时旧字段不会被清理（仅新字段生效），使用时按 `keyOrMouse` 取对应字段即可。
