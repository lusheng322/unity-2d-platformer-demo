using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

// 黑洞运行时控制：进范围的敌人冻结并记进 targets，长到接近 maxsize 后头顶出 R，再按一次 R 逐个放分身打；
// 就绪超时就直接取消不发动。起手 R 由 PlayerGroundedState 管，二段 R 只在这里 isReady 时读，不会抢。
public class Blackhole_Skill_Controller : MonoBehaviour
{
    [Header("Ready Hint（可选）")]
    [SerializeField] private GameObject readyHintPrefab;    // 不填就用代码生成一个 R
    [SerializeField] private float readyWaitTime = 2.5f;    // 就绪后等按键的时限，超时取消
    [SerializeField] private float readyHintHeight = 2f;    // 提示挂在头顶多高
    [SerializeField] private float hintWorldScale = 0.01f;  // 世界空间 Canvas 的缩放
    [SerializeField] private string readyHintSortingLayer = "Ground";   // 得盖住背景，所以放 Ground 层

    [Header("收尾")]
    [SerializeField] private float finishWaitLimit = 6f;    // 等分身结束的安全上限，防卡死

    private float maxsize;
    private float growSpeed;
    private float shrinkSpeed;
    private float blackholeTimer;
    private float readyTimer;

    private float cloneAttackCooldown = .25f;
    private float cloneAttackTimer;

    private bool canGrow = true;
    private bool canShrink;
    private bool canAttack;                 // 正在爆发
    private bool isReady;                   // 已就绪，等玩家再按一次 R
    private bool canDisappear = true;

    private bool waitClonesFinished;        // 分身放完了，在等它们打完
    private float finishWaitTimer;

    private int targetIndex;

    private readonly List<Transform> targets = new List<Transform>();
    private readonly List<GameObject> spawnedClones = new List<GameObject>();
    private GameObject readyHintInstance;

    // 玩家能不能退出黑洞状态，状态机每帧来轮询
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

        // 按实际尺寸判断展开完成，提示出现的时间和肉眼看到的展开一致
        if (!isReady && !canAttack && !canShrink && !waitClonesFinished && HasFullyExpanded())
            EnterReadyState();

        // 就绪阶段只等 R，超时就取消不发动
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

        // 等分身打完再收尾，finishWaitLimit 兜底
        if (waitClonesFinished)
        {
            finishWaitTimer -= Time.deltaTime;

            if (finishWaitTimer < 0f || AllClonesFinished())
                FinishBlackholeAbility();
        }

        CloneAttackLogic();
        GrowAndShrink();
    }

    // Lerp 渐近永远到不了 1，所以取 90% 算展开完成
    private bool HasFullyExpanded()
    {
        return blackholeTimer < 0f || transform.localScale.x >= maxsize * .9f;
    }

    // 进就绪：头顶出 R，等玩家按
    private void EnterReadyState()
    {
        isReady = true;
        readyTimer = readyWaitTime;

        ShowReadyHint();
    }

    // 世界空间 Canvas + TMP 挂头顶；挂了 readyHintPrefab 就优先用它
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
        canvas.sortingLayerName = readyHintSortingLayer;   // 得跟背景一个层，不然会被盖住
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

        // 让相机看正面，不然文字是反的
        Camera cam = Camera.main;
        if (cam != null)
            root.transform.rotation = cam.transform.rotation;

        return root;
    }

    // 不挂成子物体：角色翻转会带着一起转，文字镜像
    private void FollowPlayerWithHint()
    {
        if (readyHintInstance == null || PlayerManager.instance == null || PlayerManager.instance.player == null)
            return;

        readyHintInstance.transform.position = PlayerManager.instance.player.transform.position + Vector3.up * readyHintHeight;
    }

    // 按下 R，开始逐个目标放分身
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

        // 范围内没敌人就直接收尾
        if (targets.Count <= 0)
            FinishBlackholeAbility();
    }

    // 超时没按 R，直接取消不发动
    private void CancelBlackhole()
    {
        isReady = false;
        canAttack = false;
        canShrink = true;
        playerCanExitState = true;

        DestroyReadyHint();
        UnfreezeAllTargets();
    }

    // 每个目标各放一个分身
    private void CloneAttackLogic()
    {
        if (!canAttack || cloneAttackTimer > 0f)
            return;

        while (targetIndex < targets.Count)
        {
            Transform target = targets[targetIndex];
            targetIndex++;

            if (target == null)     // 目标没了就跳过
                continue;

            cloneAttackTimer = cloneAttackCooldown;

            float xOffset = Random.Range(0, 100) > 50 ? 2f : -2f;
            GameObject newClone = SkillManager.instance.clone.CreatClone(target, new Vector3(xOffset, 0));

            if (newClone != null)
                spawnedClones.Add(newClone);

            return;                 // 一帧只放一个，节奏靠 cloneAttackCooldown
        }

        // 都放完了，转入等分身结束
        canAttack = false;
        waitClonesFinished = true;
        finishWaitTimer = finishWaitLimit;
    }

    // 分身播完动画会自销毁，这里顺手把空引用清掉
    private bool AllClonesFinished()
    {
        for (int i = spawnedClones.Count - 1; i >= 0; i--)
        {
            if (spawnedClones[i] == null)
                spawnedClones.RemoveAt(i);
        }

        return spawnedClones.Count <= 0;
    }

    // 收尾：放玩家出去、黑洞开始收缩、解冻敌人
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

    // Destroy 时不会补发 OnTriggerExit2D，这里手动解冻，否则敌人永久冻结
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
