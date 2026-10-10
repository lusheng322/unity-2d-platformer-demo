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
    [SerializeField] private float deathDestroyDelay = 1.5f;     // 死亡动画播完多久销毁，按动画长度填

    private int currentHealth = -1;                              // -1 表示还没初始化，首次受伤时才初始化，免得依赖 Start 顺序
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

        if (isDead)     // 死了就别跑 AI 了，等 Die() 延时销毁
            return;

        stateMachine.currentState.Update();
    }

    // 死了就不再吃伤害，免得反复触发死亡逻辑
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

    // 停移动 + 延时销毁。死亡动画交给子类的死亡状态播（见 SkeletonDeathState），
    // 这样死亡也是状态机里的一个状态，不用在基类里直接动 Animator
    protected virtual void Die()
    {
        isDead = true;

        // 死在黑洞冻结里的话 anim.speed 是 0，先解冻，不然死亡动画不会播
        FreezeTime(false);

        if (rb != null)
            rb.velocity = Vector2.zero;

        Destroy(gameObject, deathDestroyDelay);
    }

    public virtual void FreezeTime(bool _timeFrozen)
    {
        // 死了就不再接受冻结：剑技能（弹射/旋转）会重复命中，死后再冻一次 anim.speed 就永远是 0，
        // 死亡动画播不出来。解冻要放过去，Die() 就是靠它把 anim.speed 恢复成 1 的
        if (isDead && _timeFrozen)
            return;

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

    // 改 public 是为了让武器直接 StartCoroutine(enemy.FreezeTimerFor(t))，别用字符串版
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
