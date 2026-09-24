using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerPrimaryAttack : PlayerState
{
    private int comboCounter;

    private float comboWindow = 2f;
    private float lastAttackTime;
    
    public PlayerPrimaryAttack(PlayerStateMachine _stateMachine, Player _player, string _animBoolName) : base(_stateMachine, _player, _animBoolName)
    {
    }

    public override void Enter()
    {
        base.Enter();

        xInput = 0;

        if(comboCounter == 3 || Time.time >= lastAttackTime+comboWindow)
        {
            comboCounter = 0;
        }

        player.anim.SetInteger("ComboCounter", comboCounter);

        float attackDir = player.facingDir;

        if(xInput != 0)
            attackDir = xInput;

        player.SetVelocity(player.attackMovent[comboCounter].x * attackDir, player.attackMovent[comboCounter].y);

        stateTimer = .1f;
    }

    public override void Exit()
    {
        base.Exit();

        player.StartCoroutine("BusyFor", .15f);

        comboCounter++;
        lastAttackTime = Time.time;
    }

    public override void Update()
    {
        base.Update();

        if(stateTimer < 0)
        {
            player.SetZeroVelocity();
        }
        if (triggerCalled)
        {
            stateMachine.ChangeState(player.idleState);
        }

    }
}
