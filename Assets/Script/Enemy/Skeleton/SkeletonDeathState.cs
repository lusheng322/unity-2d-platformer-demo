using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 骷髅死亡状态，Enter 时把 Animator 的 Death 置 true；销毁靠基类 Enemy.Die() 的延时
public class SkeletonDeathState : EnemyState
{
    private Enemy_Skeleton enemy;

    public SkeletonDeathState(Enemy _enemyBase, EnemyStateMachine _stateMachine, string _animBoolName, Enemy_Skeleton _enemy)
        : base(_enemyBase, _stateMachine, _animBoolName, _enemy)
    {
        this.enemy = _enemy;
    }

    public override void Enter()
    {
        base.Enter();               // base 里会把 Animator 的 Death 参数置 true

        enemy.SetZeroVelocity();    // 死亡后不再移动
    }

    public override void Update()
    {
        base.Update();              // 这里没别的逻辑，动画播完就销毁
    }

    public override void Exit()
    {
        base.Exit();
    }
}
