# Bug 排查与改造记录

> 这份文档记录本项目三次真实排查/改造过程：**现象 → 我的初步判断 → 证据 → 根因 → 修法 → 验证方法**。
> 写它的目的有两个：① 以后自己忘了能查；② 面试时"我遇到过什么问题、怎么定位的"这类问题可以直接照着讲。

---

## 案例一：黑洞 QTE 后只攻击一次

**现象**：黑洞技能按完 QTE 后，只对敌人发动一次攻击；预期是"按了几个 QTE 就打几个敌人"。

**初步判断**：怀疑 `Blackhole_Skill_Controller.CloneAttackLogic()` 的攻击循环写错了。

**排查过程**
1. 读代码：攻击循环是"从 `targets` 里**随机挑一个**目标，一共打 `amountOfAttacks` 次"，次数来自 `Blackhole_Skill` 的序列化字段。
2. 检查场景里的实际配置（`Assets/Untitled.unity` 是文本格式，可以直接搜字段）：
   ```yaml
   amountOfAttacks: 1      # Blackhole_Skill 上配成了 1
   ```
3. 结论：**不是逻辑错，是配置值的问题**——设为 1 时当然只打一次。

**修法（做了设计改造，而不是简单改数字）**
- 玩家反馈 QTE 选择敌人的方式太老式，改成：黑洞展开完成后**角色头顶出现 "R" 提示，再按一次 `R`**，自动对黑洞内**每个敌人各攻击一次**（并且按下立即发动、打完立即结束）。
- 改造点：
  - `OnTriggerEnter2D` 只负责冻结敌人 + 记入 `targets`（不再生成 QTE 热键）；`OnTriggerExit2D` 解冻并移出列表 → 目标列表始终等于"当前在黑洞里的敌人"。
  - 持续时间结束后进入"就绪"，在**玩家头顶**生成 "R" 提示（优先用 Inspector 指定的预制体，没指定就用代码生成世界空间文字，保证不改场景也有引导）；等待 `readyWaitTime` 秒无输入则自动发动，避免技能永久卡住。
  - **两段按键的隔离**（R 同时用于起手和发动）：
    1. 第一段只在 `PlayerGroundedState` 里监听，第二段只在黑洞控制器的 `isReady` 阶段监听，两者不会在同一状态里同时成立；
    2. `Blackhole_Skill.IsActive()` 让地面上的 R 在黑洞生效期间不再重复起手；
    3. 技能冷却再兜一层。
  - 攻击改为顺序遍历 `targets`（`targetIndex` 游标），每个目标一次，间隔 `cloneAttackCooldown`；打完全部目标**立即收尾**（不再等待固定延迟）。
  - 删除 `Blackhole_Hotkey_Controller` 与 QTE 预制体。
- 顺带修掉一个原版隐患：黑洞被 `Destroy` 时 Unity **不会补发 `OnTriggerExit2D`**，敌人会永久保持冻结。现在在 `FinishBlackholeAbility()` 和 `OnDestroy()` 里都会 `UnfreezeAllTargets()`。

**验证方式**：黑洞内放 3 个敌人 → 等展开完成 → 按左键 → 应看到 3 次分身攻击、每个敌人各挨一次；不按键时 2.5 秒后自动发动。

---

## 案例二：按 QTE 键时同时放出水晶

**现象**：黑洞 QTE 期间按 `F` 选敌人，结果同时触发了水晶技能。

**证据**：黑洞预制体里的 QTE 键位池（Unity 把 `List<KeyCode>` 序列化成十六进制整数数组）：

```
keyCodeList: 65000000 71000000 77000000 66000000 74000000
             101(E)    113(Q)    119(W)    102(F)    116(T)
```

键位池里有 `F`，而 `Player.Update()` 里水晶是**无条件**监听 `F` 的：

```csharp
if (Input.GetKeyDown(KeyCode.F)) { skill.crystal.CanUseSkill(); }   // 没有任何状态判断
```

**根因**：两个问题叠加——① 键位冲突；② **技能输入没有"现在能不能放技能"的统一约束**（这也意味着攻击动画中、受击硬直中都能随手放技能）。

**修法**
1. 按需求把水晶改成"**闪避时在起点生成**"，删除 `F` 键释放的入口（键位冲突自然消失）。
2. 给输入加**状态门禁**：闪避只能在 Idle / Move / Jump / Air / WallSlide / WallJump 且非 busy 时触发（`Player.CanStartDash()`）。这是"技能输入约束"的通用解法：把"什么时候能放"收敛到一处判断。

**验证方式**：攻击动画中按 `Shift` 不应打断攻击；黑洞演出中按 `Shift` 不再生成水晶。

---

## 案例三：投掷剑命中敌人后一直转，几秒后突然消失

**现象**：剑命中敌人后继续转，过几秒直接消失，主角手里又出现一把剑。

**排查过程**
1. 查剑的模式配置：`Untitled.unity` 里 `swordType: 1` → 枚举 `Regular=0, Bounce=1, Pierce=2, Spin=3`，所以剑处于 **Bounce（弹射）模式**。
2. 弹射逻辑：命中后 `StuckInto()` 把剑的刚体设为 kinematic 并冻结，然后由 `BounceLogic()` 用 `MoveTowards(目标, bounceSpeed * dt)` **逐帧挪向目标**，依次弹完 `bounceAmount` 个敌人后自动回旋。
3. 再看两个关键配置/代码：
   - `bounceSpeed: 2`（每秒 2 个单位，非常慢）；
   - `SetupSword()` 里 `Invoke("DestroyMe", 7)` —— **写死 7 秒后无条件销毁**。
4. 结论：**剑还在弹射途中就被 7 秒的定时器销毁了**。剑被销毁后 `player.sword` 变成 null，`PlayerGroundedState.HasNoSword()` 判定"手里没剑"，于是可以再次投掷（看起来就是"手里又出现一把剑"）。

**修法**
1. `destroy` 时长抽成可配置字段 `swordLifetime`（默认 12 秒），并写明它是"安全网"，正常生命周期由状态（弹射/旋转/回旋）决定。
2. `BounceLogic()` 加健壮性：目标列表为空 → 立刻回旋；目标在途中被销毁 → 从列表剔除；找不到敌人时不再让剑停在原地等超时。
3. 顺手修掉 `SpinLogic()` 的一个真 bug：
   ```csharp
   // 原代码：条件写成 wasStopped，导致停下后每帧都再触发一次 StopWhenSpinning()，
   // 而它会把 spinTimer 重置 → 剑永远转下去、永远不回旋
   if (... > maxTravelDistance && wasStopped)   // ❌
   if (!wasStopped && ... > maxTravelDistance)  // ✅
   ```
4. `bounceSpeed` 在 Inspector 调整到 8~12（配置层面）。

**验证方式**：向 3 个以上敌人投剑，观察剑是否按顺序弹射完并自动飞回主角手中（接剑动画播放）。

---

## 案例四：剑"扔出去立刻自己飞回来"，看起来像根本没扔（一次自己引入的回归）

**现象**：鼠标右键松手后，主角播了投掷动作、随后身体向后一顿（很像接剑动作），但看起来没有剑飞出去；反复几次都这样。

**排查过程**
1. 先确认投掷链路本身没被改坏：投掷是由 `PlayerThrowSword.anim` 上的动画事件 `SwordTrigger` → `PlayerAnimationTriggers.SwordTrigger()` → `Sword_Skill.CreatSword()` 触发的，这条链完好。
2. 再看"向后一顿"来自哪里：`PlayerCatchSwordState.Enter()` 里有
   `rb.velocity = new Vector2(player.swordReturnImpact * -player.facingDir, ...)` —— **这是接剑状态独有的后退**。
3. 也就是说剑其实扔出去了，只是**立刻进入 `isReturning` 并飞回主角**，于是玩家看到的是"扔一下就接回来"。
4. 定位到上一轮自己的改动：为了让"目标全部消失的剑不要卡在原地等超时销毁"，我在 `BounceLogic()` 里加了"目标列表为空 → 立刻回旋"。
   但 `isBouncing` 只是"这把剑配置为弹射模式"，**并不代表它已经命中敌人进入弹射阶段**。
   当剑先撞到墙/地面而插住时，目标列表本来就是空的 → 于是每帧都被判定为"该回旋了"，剑立刻飞回主角。

**根因**：把"配置状态"和"运行阶段"混用一个标记，导致新加的保护逻辑在错误的前提下生效。

**修法**：新增 `bounceActive`（只在**真的收集到敌人目标**后才置 true），`BounceLogic()` 只接管 `bounceActive` 为 true 的剑；
撞墙插住的剑维持原版行为（留在原地等玩家右键召回）。

**教训**：**修复 bug 时新加的分支，一定要问"这个前提在所有路径上都成立吗"**——这次的回归正是"目标为空"这个前提在撞墙路径上不成立造成的。

**验证方式**：朝墙壁扔剑 → 剑应插在墙上不动（可按右键召回）；朝敌人扔剑 → 剑依次弹射后自动回旋。

---

## 顺带修掉的代码质量问题

| 问题 | 位置 | 修法 |
|---|---|---|
| 字符串版协程调用（走反射，拼错只在运行时暴露） | `Entity.Damage()`、`PlayerAimSwordState`、`PlayerCatchSwordState`、`Sword_Skill_Controller` | 改为直接传枚举器：`StartCoroutine(fX.FlashFX())` |
| 初始化顺序隐患：技能引用在 `Start()` 里赋值，别的脚本可能在 `Update()` 里已用到 | `SkillManager` | 挪到 `Awake()`（所有 Awake 早于任何 Start） |
| `CanUseSkill()` 既是查询又会释放技能，命名误导 | `Skill.cs` | 拆出纯查询 `IsReady()`，保留原行为并补注释 |
| 水晶追踪时空目标会抛空引用 | `Crystal_Skill_Controller.Update` | 加 `cloestEnemy != null` 判断 |
| 水晶结束可能重复触发爆炸/重复销毁 | `Crystal_Skill_Controller` | 加 `hasFinished` 标记 |
| 分身生成时"先找最近敌人、后摆位置"，搜索原点是错的 | `Clone_Skill.CreatClone` | 先设置位置，再 `FindClosestEnemy` |
| 无用 using（`Unity.VisualScripting`、`UnityEngine.VR` 等）与调试 `Debug.Log` 残留 | 多个文件 | 清理 |

---

## 这次过程中"AI 辅助"的分工（面试可讲）

| 环节 | AI 做了什么 | 我做了什么 |
|---|---|---|
| 定位"只攻击一次" | 读代码 + 解析场景序列化数据，指出是 `amountOfAttacks: 1` 的配置问题 | 判断这不符合作品需求，决定**改设计**而不是改数字 |
| 定位"QTE 键与技能键冲突" | 解析预制体里的十六进制键位数组，指出键位池包含 F | 决定把水晶改为闪避触发，并给技能输入加状态门禁 |
| 定位"剑消失" | 串出"弹射 → 速度慢 → 7 秒硬销毁 → 引用变 null"的因果链 | 在 Unity 里实际跑一遍确认现象，并把安全网时长做成可配置 |
| 改造方案 | 给出改造后的代码与讲解 | 决定最终玩法设计（一键打全部敌人 / 闪避出水晶）并对每一项跑测 |

**一句话总结**：AI 负责"读得快、串因果"，我负责"定方案、做取舍、跑验证"。
