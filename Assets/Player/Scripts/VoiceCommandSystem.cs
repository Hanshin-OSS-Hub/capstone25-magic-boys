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
            magicAttack.TryCastQ();
            Debug.Log("[VoiceCommand] Cast Fireball (Q)");
        }

        // E - Water Field
        if (text.Contains("water") || text.Contains("ice"))
        {
            magicAttack.TryCastE();
            Debug.Log("[VoiceCommand] Cast Water Field (E)");
        }

        // R - Earth Wall
        if (text.Contains("earth") || text.Contains("wall") || text.Contains("rock"))
        {
            magicAttack.TryCastR();
            Debug.Log("[VoiceCommand] Cast Earth Wall (R)");
        }

        // T - Thunder Rain
        if (text.Contains("thunder") || text.Contains("lightning") || text.Contains("storm"))
        {
            magicAttack.TryCastT();
            Debug.Log("[VoiceCommand] Cast Thunder Rain (T)");
        }

        // Y - Reserved (Skill 5)
        if (text.Contains("ultimate") || text.Contains("skill y"))
        {
            magicAttack.TryCastY();
            Debug.Log("[VoiceCommand] Cast Y Skill");
        }

        whisper.ClearText();
    }
}
