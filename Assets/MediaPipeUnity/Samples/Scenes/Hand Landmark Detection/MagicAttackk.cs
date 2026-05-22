using UnityEngine;
using UnityEngine.UI;

public class MagicAttackk : MonoBehaviour
{
    [Header("Telekinesis")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private string movableTag = "Movable";
    [SerializeField] private float castRange = 20f;
    [SerializeField] private float minHoldDistance = 2f;
    [SerializeField] private float maxHoldDistance = 15f;
    [SerializeField] private float scrollSensitivity = 3f;
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private float maxTelekinesisTime = 10f;
    [SerializeField] private float maxHoldHeightOffset = 1.0f;

    [Header("UI")]
    [SerializeField] private Image telekinesisFillImage;

    private Rigidbody currentTargetRb;
    private bool isTelekinesisActive = false;
    private float currentTelekinesisTime = 0f;

    // 현재 물체를 잡고 있는 거리
    private float currentHoldDistance = 4f;

    void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (telekinesisFillImage != null)
        {
            telekinesisFillImage.fillAmount = 0f;
            telekinesisFillImage.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // U키 토글
        if (Input.GetKeyDown(KeyCode.U))
        {
            if (isTelekinesisActive)
            {
                StopTelekinesis();
            }
            else
            {
                Telekinesis();
            }
        }

        if (isTelekinesisActive)
        {
            // 시전 시간 감소
            currentTelekinesisTime -= Time.deltaTime;

            if (telekinesisFillImage != null)
            {
                telekinesisFillImage.fillAmount = currentTelekinesisTime / maxTelekinesisTime;
            }

            if (currentTelekinesisTime <= 0f)
            {
                StopTelekinesis();
                return;
            }

            // 마우스 휠로 거리 조절
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scroll) > 0.0001f)
            {
                currentHoldDistance += scroll * scrollSensitivity;
                currentHoldDistance = Mathf.Clamp(currentHoldDistance, minHoldDistance, maxHoldDistance);
            }
        }
    }

    void FixedUpdate()
    {
        if (!isTelekinesisActive || currentTargetRb == null || playerCamera == null)
            return;

        Vector3 targetPosition = playerCamera.transform.position + playerCamera.transform.forward * currentHoldDistance;

        // 너무 위로 뜨는 것 방지
        float maxY = playerCamera.transform.position.y + maxHoldHeightOffset;
        if (targetPosition.y > maxY)
        {
            targetPosition.y = maxY;
        }

        Vector3 direction = targetPosition - currentTargetRb.position;
        currentTargetRb.linearVelocity = direction * moveSpeed;
        currentTargetRb.angularVelocity = Vector3.zero;
    }

    public void Telekinesis()
    {
        if (isTelekinesisActive)
            return;

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (playerCamera == null)
            return;

        Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (Physics.Raycast(ray, out RaycastHit hit, castRange))
        {
            if (!hit.collider.CompareTag(movableTag))
                return;

            Rigidbody targetRb = hit.collider.GetComponentInParent<Rigidbody>();
            if (targetRb == null)
                return;

            StartTelekinesis(targetRb, hit.distance);
        }
    }

    private void StartTelekinesis(Rigidbody targetRb, float hitDistance)
    {
        currentTargetRb = targetRb;

        // 시작할 때는 "맞은 거리"를 그대로 유지해서 바로 앞으로 안 끌려오게 함
        currentHoldDistance = Mathf.Clamp(hitDistance, minHoldDistance, maxHoldDistance);

        currentTargetRb.useGravity = false;
        currentTargetRb.linearVelocity = Vector3.zero;
        currentTargetRb.angularVelocity = Vector3.zero;

        isTelekinesisActive = true;
        currentTelekinesisTime = maxTelekinesisTime;

        if (telekinesisFillImage != null)
        {
            telekinesisFillImage.gameObject.SetActive(true);
            telekinesisFillImage.fillAmount = 1f;
        }

        Debug.Log("Telekinesis Start");
    }

    private void StopTelekinesis()
    {
        if (currentTargetRb != null)
        {
            currentTargetRb.useGravity = true;
            currentTargetRb.linearVelocity = Vector3.zero;
            currentTargetRb.angularVelocity = Vector3.zero;
            currentTargetRb.WakeUp();
        }

        currentTargetRb = null;
        isTelekinesisActive = false;
        currentTelekinesisTime = 0f;

        if (telekinesisFillImage != null)
        {
            telekinesisFillImage.fillAmount = 0f;
            telekinesisFillImage.gameObject.SetActive(false);
        }

        Debug.Log("Telekinesis End");
    }
}