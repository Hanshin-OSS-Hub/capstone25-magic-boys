
using UnityEngine;

public class MouseLook2 : MonoBehaviour
{
    [Header("Mouse Sensitivity")]
    public float mouseSensitivity = 100f;

    [Header("Object References")]
    public Transform playerBody;      // 플레이어의 몸체 (좌우 회전용)
    public Transform cameraTransform; // 카메라 (상하 회전용)

    private float xRotation = 0f; // 카메라의 상하 회전(끄덕임)

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 1. 마우스 입력 받기
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // 2. 카메라 상하 회전 (끄덕임)
        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f); // 90도 고정
        cameraTransform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // 3. 플레이어 몸체 좌우 회전
        playerBody.Rotate(Vector3.up * mouseX);
    }
}