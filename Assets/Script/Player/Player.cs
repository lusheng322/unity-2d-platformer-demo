using System.Collections;
using UnityEngine;

// 玩家：状态机、状态实例、技能引用都在这儿
// 技能输入在各状态类里判断，方便约束什么时候能放什么
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

    // 黑洞、冲刺这种演出中不许接剑，强切 catchSword 会把演出打断
    // 典型表现：投剑后按 R 放黑洞，剑飞回来角色从空中掉下来，黑洞还在
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

    // 闪避的门禁：只有能自由行动的状态才让闪
    // 以前在 Update 里无条件跑，攻击硬直、黑洞演出里都能按 Shift 强切
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
