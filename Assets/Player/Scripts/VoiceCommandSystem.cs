using UnityEngine;
using TMPro;
using System.Collections;

public class VoiceCommandSystem : MonoBehaviour
{
    public WhisperPure whisper;
    public PlayerInput input;
    public MagicAttack magicAttack;
    public TMP_Text voiceCommandText;

    private Coroutine clearTextCoroutine;

    public void KeepWithPlayer(Transform player)
    {
        magicAttack = player.GetComponent<MagicAttack>();
        input = player.GetComponent<PlayerInput>();
        transform.SetParent(player, true);
        if (whisper != null) whisper.transform.SetParent(player, true);
        Debug.Log($"[VoiceFlow] 플레이어 연결 player={player.GetInstanceID()} " +
            $"magic={(magicAttack != null ? magicAttack.GetInstanceID() : 0)}");
    }

    void Awake()
    {
        if (!magicAttack) magicAttack = GetComponent<MagicAttack>();
        if (!input) input = GetComponent<PlayerInput>();
    }

    void Update()
    {
        if (!whisper) return;

        string text = whisper.GetText();
        if (string.IsNullOrEmpty(text)) return;

        if (!magicAttack)
        {
            Debug.LogError("[VoiceFlow] 인식 결과는 받았지만 마법 대상이 없습니다: " + text);
            whisper.ClearText();
            return;
        }

        text = text.ToLower().Trim();
        bool matched = false;
        string displayWord = "";

        // Q - Fireball
        if (text.Contains("fireball")) { matched = true; displayWord = "fireball"; magicAttack.SelectSkill(MagicAttack.SkillSlot.Q); }
        else if (text.Contains("fire")) { matched = true; displayWord = "fire"; magicAttack.SelectSkill(MagicAttack.SkillSlot.Q); }

        // E - Water Field
        else if (text.Contains("water")) { matched = true; displayWord = "water"; magicAttack.SelectSkill(MagicAttack.SkillSlot.E); }
        else if (text.Contains("ice")) { matched = true; displayWord = "ice"; magicAttack.SelectSkill(MagicAttack.SkillSlot.E); }

        // R - Earth Wall
        else if (text.Contains("earth")) { matched = true; displayWord = "earth"; magicAttack.SelectSkill(MagicAttack.SkillSlot.R); }
        else if (text.Contains("wall")) { matched = true; displayWord = "wall"; magicAttack.SelectSkill(MagicAttack.SkillSlot.R); }
        else if (text.Contains("rock")) { matched = true; displayWord = "rock"; magicAttack.SelectSkill(MagicAttack.SkillSlot.R); }

        // T - Thunder Rain
        else if (text.Contains("thunder")) { matched = true; displayWord = "thunder"; magicAttack.SelectSkill(MagicAttack.SkillSlot.T); }
        else if (text.Contains("lightning")) { matched = true; displayWord = "lightning"; magicAttack.SelectSkill(MagicAttack.SkillSlot.T); }
        else if (text.Contains("storm")) { matched = true; displayWord = "storm"; magicAttack.SelectSkill(MagicAttack.SkillSlot.T); }

        // Y - Meteor (ultimate)
        else if (text.Contains("meteor") || text.Contains("meteo")) { matched = true; displayWord = "meteor"; magicAttack.SelectSkill(MagicAttack.SkillSlot.Y); }
        else if (text.Contains("anvil")) { matched = true; displayWord = "anvil"; magicAttack.SelectSkill(MagicAttack.SkillSlot.Y); }
        else if (text.Contains("ultimate")) { matched = true; displayWord = "ultimate"; magicAttack.SelectSkill(MagicAttack.SkillSlot.Y); }
        else if (text.Contains("skill y")) { matched = true; displayWord = "skill y"; magicAttack.SelectSkill(MagicAttack.SkillSlot.Y); }

        Debug.Log($"[VoiceFlow] scene={UnityEngine.SceneManagement.SceneManager.GetActiveScene().name} " +
            $"player={magicAttack.gameObject.GetInstanceID()} text={text} 명령전달={matched}");

        if (voiceCommandText != null)
        {
            if (matched)
            {
                voiceCommandText.text = displayWord;
                voiceCommandText.color = Color.white;
            }
            else
            {
                voiceCommandText.text = "Error: " + text;
                voiceCommandText.color = Color.red;
            }

            if (clearTextCoroutine != null) StopCoroutine(clearTextCoroutine);
            clearTextCoroutine = StartCoroutine(ClearTextAfterDelay(2f));
        }

        whisper.ClearText();
    }

    IEnumerator ClearTextAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (voiceCommandText != null) voiceCommandText.text = "";
    }
}
