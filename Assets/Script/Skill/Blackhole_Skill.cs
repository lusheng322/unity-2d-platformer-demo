using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 黑洞技能的"数据层"：负责实例化黑洞、把配置参数交给控制器，并对外提供"技能是否结束"的查询。
/// 攻击次数不再由这里配置——新设计的规则是"每个在黑洞内的敌人各被打一次"。
/// </summary>
public class Blackhole_Skill : Skill
{
    [SerializeField] private GameObject BlackholePrefab;
    [SerializeField] private float maxsize;
    [SerializeField] private float growSpeed;
    [SerializeField] private float shrinkSpeed;
    [SerializeField] private float blackholeDuration;

    Blackhole_Skill_Controller currentBlackhole;

    public override void UseSkill()
    {
        base.UseSkill();

        GameObject newBlackhole = Instantiate(BlackholePrefab, player.transform.position, Quaternion.identity);
        currentBlackhole = newBlackhole.GetComponent<Blackhole_Skill_Controller>();
        currentBlackhole.SetupBlackhole(maxsize, growSpeed, shrinkSpeed, blackholeDuration);
    }

    /// 玩家状态机会轮询这个方法来决定何时退出黑洞状态
    public bool SkillCompleted()
    {
        if (!currentBlackhole)
            return false;

        if (currentBlackhole.playerCanExitState)
        {
            currentBlackhole = null;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 场上是否已有黑洞技能在生效。
    /// 用来把"第一段（地面按 R 起手）"和"第二段（黑洞内再按 R 发动）"隔离开：
    /// 黑洞还在生效时，地面上的 R 不再重复起手，避免一次按键被两个阶段抢读。
    /// </summary>
    public bool IsActive() => currentBlackhole != null;
}
