# 1151VR-HW4-F15440011-萬紓語

## 星際穿梭 STAR RUNNER — 3D 飛行躲避遊戲

Unity 3D 作業：操控飛行器穿越隕石帶，**60 秒內不撞到任何障礙物即通關**。

---

## 開發環境

| 項目 | 版本 |
| --- | --- |
| Unity | 2022.3.62f2 |
| 渲染管線 | Built-in Render Pipeline |
| 平台 | Windows |

## 執行方式

1. 以 Unity Hub 開啟本專案資料夾
2. 開啟場景 `Assets/FlyingGame/Scenes/FlyingGame.unity`（已加入 Build Settings）
3. 點擊 **Play**

## 操作說明

| 輸入 | 功能 |
| --- | --- |
| `W` `A` `S` `D` / 方向鍵 | 上下左右移動飛行器 |
| `ESC` | 返回主選單 |
| 滑鼠點擊 | 操作 UI 按鈕 |

## 遊戲流程

1. **開始介面**：標題動畫、`開始遊戲`、`退出遊戲` 兩顆按鈕
2. **遊戲進行**：倒數 60 秒，隕石持續從前方生成並高速飛來
3. **結算**：撞到隕石 → `撞毀了`；撐滿 60 秒 → `通關成功`

## 作業需求對照

| 需求 | 實作位置 |
| --- | --- |
| 加入 3D 飛行器模型 | `Models/craft_speederA.fbx`，含側傾、俯仰姿態與引擎光暈 |
| 加入障礙物模型 | `Prefabs/` 下 5 種岩石／隕石預製體 |
| 場景中不斷生成障礙物石頭 | `Scripts/ObstacleSpawner.cs`，物件池 + 難度隨時間遞增 |
| 音效：飛行時 | `Audio/fly_engine.ogg`（循環播放，音量淡入淡出） |
| 音效：撞到障礙物 | `Audio/crash.ogg`（撞擊瞬間播放並震屏） |
| 音效：順利通過 | `Audio/pass.ogg`（每閃過一顆隕石播放，通關時再次播放） |
| 一分鐘內不撞到障礙物即通關 | `Scripts/GameManager.cs`，`roundDuration = 60` |
| 開始介面（開始遊戲／退出遊戲） | `UI/MenuPanel`，按鈕事件綁定 `GameManager.StartGame` / `QuitGame` |

## 專案結構

```
Assets/FlyingGame/
├── Models/       3D 模型（飛行器 + 5 種岩石）
├── Audio/        三個音效
├── Fonts/        Noto Sans SC 中文字型
├── Materials/    材質與程式化產生的貼圖（粒子、圓角按鈕、暗角）
├── Prefabs/      岩石預製體
├── Scripts/
│   ├── GameManager.cs      遊戲狀態機、計時、音效、UI 切換
│   ├── PlayerController.cs 飛行器操控與碰撞判定
│   ├── ObstacleSpawner.cs  障礙物生成器（物件池）
│   ├── Obstacle.cs         障礙物移動、通過判定、近鏡頭淡出
│   ├── CameraFollow.cs     跟隨攝影機、FOV 加速感、撞擊震屏
│   └── UIChineseFont.cs    中文字型兜底
├── Editor/
│   └── FlyingGameBuilder.cs 一鍵重建整個場景（Tools ▸ 星際穿梭 ▸ 構建遊戲場景）
└── Scenes/
    └── FlyingGame.unity     主場景
```

## 素材來源

| 素材 | 來源 | 授權 |
| --- | --- | --- |
| 3D 模型（飛行器、岩石） | [Kenney Space Kit](https://kenney.nl/assets/space-kit) | CC0 公眾領域 |
| 音效 | [Kenney Sci-Fi Sounds](https://kenney.nl/assets/sci-fi-sounds) | CC0 公眾領域 |
| 中文字型 | [Noto Sans SC](https://fonts.google.com/noto/specimen/Noto+Sans+SC) | SIL Open Font License 1.1 |

## 可調參數

| 腳本 | 參數 | 說明 |
| --- | --- | --- |
| `GameManager` | `roundDuration` | 通關所需秒數（預設 60） |
| `ObstacleSpawner` | `startSpeed` / `endSpeed` | 隕石飛行速度（15 → 26） |
| `ObstacleSpawner` | `spawnInterval` / `spawnIntervalEnd` | 生成間隔（0.78s → 0.44s） |
| `PlayerController` | `moveSpeed` | 飛行器移動速度（12） |
| `PlayerController` | `hitRadius` | 碰撞判定半徑（0.78，比模型略小使手感更寬容） |
