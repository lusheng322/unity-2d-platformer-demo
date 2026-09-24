# 2D 类银河恶魔城 Demo（Unity / C#）

一个用 **Unity 2022.3 LTS** 做的 2D 动作游戏练习项目：状态机驱动的玩家控制、技能系统、敌人 AI、Tilemap 关卡。
项目仍在迭代中，已实现从"移动/战斗"到"技能组合"的完整核心循环。

> 引擎版本：`2022.3.50f1c1`（Unity 中国版 2022.3 LTS）。请勿用 Unity 6 打开——`Rigidbody2D.velocity` 等 API 在 Unity 6 已改名（`linearVelocity`），会产生大量警告与兼容问题。

---

## 一、怎么玩（操作说明）

| 操作 | 按键 |
|---|---|
| 移动 | `A` / `D`（或左右方向键） |
| 跳跃 / 墙跳 | `Space` |
| 冲刺（闪避） | `Left Shift` —— **起点会留下一个水晶**，自动追踪并攻击附近敌人 |
| 普通攻击（连段） | 鼠标左键 |
| 弹反（反击） | `Q` |
| 投掷剑（蓄力瞄准） | 按住鼠标右键瞄准，松开投出 |
| 召回剑 | 剑在场上时按鼠标右键 |
| 释放黑洞 | `R` 起手（角色升空并展开黑洞）→ 黑洞展开完成后**角色头顶出现 "R" 提示**，**再按一次 `R`** 即可对黑洞内所有敌人各发动一次分身攻击 |

---

## 二、功能清单

**玩家控制**
- 基于 **State 模式**的玩家状态机：Idle / Move / Jump / Air / WallSlide / WallJump / Dash / PrimaryAttack / CounterAttack / AimSword / CatchSword / Blackhole（共 12 个状态）
- 动画事件（Animation Event）驱动状态结束回调，而不是靠计时器猜动画长度
- 攻击方向、墙跳方向、冲刺方向都来自输入

**技能系统**（`Skill` 基类 → `SkillManager` 分发 → `Xxx_Skill_Controller` 管理生成物）
- 剑技能：普通 / 弹射（Bounce）/ 穿透（Pierce）/ 旋转（Spin）四种模式，可在 Inspector 切换
- 分身（Clone）：被黑洞技能调用，对目标发动一次攻击
- 水晶（Crystal）：闪避时在起点生成 **3 个**水晶（随机散布 + 错开生成时间），各自追踪最近的敌人，接触或超时后爆炸
- 黑洞（Blackhole）：范围冻结敌人 → 展开完成后头顶出现 "R" 提示 → 再按 `R` 对黑洞内每个敌人各发动一次分身攻击
- 冲刺（Dash）：带冷却的位移技能

**敌人与关卡**
- 骷髅敌人独立 AI 状态机：Idle / Move / Battle / Attack / Stunned / Grounded
- 弹反窗口（`canBeStunned`）与霸体打断，受击闪白、击退等打击反馈由 `Entity` 基类统一提供
- Tilemap 关卡、Cinemachine 相机跟随、视差背景

**工程实践**
- 玩家与敌人共用 `Entity` 基类（受击/击退/地形检测/组件缓存）
- 技能参数全部 `[SerializeField]` 在 Inspector 配置，不在代码里写死数值
- 关键设计点带“为什么这么写”的注释，排查过程记录在 `Docs/`

---

## 三、代码结构

```
Assets/
├── Script/
│   ├── Entity.cs / EntityFX.cs          # 角色基类：受击、击退、闪白、地形检测
│   ├── PlayerManager.cs                 # 玩家单例引用
│   ├── ParallaxBackground.cs
│   ├── Player/
│   │   ├── Player.cs                    # 玩家：状态机持有者 + 输入 + 组件
│   │   ├── PlayerState.cs / PlayerStateMachine.cs
│   │   └── PlayerXxxState.cs            # 各状态（Idle/Move/Jump/Dash/Attack/...）
│   ├── Enemy/
│   │   ├── Enemy.cs / EnemyState.cs / EnemyStateMachine.cs
│   │   └── Skeleton/                    # 骷髅敌人的各状态
│   └── Skill/
│       ├── Skill.cs                     # 技能基类：冷却、IsReady、释放
│       ├── SkillManager.cs              # 单例：集中持有并分发所有技能
│       ├── Sword_Skill / Clone_Skill / Crystal_Skill / Blackhole_Skill / Dash_Skill
│       └── Skill_Controller/            # 各技能生成物的运行时行为
├── Prefabs/                             # Sword / Clone / Crystal / Blackhole 等
├── Scenes/                              # Untitled.unity（主场景）
├── Animataion/ Graphics/ Material/ Tile Palette/
└── Docs/                                # 设计与排查记录
```

**三条设计主线**（面试常问）：
1. **状态机**：每个状态是独立类，`Enter/Update/Exit` 各管一段逻辑；新增状态不改动已有状态（开闭原则），且同一套思路可以复用给敌人。
2. **技能三层分工**：`Skill`（数据与冷却）→ `SkillManager`（持有与分发）→ `Controller`（表现与生成物生命周期），避免把表现逻辑塞进数据层。
3. **基类复用**：`Entity` 统一处理"会被打的物体"，玩家和敌人只实现各自差异。

---

## 四、已知问题与后续计划

- [x] 敌人血量与死亡动画（`Death` 布尔 + 死亡后停 AI、延时销毁；动画需在 Animator 里接入，步骤见 `Docs/04_编辑器待办.md`）
- [ ] 血条与技能冷却 UI（UGUI）尚未实现（**玩家血量/死亡按当前设计暂不做**）
- [ ] 没有存档系统
- [ ] 技能生成物（剑/分身/水晶）目前用 `Instantiate` / `Destroy`，计划引入对象池
- [ ] 敌人数量多时缺少性能测试（Profiler 数据）

## 五、开发说明（AI 工具的使用边界）

本项目的代码由本人跟着教程逐步完成，并在其之上做了自己的改造（黑洞机制重做、水晶改为闪避触发、一批 bug 的定位与修复）。
排查与重构阶段使用了 AI 辅助（读代码、串因果链、提出修改方案），使用原则是：**AI 提出方案，取舍与验证由本人完成**——每一项改动都在 Unity 里实跑验证，涉及性能的结论用 Profiler / 控制台确认。

- 排查记录：`Docs/01_Bug排查与改造记录.md`
- 架构与面试讲解稿：`Docs/02_架构与讲解稿.md`
- Unity / UE 对照与"为什么投 Unity 岗"话术：`Docs/03_Unity与UE对照.md`
- 编辑器待办清单：`Docs/04_编辑器待办.md`
