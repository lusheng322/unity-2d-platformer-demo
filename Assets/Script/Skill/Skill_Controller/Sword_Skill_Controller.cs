using System.Collections.Generic;
using UnityEngine;

public class Sword_Skill_Controller : MonoBehaviour
{
    private Animator anim;
    private Rigidbody2D rb;
    private CircleCollider2D cd;
    private Player player;

    [Header("Lifetime")]
    [SerializeField] private float swordLifetime = 12f;      // 安全网：无论处于什么状态，超时后销毁，避免剑永久留在场上

    private bool canRotate = true;
    private bool isReturning;
    private bool isStuckInEnvironment;      // 是否插在墙/地面上（只有这种状态才允许玩家右键召回）

    private float freezeTimeDuratiom;
    private float returnSpeed;

    [Header("Pirece Info")]
    private int pireceAmount;

    [Header("Bounce Info")]
    private int bounceSpeed;
    private bool isBouncing;
    private bool bounceActive;              // 是否已经收集到目标、真正进入"弹射追击"阶段
    private int bounceAmount;
    private List<Transform> EnemyTarget;
    private int targetIndex;

    [Header("Spin Info")]
    private float maxTravelDistance;
    private float spinDuration;
    private float spinTimer;
    private bool wasStopped;
    private bool isSpinning;
    private float spinDirection;

    private float hitTimer;
    private float hitCooldown;


    private void Awake()
    {
        anim = GetComponentInChildren<Animator>();
        rb = GetComponent<Rigidbody2D>();
        cd = GetComponent<CircleCollider2D>();
    }

    private void DestroyMe()
    {
        Destroy(gameObject);
    }

    public void SetupSword(Vector2 _dir, float _gravityScale, Player _player, float _freezeTimeDuration, float _returnSpeed)
    {
        player = _player;
        freezeTimeDuratiom = _freezeTimeDuration;
        returnSpeed = _returnSpeed;

        rb.velocity = _dir;
        rb.gravityScale = _gravityScale;

        if(pireceAmount <= 0) 
            anim.SetBool("Rotation", true);
        
        spinDirection = Mathf.Clamp(rb.velocity.x, -1, 1);

        // 原来写死 7 秒：弹射/旋转还没走完剑就被销毁，玩家会觉得"剑莫名其妙消失了"。
        // 现在改成可配置的安全网时长，并且下面的 BounceLogic 会在目标失效时主动让剑回旋。
        Invoke(nameof(DestroyMe), swordLifetime);

    }
    public void SetupBounce(bool _isBouncing, int _amountOfBounce, int _bounceSpeed)
    {
        isBouncing = _isBouncing;
        bounceAmount = _amountOfBounce;
        bounceSpeed = _bounceSpeed;

        EnemyTarget = new List<Transform>();
    }


    public void SetupPirece(int _pierceAmount)
    {
        pireceAmount = _pierceAmount;
    }

    public void SetupSpin(bool _isSpinning, float _maxTravelDistance, float _hitCooldown, float _spinDuration)
    {
        isSpinning = _isSpinning;
        maxTravelDistance = _maxTravelDistance;
        hitCooldown = _hitCooldown;
        spinDuration = _spinDuration;
    }

    public void ReturnSword()
    {
        rb.constraints = RigidbodyConstraints2D.FreezeAll;
        transform.parent = null;
        isStuckInEnvironment = false;
        isReturning = true;
    }

    /// <summary>
    /// 是否允许玩家用右键召回这把剑。
    /// 只有"插在墙/地面上的剑"（isStuckInEnvironment）可以召回；
    /// 飞行中、弹射追击中、原地旋转中的剑都不允许，避免召回时机错乱引起的一系列问题。
    /// </summary>
    public bool CanBeRecalled() => isStuckInEnvironment && !isReturning;

    private void Update()
    {
        if (canRotate)
        {
            transform.right = rb.velocity;
        }

        if (isReturning)
        {
            transform.position = Vector2.MoveTowards(transform.position, player.transform.position, returnSpeed * Time.deltaTime);

            // 只有玩家处于"可以接剑"的状态才接。
            // 万一玩家正在黑洞/冲刺演出中，剑就一直跟着玩家飞，等回到普通状态再接住，
            // 避免强行切状态把演出打断（角色掉下来但黑洞还在）。
            if (Vector2.Distance(transform.position, player.transform.position) < 1 && player.CanCatchSword())
            {
                player.CatchTheSword();
            }
        }

        // 插在墙/地面上的剑：玩家走过去就能直接捡回来（不需要再按右键）
        if (isStuckInEnvironment && !isReturning
            && Vector2.Distance(transform.position, player.transform.position) < 1f
            && player.CanCatchSword())
        {
            player.CatchTheSword();
        }

        BounceLogic();
        SpinLogic();
    }

    private void SpinLogic()
    {
        if (isSpinning)
        {
            // 飞出 maxTravelDistance 后就地停下进入旋转。
            // 注意这里必须是 !wasStopped：原代码写的是 wasStopped，导致停下之后每帧都会再触发一次
            // StopWhenSpinning()，而它会把 spinTimer 重置成 spinDuration —— 结果是剑永远转下去、永远不回旋。
            if (!wasStopped && Vector2.Distance(player.transform.position, transform.position) > maxTravelDistance)
            {
                StopWhenSpinning();
            }

            if (wasStopped)
            {
                spinTimer -= Time.deltaTime;

                transform.position = Vector2.MoveTowards(transform.position, new Vector2(transform.position.x + spinDirection, transform.position.y), 1.5f * Time.deltaTime);

                if (spinTimer < 0)
                {
                    isReturning = true;
                    isSpinning = false;
                }

                hitTimer -= Time.deltaTime;

                if (hitTimer < 0)
                {
                    hitTimer = hitCooldown;

                    Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 1);

                    foreach (var hit in colliders)
                    {
                        if (hit.GetComponent<Enemy>() != null)
                            swordSkillDamage(hit.GetComponent<Enemy>());
                    }
                }
            }
        }
    }

    private void StopWhenSpinning()
    {
        wasStopped = true;
        rb.constraints = RigidbodyConstraints2D.FreezePosition;
        spinTimer = spinDuration;
    }

    private void BounceLogic()
    {
        // 注意这里的 bounceActive：
        // isBouncing 表示"这把剑的配置是弹射模式"，而 bounceActive 表示"已经命中敌人、开始依次弹射目标"。
        // 只有后者才由本方法接管剑的移动。
        // 如果剑是先撞到墙/地面而插住（没收集到任何敌人），就应该保持插在原地等玩家召回，
        // 否则会出现"刚扔出去就自己飞回来、主角原地接剑"的怪现象。
        if (!isBouncing || !bounceActive)
            return;

        // 目标全部消失 → 立刻回旋，别让剑停在原地等超时销毁
        if (EnemyTarget == null || EnemyTarget.Count <= 0)
        {
            isBouncing = false;
            bounceActive = false;
            isReturning = true;
            return;
        }

        if (targetIndex >= EnemyTarget.Count)
            targetIndex = 0;

        Transform target = EnemyTarget[targetIndex];

        // 目标在弹射途中被销毁 → 从列表里剔除，换下一个目标
        if (target == null)
        {
            EnemyTarget.RemoveAt(targetIndex);
            return;
        }

        transform.position = Vector2.MoveTowards(transform.position, target.position, bounceSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, target.position) < .1f)
        {
            Enemy enemy = target.GetComponent<Enemy>();
            if (enemy != null)
                swordSkillDamage(enemy);

            targetIndex++;
            bounceAmount--;

            if (bounceAmount <= 0)
            {
                isBouncing = false;
                bounceActive = false;
                isReturning = true;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {

        if (isReturning)
            return;

        if (collision.GetComponent<Enemy>() != null)
        {
            Enemy enemy = collision.GetComponent<Enemy>();

            swordSkillDamage(enemy);
        }

        SetupTargetsForBounce(collision);

        StuckInto(collision);
    }

    private void swordSkillDamage(Enemy enemy)
    {
        enemy.Damage();
        // 直接传枚举器，替代字符串版 StartCoroutine("FreezeTimerFor", ...)（避免反射调用与拼写风险）
        enemy.StartCoroutine(enemy.FreezeTimerFor(freezeTimeDuratiom));
    }

    private void SetupTargetsForBounce(Collider2D collision)
    {
        if (collision.GetComponent<Enemy>() != null)
        {
            if (isBouncing && EnemyTarget.Count <= 0)
            {
                Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 20);

                foreach (var hit in colliders)
                {
                    if (hit.GetComponent<Enemy>() != null)
                        EnemyTarget.Add(hit.transform);
                }

                // 只有真的收集到目标，才进入"弹射追击"阶段；
                // 没收集到（比如周围没有敌人）就让剑按原逻辑插在原地，等玩家右键召回。
                bounceActive = EnemyTarget.Count > 0;
            }
        }
    }

    private void StuckInto(Collider2D collision)
    {
        if (pireceAmount > 0 && collision.GetComponent<Enemy>() != null)
        {
            pireceAmount--;
            return;
        }

        if (isSpinning)
        {
            // 撞到敌人：就地停下继续旋转（对范围内敌人持续伤害）
            if (collision.GetComponent<Enemy>() != null)
            {
                StopWhenSpinning();
                return;
            }

            // 撞到墙/地面：结束旋转，走到下面"扎住"的流程 → 直接扎在墙/地面上
            isSpinning = false;
        }

        canRotate = false;
        cd.enabled = false;

        rb.isKinematic = true;
        rb.constraints = RigidbodyConstraints2D.FreezeAll;

        if (isBouncing && EnemyTarget.Count > 0)
            return;

        anim.SetBool("Rotation", false);
        transform.parent = collision.transform;
        isStuckInEnvironment = true;        // 插在墙/地面上了，此时才允许玩家右键召回
    }
}
