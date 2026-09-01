# FluffyMovers Wiki

欢迎来到 **FluffyMovers** 项目 Wiki。本项目是一款基于 Unity 引擎开发的 3D 游戏，配套一套自研的轻量级 Unity 框架，覆盖资源加载、UI 管理、事件通信、对象池、定时器、输入、音频、场景等常用子系统。

> 项目当前处于早期开发阶段，框架部分较为完善，玩法与关卡等业务模块正在逐步搭建。

## 📌 项目速览

| 项目 | 说明 |
| --- | --- |
| 引擎 | Unity（uGUI 1.0、TextMeshPro 3.0、Cinemachine 2.10、Timeline 1.7） |
| 语言 | C# |
| 架构 | 自研框架 + Manager 单例模式 + 事件中心解耦 |
| 资源分发 | AssetBundle（StreamingAssets，按平台分包） |
| 入口场景 | `Level_01_CharacterController` |

## 🧭 文档导航

### 入门

- [快速开始](./Getting-Started.md) —— 环境要求、工程打开、首次运行
- [整体架构与目录结构](./Architecture.md) —— 工程布局、模块划分、依赖关系

### 框架核心（Framework）

- [框架总览与单例系统](./Framework-Overview.md) —— `BaseManager` / `SingletonMono` / `SingletonAutoMono`
- [事件中心 EventCenter](./Framework-EventCenter.md) —— 模块解耦通信
- [资源管理](./Framework-Resources.md) —— `ResMgr` / `ABMgr` / `ABResMgr` / `EditorResMgr` / `UWQResMgr`
- [UI 系统](./Framework-UI.md) —— `UIMgr` / `BasePanel` / 层级管理
- [对象池 Pool](./Framework-Pool.md) —— GameObject 池与逻辑对象池
- [定时器 Timer](./Framework-Timer.md) —— 基于协程的定时任务
- [输入管理 Input](./Framework-Input.md) —— 键鼠监听与可重绑按键
- [音频管理 Music](./Framework-Audio.md) —— 背景音乐与音效
- [场景管理 Scene](./Framework-Scene.md) —— 同步/异步切场景
- [Mono 管理](./Framework-Mono.md) —— 帧事件分发中枢
- [工具类 Util](./Framework-Utils.md) —— 数学/文本/加密工具

### 业务

- [角色控制 Character](./Gameplay-Character.md) —— 角色移动与 Q 弹形变

### 规范

- [开发约定与最佳实践](./Conventions.md) —— 命名、目录、新增模块规范

## 🗂️ 关键路径

- 框架代码：[Assets/_Project/Scripts/Core/Framework](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts/Core/Framework)
- 业务代码：[Assets/_Project/Scripts](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scripts)
- 美术资源：[Assets/_Project/Art](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Art)
- 场景文件：[Assets/_Project/Scenes](file:///e:/Project/FluffyMovers/FluffyMovers/Assets/_Project/Scenes)
