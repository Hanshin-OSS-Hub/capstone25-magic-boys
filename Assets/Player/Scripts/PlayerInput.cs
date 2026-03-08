using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    // ======= Movement / Basic Input =======
    public Vector2 MoveInput { get; private set; }
    public Vector2 MouseInput { get; private set; }
    public bool IsRunPressed { get; private set; }
    public bool IsCrouchPressed { get; private set; }
    public bool IsJumpPressed { get; private set; }
    public bool IsAttackPressed { get; private set; }

    // ======= UI / Stats Panel Toggle =======
    public bool IsToggleStatsPressed { get; private set; }   // ★ 추가됨

    // ======= Voice-trigger queued state =======
    bool _skill1Queued, _skill2Queued;
    float _lastVoice1, _lastVoice2;
    public float voiceDebounce = 0.3f;

    // ======= External Trigger (from VoiceCommandSystem) =======
    public void QueueSkill1FromVoice()
    {
        if (Time.unscaledTime - _lastVoice1 < voiceDebounce) return;
        _skill1Queued = true;
        _lastVoice1 = Time.unscaledTime;
    }

    public void QueueSkill2FromVoice()
    {
        if (Time.unscaledTime - _lastVoice2 < voiceDebounce) return;
        _skill2Queued = true;
        _lastVoice2 = Time.unscaledTime;
    }

    // ======= Consumed by PlayerAttack =======
    public bool TryConsumeSkill1()
    {
        bool pressed = Input.GetKeyDown(KeyCode.Q) || _skill1Queued;
        _skill1Queued = false;
        return pressed;
    }

    public bool TryConsumeSkill2()
    {
        bool pressed = Input.GetKeyDown(KeyCode.E) || _skill2Queued;
        _skill2Queued = false;
        return pressed;
    }

    void Update()
    {
        // 이동/시점 입력
        MoveInput = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        MouseInput = new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));

        // 기본 조작
        IsJumpPressed = Input.GetKeyDown(KeyCode.Space);
        IsAttackPressed = Input.GetButtonDown("Fire1");
        IsRunPressed = Input.GetKey(KeyCode.LeftShift);
        IsCrouchPressed = Input.GetKey(KeyCode.LeftControl);

        // ★ 토글 입력 추가
        IsToggleStatsPressed = Input.GetKeyDown(KeyCode.K);
    }
}
