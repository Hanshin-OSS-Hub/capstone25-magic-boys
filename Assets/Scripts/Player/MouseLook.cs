using UnityEngine;

public class MouseLook : MonoBehaviour
{
    [Header("Mouse Sensitivity")]
    public float mouseSensitivity = 100f;

    [Header("Object References")]
    public Transform playerBody; // 카메라가 공전할 '중심' (플레이어)

    [Header("Camera Settings")]
    public float distance = 5.0f;     // 플레이어와의 거리
    public float minYAngle = -20f;  // 카메라 최소 Y 각도 (아래)
    public float maxYAngle = 80f;   // 카메라 최대 Y 각도 (위)

    private float currentX = 0.0f; // 마우스 X 누적값
    private float currentY = 0.0f; // 마우스 Y 누적값

    void Start()
    {
        // 마우스 커서 고정
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // 1. 마우스 입력 받기
        currentX += Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        currentY -= Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        // 2. 카메라 상하 각도 제한
        currentY = Mathf.Clamp(currentY, minYAngle, maxYAngle);
    }

    // LateUpdate: 모든 Update()가 끝난 후 실행 (카메라의 떨림 방지)
    void LateUpdate()
    {
        // 1. 카메라의 회전값 계산
        Quaternion rotation = Quaternion.Euler(currentY, currentX, 0);

        // 2. 플레이어의 위치(playerBody.position)에서
        //    계산된 회전(rotation)으로,
        //    정해진 거리(distance)만큼 떨어진 위치 계산
        Vector3 targetPosition = playerBody.position + rotation * new Vector3(0, 0, -distance);

        // 3. 카메라 위치와 회전 적용
        transform.position = targetPosition;
        transform.LookAt(playerBody.position); // 항상 플레이어를 바라보게 함
    }
}