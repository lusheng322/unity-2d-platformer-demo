using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 水晶数据层。UseSkill 是键盘入口（还没绑键），CreatCrystal 由闪避调用
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
    [SerializeField] private float fallbackSpawnInterval = .08f;  // SkillManager 没配时的兜底
    [SerializeField] private int maxActiveCrystals = 12;          // 场上最多留几个，超了回收最旧的

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

    // 闪避时调用，位置就固定在传进来的点上，不随机散开
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
            // 都在同一个点生成
            GameObject newCrystal = Instantiate(crystalPrefab, _position, Quaternion.identity);
            activeCrystals.Add(newCrystal);
            LimitCrystals();

            Crystal_Skill_Controller newCrystalScript = newCrystal.GetComponent<Crystal_Skill_Controller>();

            // 先摆到位置再找敌人，不然从世界原点搜会找错
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

    // 超上限只清最旧的。一按闪避就清空上一批的话，水晶会凭空消失
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
