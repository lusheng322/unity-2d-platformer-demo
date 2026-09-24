using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 分身技能的数据层：只负责"在指定位置生成一个分身"，具体行为交给 Clone_Skill_Controller。
/// 黑洞爆发时由 Blackhole_Skill_Controller 调用，逐个攻击目标。
/// </summary>
public class Clone_Skill : Skill
{
    [Header("Clone Info")]
    [SerializeField] private GameObject clonePrefab;
    [SerializeField] private float cloneDuration;
    [SerializeField] private bool canAttack;

    /// <summary>
    /// 生成一个分身，并把生成出来的对象返回给调用方
    /// （黑洞技能靠它跟踪"分身是否已经播完攻击"，从而决定什么时候收尾）。
    /// </summary>
    public GameObject CreatClone(Transform _clonePosition, Vector3 _offset)
    {
        GameObject newClone = Instantiate(clonePrefab);

        // 先摆好位置，再搜索"最近的敌人"。
        // 原代码把 FindClosestEnemy 写在 SetupClone 的参数里，此时新分身还停在世界原点，
        // 于是搜索原点是错的（分身会朝错方向、或找不到目标）。
        newClone.transform.position = _clonePosition.position + _offset;

        newClone.GetComponent<Clone_Skill_Controller>().SetupClone(_clonePosition, cloneDuration, canAttack, _offset, FindClosestEnemy(newClone.transform));

        return newClone;
    }
}
