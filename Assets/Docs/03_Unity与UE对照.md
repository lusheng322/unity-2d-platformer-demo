# Unity 与 UE 对照 · 以及"为什么投 Unity 岗"

> 用途：投 Unity 岗时把 UE 经历讲成"可迁移能力"；也避免被问"你 UE 的东西和 Unity 有什么关系"时答不出。

---

## 一、核心对照表（面试可以直接说 2~3 组）

| 概念 | Unity | UE5 |
|---|---|---|
| 对象模型 | GameObject + Component | Actor + Component |
| 脚本基类与生命周期 | `MonoBehaviour`：Awake → Start → Update → FixedUpdate → LateUpdate → OnDestroy | `AActor/UObject`：构造 → PostInitializeComponents → BeginPlay → Tick → EndPlay |
| 编辑器暴露 | `[SerializeField]` / `public` | `UPROPERTY(EditAnywhere)` |
| 拿组件 | `GetComponent<T>()`（建议 Awake 缓存） | `FindComponentByClass<T>()` / 构造期 `CreateDefaultSubobject` |
| 事件解耦 | C# `event` / `Action` / `UnityEvent` | 委托 / EventDispatcher / 多播委托 |
| 定时与延迟 | Coroutine、`Invoke`、`WaitForSeconds` | Timer Manager、Timeline、`SetTimer` |
| 动画 | Animator Controller（状态 + 参数 + Transition）、Animation Clip、Animation Event | 动画蓝图 + 状态机 + Montage + AnimNotify |
| 物理 | Rigidbody2D / Collider2D / `Physics2D` 查询 | 组件物理 / 碰撞通道 / 重叠与射线 |
| 内存管理 | C# 托管堆 + GC（有 `IDisposable`，但析构不确定） | UObject GC（标记清除、分帧）+ `UPROPERTY` 维持引用 |
| 场景组织 | Scene + Prefab | Level/World + Blueprint |
| UI | uGUI（Canvas / Text / Image） | UMG / Slate |
| 性能工具 | Profiler、Frame Debugger、Stats 面板 | `stat unit` / Insights / ProfileGPU |

## 二、常见追问怎么答

**Q：你 UE 项目里哪些做法在 Unity 里同样成立？**
> "对象模型和组件化思路是一样的：UE 里我用 `IHitInterface` 做命中解耦，让武器不认识具体的敌人类型；Unity 里就是事件/接口 + 组件通信。状态机也一样，我在两边都是'每个状态一个类 + Enter/Update/Exit'。真正需要重学的只是 API 和工具链，比如 UE 的 GC 换成 C# 的 GC、Timer 换成协程、AnimBP 换成 Animator。"

**Q：Unity 你熟吗？（既然简历主项目是 UE）**
> "熟到能独立完成一个 2D 动作 demo：状态机驱动的玩家控制、技能系统、敌人 AI、Tilemap 关卡、Cinemachine 相机。我知道 Unity 侧的坑，比如 `Update` 和 `FixedUpdate` 的区别、协程不是线程、`GetComponent` 要缓存、UGUI 的 Canvas 重建成本。"

**Q：为什么不投 UE 岗？**
> 不要说"UE 岗位少"或贬低 UE，改说方向匹配 + 双引擎经历（见下）。

---

## 三、"为什么投 Unity 岗"30 秒话术

> "我大二从 Unity 入门，做过一个 2D 平台动作项目——状态机驱动的玩家控制、技能系统、敌人 AI 都是自己写的；后来因为想做 3D 动作游戏，转到 UE 用 C++ 做了完整的战斗项目。这次投 Unity 岗不是把 UE 的经历硬套过来，有三个原因：第一，两个引擎我都实际做过项目，Unity 侧的 C#、组件模型、协程、UGUI 不是纸上谈兵；第二，我理解两者的差异主要在 API 和工具链，架构层的东西是通用的；第三，我最想做的方向是动作/2D 玩法，Unity 在这类项目上更多，所以我愿意来。"

**说完主动补一句（加分）**：
> "比如命中判定解耦，我在 UE 里是用接口 + 阵营 Tag，在 Unity 里就是事件 + 组件查询——思路完全一样，只是语法不同。"

**要避免的答法**：
- ❌ "UE 岗位太少/门槛太高，所以投 Unity"（听起来像退而求其次）
- ❌ "两个引擎差不多"（等于说你不懂差异）
- ❌ 只讲 UE 项目，完全不提 Unity 项目（面试官会怀疑你没真正上手过 Unity）

---

## 四、投 Unity 岗必须额外准备的八股（简历写了 Unity，就一定会被问）

**C# / Unity 特有**
- 值类型 vs 引用类型、装箱拆箱、`struct` vs `class`
- **GC 与内存**：托管堆、`GC.Collect` 的代价、`IDisposable` / `using`
- 委托与事件、闭包捕获
- **`Update` / `FixedUpdate` / `LateUpdate`** 的区别；物理为什么放 `FixedUpdate`
- **协程的原理**（迭代器 + 状态机，**不是线程**）、协程与 `async/await` 的区别
- `MonoBehaviour` 生命周期顺序
- **UGUI**：Canvas 渲染模式、Canvas 重建/合批、`Image` 与 `RawImage`
- **Animator**：参数、Transition、Animation Event、Layer/IK
- 对象池、`Instantiate/Destroy` 的开销
- Profiler / Frame Debugger / Stats
- 图集与 DrawCall 合批、AssetBundle 与热更新（了解）

**通用（两个引擎都要）**
- 数据结构与算法、C++/C# 基础、操作系统基础、网络基础（见备考路线图）
