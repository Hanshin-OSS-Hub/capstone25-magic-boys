using UnityEngine;

public class VoiceCommandSystem : MonoBehaviour
{
    public WhisperPure whisper;
    public PlayerInput input;

    void Update()
    {
        if (!whisper || !input) return;

        string text = whisper.GetText();
        if (string.IsNullOrEmpty(text)) return;

        text = text.ToLower().Trim();

        if (text.Contains("fire"))
            input.QueueSkill1FromVoice();

        if (text.Contains("spark"))
            input.QueueSkill2FromVoice();

        whisper.ClearText();
    }
}
