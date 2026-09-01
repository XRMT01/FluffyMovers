# 工具类 Util

框架在 [Util/](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Util) 下提供三个静态工具类。

| 工具类 | 职责 |
| --- | --- |
| [MathUtil](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Util/MathUtil.cs) | 角度/弧度、距离判断、扇形范围、射线检测、范围检测 |
| [TextUtil](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Util/TextUtil.cs) | 字符串分割、数字/时间格式化、大数字转中文简写 |
| [EncryptionUtil](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Util/EncryptionUtil.cs) | 简单异或加密（非安全强度，仅防篡改） |

---

## 1. MathUtil

源码：[MathUtil.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Util/MathUtil.cs)

### 1.1 角度/弧度

```csharp
float Deg2Rad(float deg);   // 角度 → 弧度
float Rad2Deg(float rad);   // 弧度 → 角度
```

### 1.2 距离判断（按平面投影）

```csharp
float GetObjDistanceXZ(Vector3 src, Vector3 target);   // XZ 平面距离（忽略 Y）
bool  CheckObjDistanceXZ(Vector3 src, Vector3 target, float dis);  // 是否 ≤ dis
float GetObjDistanceXY(Vector3 src, Vector3 target);   // XY 平面距离
bool  CheckObjDistanceXY(Vector3 src, Vector3 target, float dis);
```

### 1.3 位置判断

```csharp
// 世界坐标点是否在主摄像机屏幕外
bool IsWorldPosOutScreen(Vector3 pos);

// 目标点是否在扇形范围内（XZ 平面，pos 自身朝向 forward）
bool IsInSectorRangeXZ(Vector3 pos, Vector3 forward, Vector3 targetPos, float radius, float angle);
```

### 1.4 射线检测

提供 `RayCast`（首个）与 `RayCastAll`（全部）两组重载，泛型 `T` 可取 `RaycastHit` / `GameObject` / 任意组件：

```csharp
void RayCast(Ray ray, UnityAction<RaycastHit> callBack, float maxDistance, int layerMask);
void RayCast(Ray ray, UnityAction<GameObject> callBack, float maxDistance, int layerMask);
void RayCast<T>(Ray ray, UnityAction<T> callBack, float maxDistance, int layerMask);

void RayCastAll(Ray ray, UnityAction<RaycastHit> callBack, float maxDistance, int layerMask);
void RayCastAll(Ray ray, UnityAction<GameObject> callBack, float maxDistance, int layerMask);
void RayCastAll<T>(Ray ray, UnityAction<T> callBack, float maxDistance, int layerMask);
```

> 命中时回调触发，未命中不触发。

### 1.5 范围检测（Overlap）

```csharp
// 盒子范围（中心、旋转、半尺寸、层级）
void OverlapBox<T>(Vector3 center, Quaternion rotation, Vector3 halfExtents, int layerMask, UnityAction<T> callBack);

// 球形范围
void OverlapSphere<T>(Vector3 center, float radius, int layerMask, UnityAction<T> callBack);
```

`T` 可为 `Collider` / `GameObject` / 任意组件，自动按类型分发。

### 1.6 用法

```csharp
// 鼠标点击射线检测
Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
MathUtil.RayCast<Enemy>(ray, enemy => enemy.TakeDamage(), 100f, LayerMask.GetMask("Enemy"));

// 范围伤害
MathUtil.OverlapSphere<Enemy>(transform.position, 5f, LayerMask.GetMask("Enemy"), e => e.TakeDamage(damage));
```

---

## 2. TextUtil

源码：[TextUtil.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Util/TextUtil.cs)

> 内部使用静态 `StringBuilder resultStr` 复用，非线程安全，仅在主线程调用。

### 2.1 字符串分割

```csharp
// 分隔符类型 type：1-; 2-, 3-% 4-: 5-空格 6-| 7-_
string[] SplitStr(string str, int type = 1);
int[]    SplitStrToIntArr(string str, int type = 1);

// 二次分割（键值对形式），如 "1,10;2,20"
void SplitStrToIntArrTwice(string str, int typeOne, int typeTwo, UnityAction<int,int> callBack);
void SplitStrTwice(string str, int typeOne, int typeTwo, UnityAction<string,string> callBack);
```

> 自动把中文标点（`；`，`，`，`：`）替换为对应英文标点再分割，兼容中英混排。

### 2.2 数字格式化

```csharp
string GetNumStr(int value, int len);    // 指定长度，前补 0，如 GetNumStr(5,3)="005"
string GetDecimalStr(float value, int len);  // 保留 n 位小数
```

### 2.3 大数字简写（中文）

```csharp
string GetBigDataToString(int num);
// >= 1亿 → "n亿n千万"；>= 1万 → "n万n千"；否则原值
```

### 2.4 时间格式化

```csharp
// 秒 → "n时n分n秒" 等
string SecondToHMS(int s, bool egZero = false, bool isKeepLen = false,
                   string hourStr = "时", string minuteStr = "分", string secondStr = "秒");

// 秒 → "HH:MM:SS"
string SecondToHMS2(int s, bool egZero = false);
```

参数说明：

- `egZero`：为 `true` 时省略值为 0 的高位（如 30 秒 → "30 秒" 而非 "0 时 0 分 30 秒"）。
- `isKeepLen`：为 `true` 时数字补零到两位（如 "05 分"）。

### 2.5 用法

```csharp
TextUtil.SecondToHMS2(3661);   // "01:01:01"
TextUtil.GetBigDataToString(12345678);  // "1234万5千"（示例语义）
```

---

## 3. EncryptionUtil

源码：[EncryptionUtil.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Util/EncryptionUtil.cs)

> ⚠️ 这是一套**轻量异或加密**，仅用于防止普通玩家篡改本地存档，**不具备密码学安全性**，不要用于保护敏感数据或联网传输。

### 3.1 API

```csharp
int  GetRandomKey();                     // 1~10000 + 5 的随机密钥
int  LockValue(int value, int key);       // 加密
long LockValue(long value, int key);
int  UnLoackValue(int value, int key);   // 解密（注意源码拼写为 UnLoackValue）
long UnLoackValue(long value, int key);
```

### 3.2 算法

```
加密：value ^= (key % 9); value ^= 0xADAD; value ^= (1<<5); value += key;
解密：value -= key; value ^= (key % 9); value ^= 0xADAD; value ^= (1<<5);
```

> 解密对 `value == 0` 直接返回 0（视为未加密的初始值）。

### 3.3 用法

```csharp
int key = EncryptionUtil.GetRandomKey();
int stored = EncryptionUtil.LockValue(gold, key);   // 存档
// 读取时
int gold = EncryptionUtil.UnLoackValue(stored, key);
```

> ⚠️ 方法名拼写为 `UnLoackValue`（非 `UnLockValue`），调用时注意。密钥 `key` 需与加密时一致并妥善保存。

## 4. 注意事项

- `TextUtil` 与 `EncryptionUtil` 部分方法名存在拼写问题（`UnLoackValue`、`Claer` 事件中心），重构时建议统一修正并全局替换调用点。
- `MathUtil` 射线/范围检测均为**同步**调用，回调在命中时同步触发，不要在回调中做重逻辑。
