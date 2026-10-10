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
    [SerializeField] private float crystalSpawnInterval = .08f;   // 每个水晶之间的间隔

    // 挂在这儿方便在 Inspector 里对着效果调
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

        // 放 Awake 不放 Start，所有 Awake 都在 Start 之前，别处取 instance.dash 才不会空
        dash = GetComponent<Dash_Skill>();
        clone = GetComponent<Clone_Skill>();
        sword = GetComponent<Sword_Skill>();
        blackhole = GetComponent<Blackhole_Skill>();
        crystal = GetComponent<Crystal_Skill>();
    }

}
