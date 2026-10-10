using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 水晶运行时：计时、追最近敌人、生长、爆炸
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

    private bool hasFinished;       // 防止重复爆炸/重复销毁

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

        // 附近没敌人时 cloestEnemy 是空的，不判 null 会报空引用
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

    // 会爆炸的播动画（动画事件里再调 SelfDestroy），不爆的直接销毁
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

    // 爆炸动画事件调的，对圈内敌人造成伤害
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
