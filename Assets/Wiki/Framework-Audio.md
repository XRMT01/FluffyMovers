# 音频管理 Music

音频管理器负责**背景音乐（BGM）**与**音效（Sound）**的播放控制，音效走对象池复用 `AudioSource`。

源码：[MusicMgr.cs](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework/Music/MusicMgr.cs)

## 1. 架构概览

```
MusicMgr (BaseManager 单例)
  │
  ├─ bkMusic: AudioSource   单一全局背景音乐源（常驻 DontDestroyOnLoad）
  │
  ├─ soundList: List<AudioSource>  当前播放中的音效源
  │
  └─ Fixedupdate(借 MonoMgr)：
        └─ 遍历 soundList，已播完的 → 清 clip → 归还对象池 → 移除
```

## 2. 资源约定

| 类型 | AB 包名 | 资源类型 |
| --- | --- | --- |
| 背景音乐 | `music` | `AudioClip` |
| 音效 | `sound` | `AudioClip` |
| 音效载体预制体 | `Sound/soundObj`（ResMgr 路径） | `GameObject`（带 `AudioSource`） |

> 音效通过 `PoolMgr.GetObj("Sound/soundObj")` 获取带 `AudioSource` 的预制体，用完归还。该预制体需放在 `Assets/Resources/Sound/soundObj.prefab`。

## 3. 背景音乐 API

```csharp
void PlayBKMusic(string name);              // 播放（异步加载后 loop）
void StopBKMusic();                         // 停止
void PauseBKMusic();                        // 暂停
void ChangeBKMusicValue(float v);           // 设置音量（0~1）
```

- 首次播放时动态创建 `BKMusic` GameObject 并 `DontDestroyOnLoad`。
- 默认音量 `bkMusicValue = 0.1f`，可通过 `ChangeBKMusicValue` 调整。

## 4. 音效 API

```csharp
void PlaySound(string name, bool isLoop = false,
               bool isSync = false,
               UnityAction<AudioSource> callBack = null);

void StopSound(AudioSource source);         // 停止单个音效（归还池）
void ChangeSoundValue(float v);             // 改全部音效音量
void PlayOrPauseSound(bool isPlay);          // 全部继续/暂停
void ClearSound();                           // 清空全部音效（重要，见下）
```

### 4.1 PlaySound 流程

1. `ABResMgr.LoadResAsync<AudioClip>("sound", name, ...)` 加载片段。
2. `PoolMgr.GetObj("Sound/soundObj").GetComponent<AudioSource>()` 取载体。
3. `Stop()` 重置 → 设 clip / loop / volume → `Play()`。
4. 加入 `soundList`，回调返回 `AudioSource` 供外部控制。

### 4.2 自动回收

每物理帧检查 `soundList`，`isPlaying == false` 的视为播完：

- `clip = null` → `PoolMgr.PushObj(gameObject)` → 移除列表。

## 5. 用法示例

```csharp
// 播放 BGM
MusicMgr.Instance.PlayBKMusic("bgm_main");

// 调整音量
MusicMgr.Instance.ChangeBKMusicValue(0.3f);

// 播放一次性音效
MusicMgr.Instance.PlaySound("sfx_jump");

// 播放循环音效并保留引用，稍后停止
MusicMgr.Instance.PlaySound("sfx_engine", isLoop: true, source =>
{
    engineSource = source;
});
// ...
MusicMgr.Instance.StopSound(engineSource);
```

## 6. 注意事项

- ⚠️ **退场景/退游戏前必须调用 `ClearSound()`**：源码注释中三次强调。原因：音效载体在对象池中，若不主动清空，`AudioSource.clip` 引用残留可能导致资源无法卸载。
- ⚠️ `soundIsPlay` 标记控制是否执行回收检查；`PlayOrPauseSound(false)` 暂停时回收暂停，但音效对象仍占用，记得后续 `ClearSound()`。
- ✅ 音效载体来自对象池，频繁播放同一音效开销低。
- ✅ 默认音量 `0.1f` 偏小，可按需调整或暴露到设置面板。
