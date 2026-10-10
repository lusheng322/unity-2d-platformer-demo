using System;
using System.Threading;
using TMPro;
using UnityEngine;

public enum SwordType
{
    Regular,
    Bounce,
    Pierce,
    Spin,
}

public class Sword_Skill : Skill
{
    public SwordType swordType = SwordType.Regular;

    [Header("Bounce Info")]
    [SerializeField] private int bounceAmount;
    [SerializeField] private float bounceGravity;
    [SerializeField] private int bounceSpeed;

    [Header("Pirece Info")]
    [SerializeField] private int pireceAmount;
    [SerializeField] private float pireceGravity;

    [Header("Spin Info")]
    [SerializeField] private float hitCooldown = .35f;
    [SerializeField] private float maxTravelDistance = 7;
    [SerializeField] private float spinDuration = 2;
    [SerializeField] private float spinGravity = 1;

    [Header("Skill Info")]
    [SerializeField] private GameObject swordPrefab;
    [SerializeField] private Vector2 launchForce;
    [SerializeField] private float swordGravity;
    [SerializeField] private float returnSpeed;
    [SerializeField] private float freezeTimeDuration;

    private Vector2 finalDir;

    [Header("Aim Dots")]
    [SerializeField] private GameObject dotPrefab;
    [SerializeField] private int numberOfDots;
    [SerializeField] private float spaceBetweenDots;
    [SerializeField] private Transform dotsParent;

    private GameObject[] dots;

    protected override void Start()
    {
        base.Start();

        GenerateDots();

        SetupGravity();
    }

    private void SetupGravity()
    {
        switch (swordType)
        {
            case SwordType.Bounce:
                swordGravity = bounceGravity;
                break;
            case SwordType.Pierce:
                swordGravity = pireceGravity;
                break;
            case SwordType.Spin:
                swordGravity = spinGravity;
                break;
        }
    }

    protected override void Update()
    {
        DebugSwitchSwordType();
        UpdateModeHint();

        if (Input.GetKeyUp(KeyCode.Mouse1))
        {
            finalDir = new Vector2(AimDirection().normalized.x * launchForce.x, AimDirection().normalized.y * launchForce.y);
        }

        if (Input.GetKey(KeyCode.Mouse1))
        {
            for (int i = 0; i < dots.Length; i++)
            {
                dots[i].transform.position = DotsPosition(i * spaceBetweenDots);
            }
        }

    }

    // 录 demo 用的调试快捷键：全屏下改不了 Inspector，直接按键切投掷剑模式
    // F1 普通 / F2 弹射 / F3 穿透 / F4 旋转，想换键就改下面的 KeyCode
    private void DebugSwitchSwordType()
    {
        SwordType? next = null;

        if (Input.GetKeyDown(KeyCode.F1)) next = SwordType.Regular;
        else if (Input.GetKeyDown(KeyCode.F2)) next = SwordType.Bounce;
        else if (Input.GetKeyDown(KeyCode.F3)) next = SwordType.Pierce;
        else if (Input.GetKeyDown(KeyCode.F4)) next = SwordType.Spin;

        if (next == null)
            return;

        if (next.Value != swordType)
        {
            swordType = next.Value;
            SetupGravity();     // 换模式必须重算重力，不然还是上一个模式的手感
        }

        ShowModeHint();

        Debug.Log($"投掷剑模式：{swordType}（下一次投掷生效）");
    }

    // 屏幕左上角的模式小字（录 demo 用，代码生成，不用做预制体）
    // 文本故意用英文：项目里只有拉丁字体，中文会显示成方块
    private TextMeshProUGUI modeHint;
    private float modeHintTimer;
    private const float modeHintHoldTime = 2f;      // 显示 2 秒后开始淡出
    private const float modeHintFadeSpeed = 2f;

    private void ShowModeHint()
    {
        if (modeHint == null)
            modeHint = CreateModeHint();

        modeHint.text = "SWORD: " + SwordTypeLabel();
        modeHint.color = new Color(1f, 1f, 1f, 1f);
        modeHint.gameObject.SetActive(true);
        modeHintTimer = modeHintHoldTime;
    }

    private TextMeshProUGUI CreateModeHint()
    {
        GameObject canvasGo = new GameObject("SwordModeHint");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;              // 压在别的 UI 上面

        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(canvasGo.transform, false);

        TextMeshProUGUI text = textGo.AddComponent<TextMeshProUGUI>();
        text.fontSize = 40;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.raycastTarget = false;

        RectTransform rt = text.rectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = new Vector2(30f, -30f);
        rt.sizeDelta = new Vector2(700f, 80f);

        return text;
    }

    private void UpdateModeHint()
    {
        if (modeHint == null || !modeHint.gameObject.activeSelf)
            return;

        if (modeHintTimer > 0f)
        {
            modeHintTimer -= Time.deltaTime;
            return;
        }

        Color c = modeHint.color;
        c.a -= modeHintFadeSpeed * Time.deltaTime;
        modeHint.color = c;

        if (c.a <= 0f)
            modeHint.gameObject.SetActive(false);
    }

    private string SwordTypeLabel()
    {
        switch (swordType)
        {
            case SwordType.Bounce: return "BOUNCE";
            case SwordType.Pierce: return "PIERCE";
            case SwordType.Spin: return "SPIN";
            default: return "REGULAR";
        }
    }

    public void CreatSword()
    {
        GameObject newSword = Instantiate(swordPrefab, player.transform.position, transform.rotation);
        Sword_Skill_Controller newSwordScript = newSword.GetComponent<Sword_Skill_Controller>();

        if(swordType == SwordType.Bounce)
        {
            newSwordScript.SetupBounce(true,bounceAmount,bounceSpeed);
        }
        else if(swordType == SwordType.Pierce)
        {
            newSwordScript.SetupPirece(pireceAmount);
        }
        else if(swordType == SwordType.Spin)
        {
            newSwordScript.SetupSpin(true, maxTravelDistance, hitCooldown, spinDuration);
        }

        newSwordScript.SetupSword(finalDir, swordGravity, player, freezeTimeDuration,returnSpeed);

        player.AssignNewSword(newSword);

        DotsActive(false);
    }

    #region Aim region
    public Vector2 AimDirection()
    {
        Vector2 playerPosition = player.transform.position;
        Vector2 mousePositon = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 direction = mousePositon - playerPosition;

        return direction;
    }

    public void DotsActive(bool _isActive)
    {
        for (int i = 0; i < dots.Length; i++)
        {
            dots[i].SetActive(_isActive);
        }
    }

    public void GenerateDots()
    {
        dots = new GameObject[numberOfDots];
        for (int i = 0; i < numberOfDots; i++)
        {
            dots[i] = Instantiate(dotPrefab, player.transform.position, Quaternion.identity, dotsParent);
            dots[i].SetActive(false);
        }
    }

    private Vector2 DotsPosition(float t)
    {
        Vector2 position = (Vector2)player.transform.position + new Vector2(
            AimDirection().normalized.x * launchForce.x,
            AimDirection().normalized.y * launchForce.y) * t + .5f * (Physics2D.gravity * swordGravity) * (t * t);

        return position;
    }
    #endregion
}
