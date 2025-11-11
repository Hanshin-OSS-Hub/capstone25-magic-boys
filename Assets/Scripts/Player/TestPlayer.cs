using UnityEngine;

public class TestPlayer : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float rotationSpeed = 10f; // 플레이어가 회전하는 속도


    public Transform cameraTransform;

    void Update()
    {
        // 1. 키보드 입력 받기
        float horizontal = Input.GetAxis("Horizontal"); // A, D
        float vertical = Input.GetAxis("Vertical");     // W, S

        Vector3 inputDirection = new Vector3(horizontal, 0, vertical);

        // 2. 만약 입력이 있다면 (키를 눌렀다면)
        if (inputDirection.magnitude >= 0.1f)
        {
            // 3. 카메라가 바라보는 방향을 기준으로 이동 방향 계산
            float targetAngle = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + cameraTransform.eulerAngles.y;
            Vector3 moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

            // 4. 플레이어 이동
            transform.Translate(moveDirection.normalized * moveSpeed * Time.deltaTime, Space.World);

            // 5. 플레이어 모델이 이동 방향을 바라보도록 부드럽게 회전
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection.normalized);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
        }
    }
}