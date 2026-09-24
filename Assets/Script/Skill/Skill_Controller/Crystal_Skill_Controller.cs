using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 水晶的运行时控制器：计时、追踪最近敌人、生长、爆炸伤害。
/// </summary>
public class Crystal_Skill_Controller : MonoBehaviour
{
    private Animator anim => GetComponent<Animator>();
    private CircleCollider2D cd => GetComponent<CircleCollider2D>();

    private float crystalExitTimer;

    private bool canExplode;
    private bool canMove;
    private float moveSpeed;

    private bool canGrow;
    private float growSpeed = 5;

    private bool hasFinished;       // 防止重复触发爆炸/重复销毁

    private Transform cloestEnemy;

    public void SetupCrystal(float _crystalDuration, bool _canExplode, bool _canMove, float _moveSpeed, Transform _closestEnemy)
    {
        crystalExitTimer = _crystalDuration;
        canExplode = _canExplode;
        canMove = _canMove;
        moveSpeed = _moveSpeed;
        cloestEnemy = _closestEnemy;
    }

    private void Update()
    {
        if (hasFinished)
            return;

        crystalExitTimer -= Time.deltaTime;

        if (crystalExitTimer < 0)
        {
            FinishCrystal();
            return;
        }

        // 加 null 判断：范围内没有敌人时 cloestEnemy 为空，
        // 原代码会在这里抛 NullReferenceException（闪避生成水晶时很容易触发）。
        if (canMove && cloestEnemy != null)
        {
            transform.position = Vector2.MoveTowards(transform.position, cloestEnemy.transform.position, moveSpeed * Time.deltaTime);

            if (Vector2.Distance(transform.position, cloestEnemy.transform.position) < 1)
            {
                canMove = false;
                FinishCrystal();
                return;
            }
        }

        if (canGrow)
        {
            transform.localScale = Vector2.Lerp(transform.localScale, new Vector2(3, 3), growSpeed * Time.deltaTime);
        }
    }

    /// 结束水晶：会爆炸的就播爆炸动画（动画事件里再调用 SelfDestroy），否则直接销毁
    public void FinishCrystal()
    {
        if (hasFinished)
            return;

        hasFinished = true;

        if (canExplode)
        {
            canGrow = true;
            anim.SetTrigger("Explode");
        }
        else
        {
            SelfDestroy();
        }
    }

    public void SelfDestroy()
    {
        Destroy(gameObject);
    }

    /// 由爆炸动画的事件调用：对范围内的敌人造成伤害
    private void CrystalExplodeTrigger()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, cd.radius);

        foreach (var hit in colliders)
        {
            Enemy enemy = hit.GetComponent<Enemy>();
            if (enemy != null)
                enemy.Damage();
        }
    }
}
