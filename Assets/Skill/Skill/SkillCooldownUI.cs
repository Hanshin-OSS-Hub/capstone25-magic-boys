using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(Image))]
public class SkillCooldownUI : MonoBehaviour
{
    public enum Which { Q, E, R, T, Y }
    public Which which = Which.Q;

    public MagicAttack magicAttack;
    public TMP_Text secondsText;

    private Image img;
    private float baseAlpha = 0.6f;

    void Awake()
    {
        img = GetComponent<Image>();

        if (!magicAttack)
            magicAttack = Object.FindFirstObjectByType<MagicAttack>();

        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Radial360;
        img.fillOrigin = (int)Image.Origin360.Top;
        img.fillClockwise = true;
        img.raycastTarget = false;

        var c = img.color;
        if (c.a > 0f) baseAlpha = c.a;

        SetRatio(0f);

        if (secondsText)
            secondsText.text = "";
    }

    void Update()
    {
        if (!magicAttack) return;

        int slotIndex = (int)which;

        if (SkillProgressionManager.Instance != null &&
            !SkillProgressionManager.Instance.IsUnlocked(slotIndex))
        {
            SetRatio(0f);
            if (secondsText) secondsText.text = "";
            return;
        }

        float ratio = magicAttack.GetCooldownRatio(slotIndex);
        ratio = Mathf.Clamp01(ratio);
        SetRatio(ratio);

        if (secondsText)
        {
            float cd = magicAttack.GetCooldownDuration(slotIndex);
            secondsText.text = ratio > 0f ? Mathf.Ceil(cd * ratio).ToString() : "";
        }
    }

    void SetRatio(float r)
    {
        img.fillAmount = r;

        var c = img.color;
        c.a = (r > 0f) ? baseAlpha : 0f;
        img.color = c;
    }
}