using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : Entity
{
    [SerializeField] private LayerMask whatIsPlayer;

    [Header("Stunned Info")]
    public float stunDuration;
    public Vector2 stunDirection;
    protected bool canBeStunned;
    [SerializeField] private GameObject counterImaged;

    [Header("Health")]
    [SerializeField] private int maxHealth = 5;                  // 血量（被打几下死）
    [SerializeField] private float deathDestroyDelay = 1.5f;     // 死亡动画播完后的销毁延时（按你的动画长度调）
    [SerializeField] private string deathBoolName = "Death";     // Enemy_AC 动画状态机里的布尔参数名

    private int currentHealth = -1;                              // -1 = 还没初始化（首次受伤时再初始化，避免依赖 Start 的调用顺序）
    private bool isDead;

    [Header("Move Info")]
    public float moveSpeed;
    public float idleTime;
    public float battleTime;
    private float defaultMoveSpeed;

    [Header("Attack Info")]
    public float attackDistance;
     public float attackCooldown;
    [HideInInspector] public float lastTimeAttacked;

    public EnemyStateMachine stateMachine {  get; private set; }

    protected override void Awake()
    {
        base.Awake();
        stateMachine = new EnemyStateMachine();

        defaultMoveSpeed = moveSpeed;
    }

    protected override void Update()
    {
        base.Update();

        if (isDead)     // 死后不再跑 AI，等死亡动画播完由 Die() 负责销毁
            return;

        stateMachine.currentState.Update();
    }

    /// <summary>
    /// 受伤：先走基类的闪白 / 击退，再扣血；血量归零后播放死亡动画并延时销毁。
    /// 死亡后不再接受伤害，避免同一个敌人被反复触发死亡逻辑。
    /// </summary>
    public override void Damage()
    {
        if (isDead)
            return;

        if (currentHealth < 0)
            currentHealth = maxHealth;

        base.Damage();

        currentHealth--;

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        isDead = true;

        // 万一死在黑洞的冻结里：FreezeTime(true) 会把 anim.speed 设成 0，
        // 这里先解除冻结，否则死亡动画不会播放。
        FreezeTime(false);

        if (rb != null)
            rb.velocity = Vector2.zero;

        if (HasAnimatorBool(deathBoolName))
            anim.SetBool(deathBoolName, true);
        else
            Debug.LogWarning($"Enemy: 动画状态机里没有名为 \"{deathBoolName}\" 的 Bool 参数，本次跳过死亡动画，{deathDestroyDelay} 秒后直接销毁。");

        Destroy(gameObject, deathDestroyDelay);
    }

    /// 检查 Animator 上是否有指定名字的 Bool 参数（没有就跳过死亡动画，保证不会报错）
    private bool HasAnimatorBool(string _name)
    {
        if (anim == null || anim.runtimeAnimatorController == null)
            return false;

        foreach (AnimatorControllerParameter p in anim.parameters)
        {
            if (p.type == AnimatorControllerParameterType.Bool && p.name == _name)
                return true;
        }

        return false;
    }

    public virtual void FreezeTime(bool _timeFrozen)
    {
        if (_timeFrozen)
        {
            moveSpeed = 0;
            anim.speed = 0;
        }
        else
        {
            moveSpeed = defaultMoveSpeed;
            anim.speed = 1;
        }
    }

    // 改成 public：让武器/技能可以用 enemy.StartCoroutine(enemy.FreezeTimerFor(t)) 直接调用，
    // 而不用字符串版 StartCoroutine("FreezeTimerFor", t)
    public virtual IEnumerator FreezeTimerFor(float _seconds)
    {
        FreezeTime(true);

        yield return new WaitForSeconds(_seconds);

        FreezeTime(false);
    }

    #region Counter Attack Window
    public virtual void OpenCounterAttackWindow()
    {
        canBeStunned = true;
        counterImaged.SetActive(true);
    }

    public virtual void CloseCounterAttackWindow()
    {
        canBeStunned= false;
        counterImaged.SetActive(false);
    }
    #endregion

    public virtual bool CanBeStunned()
    {
        if (canBeStunned)
        {
            CloseCounterAttackWindow();
            return true;
        }

        return false;
    }

    public virtual void AnimationFinishiTrigger() => stateMachine.currentState.AnimationFinishTrigger();

    public virtual RaycastHit2D IsPlayerDetected() => Physics2D.Raycast(wallCheck.position, Vector2.right * facingDir, 50, whatIsPlayer);

    protected override void OnDrawGizmos()
    {
        base.OnDrawGizmos();

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, new Vector3(transform.position.x + attackDistance * facingDir, transform.position.y));
    }
}
