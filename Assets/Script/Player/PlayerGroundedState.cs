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

        // R 是黑洞技能的第一段（起手）。加 IsActive() 判断做隔离：
        // 黑洞技能还在生效时不再重复起手，避免同一个按键被第一段和第二段抢读。
        if (Input.GetKeyDown(KeyCode.R) && !player.skill.blackhole.IsActive())
        {
            stateMachine.ChangeState(player.blackhole);
        }

        // 加 isBusy 判断：刚结束瞄准/接剑等动作的短暂锁定期内不允许召回剑，避免状态被连续打断
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

        // 剑投出去的时候手里没有武器 → 不能普通攻击（符合直觉）
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

        // 只有"插在墙/地面上的剑"才允许用右键召回；
        // 刚扔出去还在飞、正在弹射敌人、正在原地旋转的剑，右键不生效。
        Sword_Skill_Controller swordController = player.sword.GetComponent<Sword_Skill_Controller>();
        if (swordController != null && swordController.CanBeRecalled())
        {
            swordController.ReturnSword();
        }

        return false;
    }
}
    
