using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillCooldownUI : MonoBehaviour
{
    public PlayerAttack attack;  // 플레이어 공격 스크립트 참조
    public Image radial;             // 원형 덮개 Image
    public TMP_Text secondsText;     // 남은 초(선택)

    void Update()
    {
        if (!attack || !radial) return;

        float ratio = attack.Skill1CooldownRatio;  // 0~1 (1=막 눌렀다, 0=준비완료)
        radial.fillAmount = ratio;
        radial.enabled = ratio > 0f;               // 쿨일 때만 덮개 보이기

        if (secondsText)
            secondsText.text = ratio > 0f ? Mathf.Ceil(attack.skill1Cooldown * ratio).ToString() : "";
    }
}