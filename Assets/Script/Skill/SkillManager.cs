using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkillManager : MonoBehaviour
{
    public static SkillManager instance;
    
    public Dash_Skill dash {  get; private set; }
    public Clone_Skill clone { get; private set; }
    public Sword_Skill sword { get; private set; }
    public Blackhole_Skill blackhole { get; private set; }
    public Crystal_Skill crystal { get; private set; }

    [Header("水晶生成（闪避）")]
    [SerializeField] private float crystalSpawnInterval = .08f;   // 多个水晶之间的生成间隔（秒）

    /// 供 Crystal_Skill 读取：放在 SkillManager 上方便对着实际效果微调
    public float CrystalSpawnInterval => crystalSpawnInterval;
    

    private void Awake()
    {
        if (instance != null)
        {
            Destroy(instance.gameObject);
        }
        else
        {
            instance = this;
        }

        // 技能引用的赋值放在 Awake 而不是 Start：
        // 所有对象的 Awake 都早于任何 Start，这样才能保证别的脚本在 Start/Update 里
        // 通过 SkillManager.instance.dash 取技能时已经拿得到，不会出现空引用。
        dash = GetComponent<Dash_Skill>();
        clone = GetComponent<Clone_Skill>();
        sword = GetComponent<Sword_Skill>();
        blackhole = GetComponent<Blackhole_Skill>();
        crystal = GetComponent<Crystal_Skill>();
    }

}
