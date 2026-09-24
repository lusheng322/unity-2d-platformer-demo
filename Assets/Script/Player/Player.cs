using System.Collections;
using UnityEngine;

/// <summary>
/// 玩家：持有状态机、各状态实例、技能引用与移动相关参数。
/// 技能输入全部由具体状态类处理（例如地面状态的鼠标/按键判断），
/// 这样"什么时候能放什么技能"就有了统一的状态约束。
/// </summary>
public class Player : Entity
{
    [Header("Attack details")]
    public Vector2[] attackMovent;
    public float counterAttackDuration = .2f;

    public bool isBusy { get; private set; }

    [Header("Move Info")]
    public float moveSpeed = 8f;
    public float jumpforce;
    public float swordReturnImpact;

    [Header("Dash Info")]
    public float dashSpeed;
    public float dashDuration;
    public float dashDir { get; private set; }

    public SkillManager skill { get; private set; }
    public GameObject sword { get; private set; }

    #region State
    public PlayerStateMachine stateMachine { get; private set; }
    public PlayerIdleState idleState { get; private set; }
    public PlayerMoveState moveState { get; private set; }
    public PlayerJumpState jumpState { get; private set; }
    public PlayerAirState airState { get; private set; }
    public PlayerWallSlideState wallSlide { get; private set; }
    public PlayerWallJumpState wallJump { get; private set; }
    public PlayerDashState dashState { get; private set; }

    public PlayerPrimaryAttack primaryAttack { get; private set; }
    public PlayerCounterAttackState counterAttackState { get; private set; }

    public PlayerAimSwordState aimSword { get; private set; }
    public PlayerCatchSwordState catchSword { get; private set; }
    public PlayerBlackholeState blackhole { get; private set; }

    #endregion

    protected override void Awake()
    {
        base.Awake();
        stateMachine = new PlayerStateMachine();

        idleState = new PlayerIdleState(stateMachine, this, "Idle");
        moveState = new PlayerMoveState(stateMachine, this, "Move");
        jumpState = new PlayerJumpState(stateMachine, this, "Jump");
        airState = new PlayerAirState(stateMachine, this, "Jump");
        dashState = new PlayerDashState(stateMachine, this, "Dash");
        wallSlide = new PlayerWallSlideState(stateMachine, this, "WallSlide");
        wallJump = new PlayerWallJumpState(stateMachine, this, "Jump");

        primaryAttack = new PlayerPrimaryAttack(stateMachine, this, "Attack");
        counterAttackState = new PlayerCounterAttackState(stateMachine, this, "CounterAttack");

        aimSword = new PlayerAimSwordState(stateMachine, this, "AimSword");
        catchSword = new PlayerCatchSwordState(stateMachine, this, "CatchSword");
        blackhole = new PlayerBlackholeState(stateMachine, this, "Jump");
    }

    protected override void Start()
    {
        base.Start();

        skill = SkillManager.instance;

        stateMachine.Initialize(idleState);
    }

    protected override void Update()
    {
        base.Update();

        stateMachine.currentState.Update();
        CheckForDashInput();
    }

    public void AssignNewSword(GameObject _newSword)
    {
        sword = _newSword;
    }

    public void CatchTheSword()
    {
        Destroy(sword);

        stateMachine.ChangeState(catchSword);
    }

    public IEnumerator BusyFor(float _seconds)
    {
        isBusy = true;

        yield return new WaitForSeconds(_seconds);
        isBusy = false;
    }

    public void AnimationTrigger() => stateMachine.currentState.AnimationFinishTrigger();

    /// <summary>
    /// 是否允许接住剑（飞回来的剑 / 插在墙地上的剑）。
    ///
    /// 黑洞、冲刺这类"演出/位移中"的状态不允许接剑：
    /// 否则会强行把状态机切到 catchSword，打断正在进行的演出
    /// （典型现象：投剑后按 R 用黑洞，剑飞回来时角色从空中掉下来，但黑洞还在）。
    /// 需要放行更多状态时，往下面这个判断里加即可。
    /// </summary>
    public bool CanCatchSword()
    {
        PlayerState current = stateMachine.currentState;

        return current != blackhole
            && current != dashState;
    }

    public void CheckForDashInput()
    {
        if (!CanStartDash())
        {
            return;
        }

        if (IsWallDetected())
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.LeftShift) && SkillManager.instance.dash.CanUseSkill())
        {
            dashDir = Input.GetAxisRaw("Horizontal");

            if (dashDir == 0)
            {
                dashDir = facingDir;
            }

            stateMachine.ChangeState(dashState);
        }
    }

    /// <summary>
    /// 闪避的"状态门禁"：只有处于可以自由行动的状态时才允许闪避。
    ///
    /// 原来 CheckForDashInput 在 Update 里无条件执行，意味着攻击中、受击硬直中、
    /// 甚至黑洞演出中都能按 Shift 强行打断（现在闪避会生成水晶，问题会更明显）。
    /// 把"什么时候能放技能"收敛成一处判断，是这类竞态问题的通用解法。
    /// </summary>
    private bool CanStartDash()
    {
        if (isBusy)
            return false;

        PlayerState current = stateMachine.currentState;

        return current == idleState
            || current == moveState
            || current == jumpState
            || current == airState
            || current == wallSlide
            || current == wallJump;
    }
}
