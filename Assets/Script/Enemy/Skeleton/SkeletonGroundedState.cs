using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkeletonGroundedState : EnemyState
{
    protected Enemy_Skeleton enemy;
    protected Transform player;
    public SkeletonGroundedState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Skeleton _enemy) : base(_enemyBase, _stateMachine, _animBoolName, _enemy)
    {
        this.enemy = _enemy;
    }

    public override void Enter()
    {
        base.Enter();
        player = PlayerManager.instance.player.transform;
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();

        // 被墙或悬崖挡住时不进战斗，否则会和 BattleState 每帧来回切
        bool blocked = enemy.IsWallDetected() || !enemy.IsGroundDetected();

        if(!blocked && (enemy.IsPlayerDetected() || Vector2.Distance(enemy.transform.position,player.position) < 2))
        {
            stateMachine.ChangeState(enemy.battleState);
        }
    }
}
