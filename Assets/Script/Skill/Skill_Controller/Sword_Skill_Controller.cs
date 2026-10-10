using System.Collections.Generic;
using UnityEngine;

public class Sword_Skill_Controller : MonoBehaviour
{
    private Animator anim;
    private Rigidbody2D rb;
    private CircleCollider2D cd;
    private Player player;

    [Header("Lifetime")]
    [SerializeField] private float swordLifetime = 12f;      // 兜底：超时就销毁，免得剑永久留在场上

    private bool canRotate = true;
    private bool isReturning;
    private bool isStuckInEnvironment;      // 插在墙/地上，只有这种才能右键召回

    private float freezeTimeDuratiom;
    private float returnSpeed;

    [Header("Pirece Info")]
    private int pireceAmount;

    [Header("Bounce Info")]
    private int bounceSpeed;
    private bool isBouncing;
    private bool bounceActive;              // 已经收到目标，真正开始弹射了
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

        // 原来写死 7 秒，弹射没走完剑就没了；改成可配，目标没了就主动回旋
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

    // 只有插在墙/地上的剑能召回，飞着/弹射/旋转中召回会乱
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

            // 玩家能接才接。黑洞/冲刺演出中就跟着飞，等回普通状态再说
            if (Vector2.Distance(transform.position, player.transform.position) < 1 && player.CanCatchSword())
            {
                player.CatchTheSword();
            }
        }

        // 插在墙/地上的剑，走过去直接捡，不用按右键
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
            // 这里必须是 !wasStopped：原代码没加 !，停下来之后每帧都重复 StopWhenSpinning()，
            // 而它把 spinTimer 重置回 spinDuration，结果剑永远转下去不回旋
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
        // isBouncing 是配置了弹射模式，bounceActive 是真收到目标了，只有后者才接管移动
        // 先撞墙插住（一个敌人都没收到）就老实插着等召回，不然剑会自己飞回来主角原地接
        if (!isBouncing || !bounceActive)
            return;

        // 目标全没了就立刻回旋，别干等超时
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

        // 目标中途被销毁，踢出列表换下一个
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
        // 直接传枚举器，别用字符串版 StartCoroutine，拼错只有运行时才知道
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

                // 真收到目标才进入弹射，没收到就插在原地等召回
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
            // 撞到敌人就地停下继续转，持续造成伤害
            if (collision.GetComponent<Enemy>() != null)
            {
                StopWhenSpinning();
                return;
            }

            // 撞到墙/地就停转，往下走去扎住
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
        isStuckInEnvironment = true;        // 插住了，这时候才允许右键召回
    }
}
