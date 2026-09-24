using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 水晶技能的数据层。
/// 两个入口：
///   1. UseSkill()         —— 键盘释放（目前未绑定按键，保留给后续"与水晶换位"的扩展）；
///   2. CreatCrystal(位置) —— 闪避时调用：在"按下闪避键那一刻的位置"依次生成多个水晶。
/// </summary>
public class Crystal_Skill : Skill
{
    [SerializeField] private float crystalDuraiton;
    [SerializeField] private GameObject crystalPrefab;
    private GameObject currentCrystal;

    [Header("Crystal Explosive")]
    [SerializeField] private bool canExplode;

    [Header("Crystal Move")]
    [SerializeField] private bool canMove;
    [SerializeField] private float moveSpeed;

    [Header("Multiple Crystals（闪避生成）")]
    [SerializeField] private int crystalAmount = 3;              // 一次生成几个
    [SerializeField] private float fallbackSpawnInterval = .08f;  // SkillManager 上没配时的兜底间隔
    [SerializeField] private int maxActiveCrystals = 12;          // 场上最多保留几个（超出只回收最旧的）

    private readonly List<GameObject> activeCrystals = new List<GameObject>();

    public override void UseSkill()
    {
        base.UseSkill();

        if (currentCrystal == null)
        {
            currentCrystal = Instantiate(crystalPrefab, player.transform.position, Quaternion.identity);
            Crystal_Skill_Controller currentCrystalScript = currentCrystal.GetComponent<Crystal_Skill_Controller>();
            currentCrystalScript.SetupCrystal(crystalDuraiton, canExplode, canMove, moveSpeed, FindClosestEnemy(currentCrystal.transform));

        }
        else
        {
            if (canMove)
                return;

            Vector2 playerPo = player.transform.position;
            player.transform.position = currentCrystal.transform.position;
            currentCrystal.transform.position = playerPo;

            currentCrystal.GetComponent<Crystal_Skill_Controller>()?.FinishCrystal();
        }
    }

    /// <summary>
    /// 在指定位置依次生成多个水晶（闪避时调用）。
    /// 位置固定为传进来的点（也就是按下闪避键那一刻的位置），不做随机散布；
    /// 多个水晶之间的时间间隔取自 SkillManager，方便在 Inspector 里对着效果微调。
    /// </summary>
    public void CreatCrystal(Vector3 _position)
    {
        PruneCrystals();
        StartCoroutine(CreatCrystalRoutine(_position));
    }

    private IEnumerator CreatCrystalRoutine(Vector3 _position)
    {
        int amount = Mathf.Max(1, crystalAmount);
        float interval = SkillManager.instance != null ? SkillManager.instance.CrystalSpawnInterval : fallbackSpawnInterval;

        for (int i = 0; i < amount; i++)
        {
            // 全部生成在同一个位置（按下闪避键那一刻的坐标）
            GameObject newCrystal = Instantiate(crystalPrefab, _position, Quaternion.identity);
            activeCrystals.Add(newCrystal);
            LimitCrystals();

            Crystal_Skill_Controller newCrystalScript = newCrystal.GetComponent<Crystal_Skill_Controller>();

            // 先 Instantiate 到目标位置，再搜索最近的敌人 ——
            // 否则会从预制体默认位置（世界原点）搜索，找到的目标是错的。
            newCrystalScript.SetupCrystal(crystalDuraiton, canExplode, canMove, moveSpeed, FindClosestEnemy(newCrystal.transform));

            if (interval > 0f && i < amount - 1)
                yield return new WaitForSeconds(interval);
        }

        cooldownTimer = cooldown;
    }

    private void PruneCrystals()
    {
        for (int i = activeCrystals.Count - 1; i >= 0; i--)
        {
            if (activeCrystals[i] == null)
                activeCrystals.RemoveAt(i);
        }
    }

    /// <summary>
    /// 超过上限时只回收"最旧的"水晶。
    /// 这样"闪避后水晶还没打完就再闪一次"时，上一批水晶不会被立刻清掉
    /// （之前的写法是一按闪避就清空上一批，导致水晶凭空消失）。
    /// </summary>
    private void LimitCrystals()
    {
        PruneCrystals();

        while (activeCrystals.Count > Mathf.Max(1, maxActiveCrystals))
        {
            GameObject oldest = activeCrystals[0];
            activeCrystals.RemoveAt(0);

            if (oldest == null)
                continue;

            Crystal_Skill_Controller script = oldest.GetComponent<Crystal_Skill_Controller>();
            if (script != null)
                script.SelfDestroy();
        }
    }
}
