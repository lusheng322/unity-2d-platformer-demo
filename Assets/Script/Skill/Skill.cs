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

    /// <summary>
    /// 纯查询：冷却好了没有。没有副作用，可以放心用于"能不能放"的判断。
    /// </summary>
    public virtual bool IsReady() => cooldownTimer < 0f;

    /// <summary>
    /// 判断并释放：冷却好就调用 UseSkill() 并重置冷却。
    /// 注意这个方法有副作用（原来的命名容易让人误以为它只是查询），
    /// 只想要判断时请用 IsReady()。
    /// </summary>
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
