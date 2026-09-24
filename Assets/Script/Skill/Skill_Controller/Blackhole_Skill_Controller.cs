using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// 黑洞技能的运行时控制器（挂在 Blackhole 预制体上）。
///
/// 设计流程：
///   1. 展开：把进入范围的敌人冻结（FreezeTime）并记入 targets（敌人离开则移出并解冻）；
///   2. 就绪：黑洞**长到接近最大尺寸**后进入"就绪"，玩家头顶出现 "R" 提示；
///   3. 发动：就绪阶段再按一次 R → 分身依次对 targets 中每个敌人各攻击一次；
///   4. 收尾：**等所有分身攻击结束后**才收尾（解除敌人冻结、黑洞收缩销毁、玩家恢复显形）；
///   5. 取消：就绪阶段在 readyWaitTime 秒内没有按 R → 直接结束技能、不发动攻击（把选择权留给玩家）。
///
/// 按键隔离：R 既用于第一段（地面起手）又用于第二段（黑洞内发动），通过三层隔离避免冲突——
///   · 第一段只在 PlayerGroundedState 里监听；第二段只在 isReady 阶段监听（此时玩家处于 PlayerBlackholeState）；
///   · 就绪之前本控制器完全不读输入；
///   · Blackhole_Skill.IsActive() 让地面上的 R 在黑洞生效期间不再重复起手。
/// 因此黑洞阶段除了 R 之外的其他按键都不会有任何效果。
/// </summary>
public class Blackhole_Skill_Controller : MonoBehaviour
{
    [Header("Ready Hint（可选）")]
    [SerializeField] private GameObject readyHintPrefab;    // 自定义提示预制体（留空则用代码生成一个"R"）
    [SerializeField] private float readyWaitTime = 2.5f;    // 就绪后等待按键的时限；超时则取消本次技能
    [SerializeField] private float readyHintHeight = 2f;    // 提示显示在玩家头顶的高度
    [SerializeField] private float hintWorldScale = 0.01f;  // 提示的整体缩放（世界空间 Canvas 用）
    [SerializeField] private string readyHintSortingLayer = "Ground";   // 提示所在的 Sorting Layer（默认 Ground：要盖住背景）

    [Header("收尾")]
    [SerializeField] private float finishWaitLimit = 6f;    // 等待分身全部结束的安全上限（秒），防止意外卡住

    private float maxsize;
    private float growSpeed;
    private float shrinkSpeed;
    private float blackholeTimer;
    private float readyTimer;

    private float cloneAttackCooldown = .25f;
    private float cloneAttackTimer;

    private bool canGrow = true;
    private bool canShrink;
    private bool canAttack;                 // 正在爆发中
    private bool isReady;                   // 已就绪，等待玩家再按一次 R
    private bool canDisappear = true;

    private bool waitClonesFinished;        // 分身已全部放出，正在等它们打完
    private float finishWaitTimer;

    private int targetIndex;

    private readonly List<Transform> targets = new List<Transform>();
    private readonly List<GameObject> spawnedClones = new List<GameObject>();
    private GameObject readyHintInstance;

    /// 玩家是否可以退出黑洞状态（由玩家状态机轮询）
    public bool playerCanExitState { get; private set; }

    public void SetupBlackhole(float _maxsize, float _growSpeed, float _shrinkSpeed, float _blackholeDuration)
    {
        maxsize = _maxsize;
        growSpeed = _growSpeed;
        shrinkSpeed = _shrinkSpeed;
        blackholeTimer = _blackholeDuration;
    }

    private void Update()
    {
        cloneAttackTimer -= Time.deltaTime;
        blackholeTimer -= Time.deltaTime;

        // 1) 展开完成 → 进入就绪。
        //    判定用"实际尺寸长到接近 maxsize"，而不是等 blackholeDuration 走完：
        //    这样提示出现的时间点和玩家肉眼看到的"完全展开"是一致的；
        //    blackholeDuration 作为兜底（万一 growSpeed 配得太小也能进入就绪）。
        if (!isReady && !canAttack && !canShrink && !waitClonesFinished && HasFullyExpanded())
            EnterReadyState();

        // 2) 就绪阶段：只监听 R；超时则取消本次技能（不发动攻击）
        if (isReady)
        {
            readyTimer -= Time.deltaTime;
            FollowPlayerWithHint();

            if (Input.GetKeyDown(KeyCode.R))
            {
                StartReleaseAttack();
            }
            else if (readyTimer < 0f)
            {
                CancelBlackhole();
            }
        }

        // 3) 等所有分身打完再收尾（finishWaitLimit 是安全上限）
        if (waitClonesFinished)
        {
            finishWaitTimer -= Time.deltaTime;

            if (finishWaitTimer < 0f || AllClonesFinished())
                FinishBlackholeAbility();
        }

        CloneAttackLogic();
        GrowAndShrink();
    }

    /// 是否已经长到接近最大尺寸（Lerp 是渐近的，所以取 90% 作为"展开完成"）
    private bool HasFullyExpanded()
    {
        return blackholeTimer < 0f || transform.localScale.x >= maxsize * .9f;
    }

    /// 进入"就绪"：头顶显示 R 提示，等待玩家决定是否发动
    private void EnterReadyState()
    {
        isReady = true;
        readyTimer = readyWaitTime;

        ShowReadyHint();
    }

    /// <summary>
    /// 在玩家头顶显示 "R" 提示。
    ///
    /// 生成方式参考原来 QTE 的做法（世界空间文字挂在头顶）：
    /// 用世界空间 Canvas + TextMeshProUGUI，TMP 资源项目里已经有了（之前的 QTE 用的就是它）。
    /// 如果你想用图片/精灵样式的提示，可以自己做一个世界空间预制体挂到 readyHintPrefab 上，代码会优先用它。
    /// </summary>
    private void ShowReadyHint()
    {
        if (readyHintInstance != null)
            return;

        Player player = PlayerManager.instance != null ? PlayerManager.instance.player : null;
        if (player == null)
            return;

        Vector3 spawnPos = player.transform.position + Vector3.up * readyHintHeight;

        if (readyHintPrefab != null)
        {
            readyHintInstance = Instantiate(readyHintPrefab, spawnPos, Quaternion.identity);
            return;
        }

        readyHintInstance = CreateDefaultReadyHint(spawnPos);
    }

    private GameObject CreateDefaultReadyHint(Vector3 _position)
    {
        GameObject root = new GameObject("ReadyHint_R");
        root.transform.position = _position;
        root.transform.localScale = Vector3.one * hintWorldScale;

        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        canvas.sortingLayerName = readyHintSortingLayer;   // 关键：和背景对齐渲染层级，否则会被背景盖住
        canvas.sortingOrder = 100;

        RectTransform rootRt = root.GetComponent<RectTransform>();
        if (rootRt != null)
            rootRt.sizeDelta = new Vector2(200f, 200f);

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(root.transform, false);

        RectTransform textRt = textGo.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textGo.AddComponent<TextMeshProUGUI>();
        text.text = "R";
        text.fontSize = 120f;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;

        // 世界空间文字让相机看正面（避免出现镜像/背面）
        Camera cam = Camera.main;
        if (cam != null)
            root.transform.rotation = cam.transform.rotation;

        return root;
    }

    /// 提示跟随玩家（不挂成子物体：角色翻转时会父子一起旋转，文字会镜像）
    private void FollowPlayerWithHint()
    {
        if (readyHintInstance == null || PlayerManager.instance == null || PlayerManager.instance.player == null)
            return;

        readyHintInstance.transform.position = PlayerManager.instance.player.transform.position + Vector3.up * readyHintHeight;
    }

    /// 玩家按下 R：开始对全部目标发动攻击
    private void StartReleaseAttack()
    {
        isReady = false;
        canAttack = true;
        targetIndex = 0;
        cloneAttackTimer = 0f;

        DestroyReadyHint();

        if (canDisappear)
        {
            canDisappear = false;
            PlayerManager.instance.player.MakeTransparent(true);
        }

        // 范围内没有敌人 → 直接收尾
        if (targets.Count <= 0)
            FinishBlackholeAbility();
    }

    /// 就绪阶段超时未按键：取消本次技能（不发动攻击、玩家保持显形）
    private void CancelBlackhole()
    {
        isReady = false;
        canAttack = false;
        canShrink = true;
        playerCanExitState = true;

        DestroyReadyHint();
        UnfreezeAllTargets();
    }

    /// 依次对每个目标各发动一次分身攻击
    private void CloneAttackLogic()
    {
        if (!canAttack || cloneAttackTimer > 0f)
            return;

        while (targetIndex < targets.Count)
        {
            Transform target = targets[targetIndex];
            targetIndex++;

            if (target == null)     // 目标已被销毁 → 跳过
                continue;

            cloneAttackTimer = cloneAttackCooldown;

            float xOffset = Random.Range(0, 100) > 50 ? 2f : -2f;
            GameObject newClone = SkillManager.instance.clone.CreatClone(target, new Vector3(xOffset, 0));

            if (newClone != null)
                spawnedClones.Add(newClone);

            return;                 // 一帧只放一个分身，节奏交给 cloneAttackCooldown
        }

        // 全部目标都放完分身了 → 不再攻击，转入"等分身结束"阶段
        canAttack = false;
        waitClonesFinished = true;
        finishWaitTimer = finishWaitLimit;
    }

    /// 之前生成的分身是否都已经消失（自己的动画播完后会自销毁）
    private bool AllClonesFinished()
    {
        for (int i = spawnedClones.Count - 1; i >= 0; i--)
        {
            if (spawnedClones[i] == null)
                spawnedClones.RemoveAt(i);
        }

        return spawnedClones.Count <= 0;
    }

    /// 收尾：允许玩家退出状态、黑洞进入收缩、解除敌人冻结
    private void FinishBlackholeAbility()
    {
        playerCanExitState = true;
        canAttack = false;
        isReady = false;
        canShrink = true;
        waitClonesFinished = false;

        DestroyReadyHint();
        UnfreezeAllTargets();
    }

    private void GrowAndShrink()
    {
        if (canGrow && !canShrink)
        {
            transform.localScale = Vector2.Lerp(transform.localScale, new Vector2(maxsize, maxsize), growSpeed * Time.deltaTime);
        }

        if (canShrink)
        {
            transform.localScale = Vector2.Lerp(transform.localScale, new Vector2(-1, -1), shrinkSpeed * Time.deltaTime);

            if (transform.localScale.x < 0)
            {
                canShrink = false;
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        Enemy enemy = collision.GetComponent<Enemy>();
        if (enemy == null)
            return;

        enemy.FreezeTime(true);

        if (!targets.Contains(collision.transform))
            targets.Add(collision.transform);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        Enemy enemy = collision.GetComponent<Enemy>();
        if (enemy == null)
            return;

        enemy.FreezeTime(false);
        targets.Remove(collision.transform);
    }

    /// <summary>
    /// 解除所有目标的冻结。
    /// 注意：黑洞被销毁时 Unity 不会补发 OnTriggerExit2D，所以必须在这里手动解冻，
    /// 否则敌人会永久停在原地。
    /// </summary>
    private void UnfreezeAllTargets()
    {
        foreach (Transform t in targets)
        {
            if (t == null)
                continue;

            Enemy enemy = t.GetComponent<Enemy>();
            if (enemy != null)
                enemy.FreezeTime(false);
        }

        targets.Clear();
    }

    private void DestroyReadyHint()
    {
        if (readyHintInstance != null)
            Destroy(readyHintInstance);
    }

    private void OnDestroy()
    {
        DestroyReadyHint();
        UnfreezeAllTargets();
    }
}
