using UnityEngine;

public class VoiceCommandAdapter : MonoBehaviour
{
    public WhisperPure whisper;
    public PlayerInput input;

    float cooldown = 0.2f;
    float timer;

    void Update()
    {
     

        if (!whisper || !input) return;

        if (timer > 0f)
            timer -= Time.deltaTime;

        string text = whisper.GetText();
        if (string.IsNullOrEmpty(text))
            return;

        text = text.ToLower().Trim();
        Debug.Log("Adapter sees text: " + text);


        if (timer <= 0f)
        {
            if (text.Contains("fire"))
            {
                TriggerSkill1();
                whisper.ClearText();   
                timer = cooldown;
            }
            else if (text.Contains("spark"))
            {
                TriggerSkill2();
                whisper.ClearText();   
                timer = cooldown;
            }
        }
    }

    void TriggerSkill1()
    {
        typeof(PlayerInput)
            .GetProperty("IsSkill1Pressed")
            ?.SetValue(input, true);
    }

    void TriggerSkill2()
    {
        typeof(PlayerInput)
            .GetProperty("IsSkill2Pressed")
            ?.SetValue(input, true);
    }
}
