using UnityEngine;
using UnityEngine.UI;

public class MagicAttackk : MonoBehaviour
{
    [Header("Telekinesis")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private string movableTag = "Movable";
    [SerializeField] private float castRange = 8f;
    [SerializeField] private float holdDistance = 4f;
    [SerializeField] private float moveSpeed = 12f;
    [SerializeField] private float maxTelekinesisTime = 3f;
    [SerializeField] private float maxHoldHeightOffset = 1.0f;

    [Header("UI")]
    [SerializeField] private Image telekinesisFillImage;

    private Rigidbody currentTargetRb;
    private bool isTelekinesisActive = false;
    private float currentTelekinesisTime = 0f;

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
        // U키는 이제 홀드가 아니라 토글
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
            currentTelekinesisTime -= Time.deltaTime;

            if (telekinesisFillImage != null)
            {
                telekinesisFillImage.fillAmount = currentTelekinesisTime / maxTelekinesisTime;
            }

            if (currentTelekinesisTime <= 0f)
            {
                StopTelekinesis();
            }
        }
    }

    void FixedUpdate()
    {
        if (!isTelekinesisActive || currentTargetRb == null || playerCamera == null)
            return;

        Vector3 targetPosition = playerCamera.transform.position + playerCamera.transform.forward * holdDistance;

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
        // 이미 시전 중이면 새로 시작 안 함
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

            StartTelekinesis(targetRb);
        }
    }

    private void StartTelekinesis(Rigidbody targetRb)
    {
        currentTargetRb = targetRb;

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