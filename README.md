# 2D 类银河恶魔城 Demo（Unity 2022 / C#）

Unity 2022.3 LTS 做的 2D 动作游戏练习项目。玩法代码全在 `Assets/Script/`，第三方美术素材不进仓库。

引擎版本 `2022.3.50f1c1`（Unity 中国版）。**别用 Unity 6 打开**：`Rigidbody2D.velocity` 在 Unity 6 已改名 `linearVelocity`，会刷一堆警告和兼容问题。

---

## 一、操作

| 操作 | 按键 |
|---|---|
| 移动 | `A` / `D`（或左右方向键） |
| 跳跃 / 墙跳 | `Space` |
| 冲刺 | `Left Shift`，起点会留一颗水晶，自动追着附近敌人打 |
| 普通攻击（三连段） | 鼠标左键 |
| 弹反 | `Q` |
| 投掷剑 | 按住鼠标右键瞄准，松开投出 |
| 召回剑 | 剑在场上时按鼠标右键 |
| 切换投掷剑模式 | `F1` 普通 / `F2` 弹射 / `F3` 穿透 / `F4` 旋转（录演示用，屏幕左上角会显示当前模式）|
| 黑洞 | `R` 起手（角色升空展开黑洞）→ 黑洞完全展开后头顶出现 `R` 提示 → 再按一次 `R`，对范围内每个敌人各放一个分身攻击 |

---

## 二、做了什么

**玩家**
- 状态机控制移动：Idle / Move / Jump / Air / WallSlide / WallJump / Dash / PrimaryAttack / CounterAttack / AimSword / CatchSword / Blackhole，共 12 个状态类
- 状态什么时候结束由动画事件（Animation Event）回调决定，不用计时器去猜动画长度
- 攻击是三段连击，`Q` 弹反有反击窗口
- 投剑：蓄力时显示瞄准方向，剑在墙上会弹射 + 旋转，右键可以把剑召回手里

**技能**（`Skill` 基类 → `SkillManager` 分发 → `Xxx_Skill_Controller` 管生成物）
- 剑：普通 / 弹射 / 穿透 / 旋转四种模式，在 Inspector 里配置；录演示时也能用 `F1`~`F4` 在游戏中直接切
- 分身：被黑洞技能调用，冲到目标身上打一下就消失
- 水晶：冲刺的起点生成 3 颗，随机散开、错开生成时间，各自追最近的敌人，碰到敌人或超时后爆炸
- 黑洞：先把范围内的敌人冻结，展开完成后头顶提示 `R`，玩家再按一次 `R` 才发动攻击；不按就超时取消
- 冲刺：带冷却的位移

**敌人**
- 骷髅一套独立状态机：Idle / Move / Battle / Attack / Stunned / Grounded / Death
- 受击闪白、击退、地形检测、弹反窗口（`canBeStunned`）都在 `Entity` 基类里，玩家和敌人共用
- 死亡后停 AI、播死亡动画，延时销毁

**关卡**
- Tilemap 搭的关卡，Cinemachine 相机跟随，视差背景

---

## 三、代码结构

```
Assets/
├── Script/
│   ├── Entity.cs / EntityFX.cs          # 角色基类：受击、击退、闪白、地形检测
│   ├── PlayerManager.cs                 # 玩家单例引用
│   ├── ParallaxBackground.cs
│   ├── Player/
│   │   ├── Player.cs                    # 玩家：状态机持有者 + 输入 + 组件引用
│   │   ├── PlayerState.cs / PlayerStateMachine.cs
│   │   └── PlayerXxxState.cs            # 各状态（Idle / Move / Jump / Dash / Attack / ...）
│   ├── Enemy/
│   │   ├── Enemy.cs / EnemyState.cs / EnemyStateMachine.cs
│   │   └── Skeleton/                    # 骷髅敌人的各状态
│   └── Skill/
│       ├── Skill.cs                     # 技能基类：冷却、IsReady、释放
│       ├── SkillManager.cs              # 单例：持有并分发所有技能
│       ├── Sword_Skill / Clone_Skill / Crystal_Skill / Blackhole_Skill / Dash_Skill
│       └── Skill_Controller/            # 各技能生成物的运行时行为
├── Prefabs/                             # Sword / Clone / Crystal / Blackhole 等
├── Scenes/                              # Untitled.unity（主场景）
├── Animataion/ Material/ Tile Palette/   # 动画片段、材质、瓦片调色板
```

**怎么分工的**

- 状态用类拆开：每个状态一个类，`Enter / Update / Exit` 各管一段，加新状态不用改老状态。这套写法敌人那边直接复用了。
- 技能分三层：`Skill` 只存数据和冷却，`SkillManager` 负责持有和分发，`Controller` 管生成物的表现和生命周期——表现逻辑不塞进数据层。
- `Entity` 兜住"会被打的物体"的公共部分，玩家和敌人只写各自不一样的地方。

---

## 四、已知问题 / 待做

- [x] 敌人血量和死亡动画（Animator 的 `Death` 布尔 + 死亡后停 AI + 延时销毁）
- [ ] 血条和技能冷却 UI（UGUI）还没做（玩家血量/死亡按现在的设计暂时不做）
- [ ] 没有存档
- [ ] 剑 / 分身 / 水晶还是 `Instantiate` + `Destroy`，打算换成对象池
- [ ] 敌人多的时候没测过性能，没有 Profiler 数据

## 五、仓库说明

- **包含**：`Assets/Script/` 下的全部代码、场景与预制体、动画片段与材质、Unity 工程配置（`ProjectSettings/`、`Packages/`）
- **不包含**：第三方美术素材（`Assets/Graphics/`，约 16 MB，版权归原作者）、Unity 生成的缓存目录（`Library/`、`Temp/` 等）
- 所以克隆下来可以直接读代码；想完整跑起来得自己准备美术和音频素材

## 六、其它

- 引擎：Unity 2022.3 LTS（中国版 `2022.3.50f1c1`），C#
- 数值参数（伤害、冷却、速度之类）都用 `[SerializeField]` 放在 Inspector 里调，代码里不写死
- 本项目参考了Alex的类银河恶魔城教程，对黑洞、投掷等技能根据自身需求进行了修改
