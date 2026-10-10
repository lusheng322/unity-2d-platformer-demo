using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 黑洞数据层：实例化黑洞、把参数丢给控制器，每个在黑洞里的敌人各被打一次
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

    // 玩家状态机轮询这个，决定什么时候退出黑洞状态
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

    // 黑洞还在场上时地面上的 R 不再起手，免得一次按键被两段抢读
    public bool IsActive() => currentBlackhole != null;
}
