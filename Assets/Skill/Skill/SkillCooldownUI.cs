using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Image))]
public class SkillCooldownUI : MonoBehaviour
{
    public enum Which { Skill1, Skill2 }
    public Which which = Which.Skill2;

    public PlayerAttack attack;          // PlayerAttack만 드래그
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
        if (c.a > 0f) baseAlpha = c.a;   // 인스펙터에서 준 알파가 있으면 그 값 사용
        SetRatio(0f);
    }

    void Update()
    {
        if (!attack) return;

        float r = which == Which.Skill1
            ? attack.Skill1CooldownRatio
            : attack.Skill2CooldownRatio;

        r = Mathf.Clamp01(r);
        SetRatio(r);

        if (secondsText)
        {
            float cd = which == Which.Skill1 ? attack.skill1Cooldown : attack.skill2Cooldown;
            secondsText.text = r > 0f ? Mathf.Ceil(cd * r).ToString() : "";
        }

        if (debugLog) Debug.Log($"[CooldownUI-{which}] ratio={r:0.00}");
    }

    void SetRatio(float r)
    {
        // 절대 비활성화/SetActive 하지 않음 → 아이콘은 항상 보이고, 덮개만 투명도/채움으로 제어
        img.fillAmount = r;
        var c = img.color; c.a = (r > 0f) ? baseAlpha : 0f; img.color = c;
    }
}