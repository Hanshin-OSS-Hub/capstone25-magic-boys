using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Image))]
public class SkillCooldownUI : MonoBehaviour
{
    public enum Which { Skill1, Skill2, Skill3, Skill4, Skill5 }
    public Which which = Which.Skill1;

    public PlayerAttack attack;          // PlayerAttack 드래그 (없으면 자동 탐색)
    public TMP_Text secondsText;         // 선택
    public bool debugLog = false;

    Image img;
    float baseAlpha = 0.6f; // 덮개 기본 알파(원하는 값)

    void Awake()
    {
        img = GetComponent<Image>();
        if (!attack) attack = FindObjectOfType<PlayerAttack>();

        // 덮개 표준 설정
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Radial360;
        img.fillOrigin = (int)Image.Origin360.Top;
        img.fillClockwise = true;
        img.raycastTarget = false;

        // 초기 상태: 보이지 않게(알파 0), fill 0
        var c = img.color;
        if (c.a > 0f) baseAlpha = c.a;   // 인스펙터 알파가 있으면 그 값 사용
        SetRatio(0f);
        if (secondsText) secondsText.text = "";
    }

    void Update()
    {
        if (!attack) return;

        int slotIndex = GetSlotIndex0Based(which);

        //  잠금 상태면 쿨 UI 숨김
        if (!IsUnlocked(slotIndex))
        {
            SetRatio(0f);
            if (secondsText) secondsText.text = "";
            return;
        }

        float ratio = GetCooldownRatio(which);
        ratio = Mathf.Clamp01(ratio);
        SetRatio(ratio);

        if (secondsText)
        {
            float cd = GetCooldownDuration(which);
            secondsText.text = ratio > 0f ? Mathf.Ceil(cd * ratio).ToString() : "";
        }

        if (debugLog) Debug.Log($"[CooldownUI-{which}] ratio={ratio:0.00}");
    }

    bool IsUnlocked(int slotIndex0Based)
    {
        // SkillProgressionManager 없으면(세팅 전) 일단 잠금으로 취급하지 않고 표시
        if (SkillProgressionManager.Instance == null) return true;
        return SkillProgressionManager.Instance.IsUnlocked(slotIndex0Based);
    }

    int GetSlotIndex0Based(Which w)
    {
        switch (w)
        {
            case Which.Skill1: return 0;
            case Which.Skill2: return 1;
            case Which.Skill3: return 2;
            case Which.Skill4: return 3;
            case Which.Skill5: return 4;
        }
        return 0;
    }

    float GetCooldownRatio(Which w)
    {
        // ✅ PlayerAttack에 아래 프로퍼티들이 있어야 함:
        // Skill1CooldownRatio ... Skill5CooldownRatio
        switch (w)
        {
            case Which.Skill1: return attack.Skill1CooldownRatio;
            case Which.Skill2: return attack.Skill2CooldownRatio;
            case Which.Skill3: return attack.Skill3CooldownRatio;
            case Which.Skill4: return attack.Skill4CooldownRatio;
            case Which.Skill5: return attack.Skill5CooldownRatio;
        }
        return 0f;
    }

    float GetCooldownDuration(Which w)
    {
        //  PlayerAttack에 아래 필드가 있어야 함:
        // skill1Cooldown ... skill5Cooldown
        switch (w)
        {
            case Which.Skill1: return attack.skill1Cooldown;
            case Which.Skill2: return attack.skill2Cooldown;
            case Which.Skill3: return attack.skill3Cooldown;
            case Which.Skill4: return attack.skill4Cooldown;
            case Which.Skill5: return attack.skill5Cooldown;
        }
        return 0f;
    }

    void SetRatio(float r)
    {
        // 아이콘은 항상 보이고, 덮개만 투명도/채움으로 제어
        img.fillAmount = r;
        var c = img.color;
        c.a = (r > 0f) ? baseAlpha : 0f;
        img.color = c;
    }
}