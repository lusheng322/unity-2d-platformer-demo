using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 分身数据层，只管生成，行为交给 Clone_Skill_Controller，黑洞爆发时逐个调用
public class Clone_Skill : Skill
{
    [Header("Clone Info")]
    [SerializeField] private GameObject clonePrefab;
    [SerializeField] private float cloneDuration;
    [SerializeField] private bool canAttack;

    // 返回新分身，黑洞靠它跟踪有没有打完，决定何时收尾
    public GameObject CreatClone(Transform _clonePosition, Vector3 _offset)
    {
        GameObject newClone = Instantiate(clonePrefab);

        // 必须先摆位置再找敌人，否则从世界原点搜索会找错目标
        newClone.transform.position = _clonePosition.position + _offset;

        newClone.GetComponent<Clone_Skill_Controller>().SetupClone(_clonePosition, cloneDuration, canAttack, _offset, FindClosestEnemy(newClone.transform));

        return newClone;
    }
}
