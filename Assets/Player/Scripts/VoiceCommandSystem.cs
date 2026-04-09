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

        // Y - Reserved (Skill 5)
        else if (text.Contains("ultimate")) { matched = true; displayWord = "ultimate"; magicAttack.SelectSkill(MagicAttack.SkillSlot.Y); }
        else if (text.Contains("skill y")) { matched = true; displayWord = "skill y"; magicAttack.SelectSkill(MagicAttack.SkillSlot.Y); }

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
