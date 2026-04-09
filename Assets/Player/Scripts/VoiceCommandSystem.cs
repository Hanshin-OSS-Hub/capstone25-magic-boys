using UnityEngine;

public class VoiceCommandSystem : MonoBehaviour
{
    public WhisperPure whisper;
    public PlayerInput input;
    public MagicAttack magicAttack;

    void Awake()
    {
        if (!magicAttack) magicAttack = GetComponent<MagicAttack>();
        if (!input) input = GetComponent<PlayerInput>();
    }

    void Update()
    {
        if (!whisper || !magicAttack) return;

        string text = whisper.GetText();
        if (string.IsNullOrEmpty(text)) return;

        text = text.ToLower().Trim();

        // Q - Fireball
        if (text.Contains("fireball") || text.Contains("fire"))
        {
            magicAttack.SelectSkill(MagicAttack.SkillSlot.Q);
            Debug.Log("[VoiceCommand] Select Fireball (Q)");
        }

        // E - Water Field
        if (text.Contains("water") || text.Contains("ice"))
        {
            magicAttack.SelectSkill(MagicAttack.SkillSlot.E);
            Debug.Log("[VoiceCommand] Select Water Field (E)");
        }

        // R - Earth Wall
        if (text.Contains("earth") || text.Contains("wall") || text.Contains("rock"))
        {
            magicAttack.SelectSkill(MagicAttack.SkillSlot.R);
            Debug.Log("[VoiceCommand] Select Earth Wall (R)");
        }

        // T - Thunder Rain
        if (text.Contains("thunder") || text.Contains("lightning") || text.Contains("storm"))
        {
            magicAttack.SelectSkill(MagicAttack.SkillSlot.T);
            Debug.Log("[VoiceCommand] Select Thunder Rain (T)");
        }

        // Y - Reserved (Skill 5)
        if (text.Contains("ultimate") || text.Contains("skill y"))
        {
            magicAttack.SelectSkill(MagicAttack.SkillSlot.Y);
            Debug.Log("[VoiceCommand] Select Y Skill");
        }

        whisper.ClearText();
    }
}
