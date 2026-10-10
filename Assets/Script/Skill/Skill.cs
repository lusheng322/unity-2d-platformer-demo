using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Skill : MonoBehaviour
{
    [SerializeField] protected float cooldown;
    protected float cooldownTimer;

    protected Player player;

    protected virtual void Start()
    {
        player = PlayerManager.instance.player;
    }

    protected virtual void Update()
    {
        cooldownTimer -= Time.deltaTime;
    }

    // 只判断冷却好没好，没副作用
    public virtual bool IsReady() => cooldownTimer < 0f;

    // 名字像判断，其实冷却好了会直接放技能；只想判断用 IsReady()
    public virtual bool CanUseSkill()
    {
        if (!IsReady())
        {
            Debug.Log($"{GetType().Name} is on cooldown");
            return false;
        }

        UseSkill();
        cooldownTimer = cooldown;
        return true;
    }

    public virtual void UseSkill()
    {

    }

    public virtual Transform FindClosestEnemy(Transform _checkTransform)
    {

        Collider2D[] colliders = Physics2D.OverlapCircleAll(_checkTransform.position, 25);

        float closestDistance = Mathf.Infinity;

        Transform closestEnemy = null;

        foreach (var hit in colliders)
        {
            if (hit.GetComponent<Enemy>() != null)
            {
                float distanceToEnemy = Vector2.Distance(_checkTransform.position, hit.transform.position);

                if (distanceToEnemy < closestDistance)
                {
                    closestDistance = distanceToEnemy;
                    closestEnemy = hit.transform;
                }

            }
        }

        return closestEnemy;
    }
}
