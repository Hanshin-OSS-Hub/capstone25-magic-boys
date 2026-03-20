using UnityEngine;
using UnityEngine.UI;

public class SkillCooldownUI : MonoBehaviour
{
    public MagicAttack magicAttack;
    public Image[] cooldownMasks = new Image[5]; // Q,E,R,T,Y 순서

    void Update()
    {
        if (!magicAttack) return;

        for (int i = 0; i < cooldownMasks.Length; i++)
        {
            if (!cooldownMasks[i]) continue;
            cooldownMasks[i].fillAmount = magicAttack.GetCooldownRatio(i);
        }
    }
}