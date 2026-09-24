using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkeletonStunnedState : EnemyState
{
    private Enemy_Skeleton enemy;
    public SkeletonStunnedState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Skeleton _enemy) : base(_enemyBase, _stateMachine, _animBoolName, _enemy)
    {
        this.enemy = _enemy;
    }

    public override void Enter()
    {
        base.Enter();

        enemy.fX.InvokeRepeating("RedColorBlink", 0, .1f);

        stateTimer = enemy.stunDuration;

        rb.velocity = new Vector2(-enemy.facingDir * enemy.stunDirection.x,enemy.stunDirection.y);
    }

    public override void Exit()
    {
        base.Exit();

        enemy.fX.Invoke("CancelRedBlink", 0);
    }

    public override void Update()
    {
        base.Update();

        if(stateTimer < 0 )
        {
            stateMachine.ChangeState(enemy.idleState);
        }
    }
}
