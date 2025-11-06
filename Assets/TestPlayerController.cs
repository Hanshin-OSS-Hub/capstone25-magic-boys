using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class TestPlayerController : MonoBehaviour
{
    [Header("Refs")]
    public Transform cam;                 // Main Camera (Player 자식)

    [Header("Move")]
    public float moveSpeed = 5f;
    public float gravity = -9.81f;

    [Header("Mouse Look")]
    public float mouseSensitivity = 200f; // "도/초" 느낌
    public float pitchMin = -45f;
    public float pitchMax = 45f;

    float yaw;
    float pitch;
    Vector3 velocity;
    CharacterController cc;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!cam && Camera.main) cam = Camera.main.transform;

        yaw = transform.eulerAngles.y;
        pitch = cam ? cam.localEulerAngles.x : 0f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // --- Mouse look  ---
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        yaw += mouseX;
        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);

        transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        if (cam) cam.localRotation = Quaternion.Euler(pitch, 0f, 0f);

        // --- Move (카메라 기준 WASD) ---
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 f = cam ? cam.forward : transform.forward; f.y = 0f; f.Normalize();
        Vector3 r = cam ? cam.right : transform.right; r.y = 0f; r.Normalize();

        Vector3 inputDir = (f * vertical + r * horizontal).normalized;
        Vector3 move = inputDir * moveSpeed;

        // --- Gravity ---
        if (cc.isGrounded && velocity.y < 0f) velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;

        cc.Move((move + velocity) * Time.deltaTime);

        // ESC로 커서 토글(테스트 편의)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool locked = Cursor.lockState != CursorLockMode.Locked;
            Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !locked;
        }
    }
}

