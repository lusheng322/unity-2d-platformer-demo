using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Entity : MonoBehaviour
{
    [Header("Knocked Info")]
    [SerializeField] protected Vector2 knockBackDirection;
    [SerializeField] protected float knockBackDuration;
    protected bool isKnocked;

    [Header("Collision Info")]
    public Transform attackCheck;
    public float attackCheckRadius;
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckDistance;
    [SerializeField] private LayerMask whatIsGround;
    [SerializeField] protected Transform wallCheck;
    [SerializeField] private float wallCheckDistance;

    public float facingDir { get; private set; } = 1;
    protected bool facingRight = true;


    #region Components
    public Animator anim { get; private set; }
    public Rigidbody2D rb { get; private set; }
    public EntityFX fX { get; private set; }
    public SpriteRenderer sr { get; private set; }
    #endregion

    protected virtual void Awake()
    {

    }

    protected virtual void Start()
    {
        sr = GetComponentInChildren<SpriteRenderer>();
        anim = GetComponentInChildren<Animator>();
        fX = GetComponent<EntityFX>();
        rb = GetComponent<Rigidbody2D>();
    }

    protected virtual void Update()
    {
        
    }

    public virtual void Damage()
    {
        // 别用字符串版 StartCoroutine，走反射，名字写错只有运行时才知道
        if (fX != null)
            fX.StartCoroutine(fX.FlashFX());

        StartCoroutine(HitKnockBack());
    }

    private IEnumerator HitKnockBack()
    {
        isKnocked = true;

        rb.velocity = new Vector2(knockBackDirection.x * -facingDir, knockBackDirection.y);
        
        yield return new WaitForSeconds(knockBackDuration);
        isKnocked = false;
    }

    #region Velocity
    public void SetZeroVelocity()
    {
        if (isKnocked)
            return;

        rb.velocity = new Vector2(0, 0);
    }

    public void SetVelocity(float _xVelocity, float _yVelocity)
    {
        if (isKnocked) 
            return;

        rb.velocity = new Vector2(_xVelocity, _yVelocity);
        FlipController(_xVelocity);
    }
    #endregion

    #region Collsion
    public virtual bool IsGroundDetected() => Physics2D.Raycast(groundCheck.position, Vector2.down, groundCheckDistance, whatIsGround);
    public virtual bool IsWallDetected()
    {
        Vector2 dir = Vector2.right * facingDir;

        if (Physics2D.Raycast(wallCheck.position, dir, wallCheckDistance, whatIsGround))
            return true;

        // wallCheck 摆在物体中心的话，射线从中心出发还没走到墙就到头了（敌人的 WallCheck 就是这种），
        // 所以再补一次从碰撞体前沿往外打的检测；玩家的墙跳不受影响，原来那条照样生效
        Collider2D col = GetComponent<Collider2D>();
        if (col == null)
            return false;

        Vector2 origin = new Vector2(col.bounds.center.x + col.bounds.extents.x * facingDir, wallCheck.position.y);
        return Physics2D.Raycast(origin, dir, wallCheckDistance, whatIsGround);
    }
    protected virtual void OnDrawGizmos()
    {
        Gizmos.DrawLine(groundCheck.position, new Vector3(groundCheck.position.x, groundCheck.position.y - groundCheckDistance));
        Gizmos.DrawLine(wallCheck.position, new Vector3(wallCheck.position.x + wallCheckDistance, wallCheck.position.y));
        Gizmos.DrawWireSphere(attackCheck.position, attackCheckRadius);
    }
    #endregion

    #region Flip
    public virtual void Flip()
    {
        facingRight = !facingRight;
        facingDir *= -1;
        transform.Rotate(0, 180, 0);
    }

    public virtual void FlipController(float _x)
    {
        if ((_x > 0 && !facingRight) || (_x < 0 && facingRight))
        {
            Flip();
        }
    }
    #endregion

    public void MakeTransparent(bool _transparent)
    {
        if(_transparent)
            sr.color = Color.clear;
        else
            sr.color = Color.white;
    }
}
