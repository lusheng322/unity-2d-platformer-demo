using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerGroundedState : PlayerState
{
    public PlayerGroundedState(PlayerStateMachine _stateMachine, Player _player, string _animBoolName) : base(_stateMachine, _player, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        // R 第一段起手。黑洞还在生效就不重复起手，免得和二段抢读
        if (Input.GetKeyDown(KeyCode.R) && !player.skill.blackhole.IsActive())
        {
            stateMachine.ChangeState(player.blackhole);
        }

        // isBusy 期间不让召回剑，免得状态被连续打断
        if (Input.GetKeyDown(KeyCode.Mouse1) && !player.isBusy && HasNoSword())
        {
            stateMachine.ChangeState(player.aimSword);
        }

        if (Input.GetKeyDown(KeyCode.Q))
        {
            stateMachine.ChangeState(player.counterAttackState);
        }

        if (!player.IsGroundDetected())
        {
            stateMachine.ChangeState(player.airState);
        }

        if(Input.GetKeyDown(KeyCode.Space) && player.IsGroundDetected())
        {
            stateMachine.ChangeState(player.jumpState);
        }

        // 剑扔出去了手里没武器，不能普攻
        if (Input.GetKeyDown(KeyCode.Mouse0) && !player.sword)
        {
            stateMachine.ChangeState(player.primaryAttack);
        }
    }

    private bool HasNoSword()
    {
        if (!player.sword)
        {
            return true;
        }

        // 只有插在墙/地上的剑能右键召回，飞行/弹射/旋转中的不行
        Sword_Skill_Controller swordController = player.sword.GetComponent<Sword_Skill_Controller>();
        if (swordController != null && swordController.CanBeRecalled())
        {
            swordController.ReturnSword();
        }

        return false;
    }
}
    
