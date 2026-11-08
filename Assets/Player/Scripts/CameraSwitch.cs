using UnityEngine;
using StarterAssets;

public class CameraSwitch : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;
    public Transform fpAnchor;   // 1인칭 위치
    public Transform tpAnchor;   // 3인칭 위치

    [Header("Settings")]
    public KeyCode toggleKey = KeyCode.V;
    public float switchLerp = 12f;
    public float fovFP = 70f;
    public float fovTP = 65f;

    [Header("Obstruction (3rd)")]
    [Tooltip("비워두면 Default/Ground/Tile/Door 레이어를 자동 사용")]
    public LayerMask collisionMask;
    public float sphereRadius = 0.2f; // 카메라 충돌 반경
    public float extraBack = 0.1f;    // 여유 거리

    [Header("Feel")]
    [Tooltip("질주할 때 3인칭 FOV를 넓혀 속도감 연출 (도)")]
    public float dashFovBoost = 8f;
    [Tooltip("벽에 가려질 때는 빠르게, 다시 멀어질 때는 천천히")]
    public float obstructionPullInLerp = 25f;

    public bool isFirstPerson = true;

    private Vector3 tpLocalTarget; // 3인칭 목표 로컬 위치
    private Vector3 basePosition;  // 흔들림을 뺀 카메라 위치
    private ThirdPersonController controller;
    private float dashFov;

    private float shakeAmplitude;
    private float shakeDuration;
    private float shakeTime;
    private float shakeSeed;

    private void Reset()
    {
        cam = GetComponentInChildren<Camera>();
    }

    private void Start()
    {
        if (cam == null) cam = GetComponentInChildren<Camera>();
        if (cam != null)
        {
            cam.fieldOfView = isFirstPerson ? fovFP : fovTP;
            basePosition = cam.transform.localPosition;
        }
        if (tpAnchor != null) tpLocalTarget = tpAnchor.localPosition;

        // 씬에 충돌 레이어가 비어 있으면 카메라가 벽을 뚫고 들어가므로 기본값 사용
        if (collisionMask.value == 0)
            collisionMask = LayerMask.GetMask("Default", "Ground", "Tile", "Door");

        controller = GetComponentInParent<ThirdPersonController>();
        shakeSeed = Random.value * 100f;
    }

    /// <summary>화면 흔들림 (타격감 연출)</summary>
    public void Shake(float amplitude, float duration)
    {
        if (amplitude >= shakeAmplitude * (1f - shakeTime / Mathf.Max(shakeDuration, 0.0001f)))
        {
            shakeAmplitude = amplitude;
            shakeDuration = Mathf.Max(0.01f, duration);
            shakeTime = 0f;
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isFirstPerson = !isFirstPerson;
        }
    }

    private void LateUpdate()
    {
        if (cam == null || fpAnchor == null || tpAnchor == null) return;

        float dt = Time.deltaTime;

        if (isFirstPerson)
        {
            basePosition = Vector3.Lerp(basePosition, fpAnchor.localPosition, dt * switchLerp);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fovFP, dt * switchLerp);
        }
        else
        {
            tpLocalTarget = tpAnchor.localPosition;

            Vector3 worldHead = transform.TransformPoint(fpAnchor.localPosition);   // 머리 기준
            Vector3 worldTarget = transform.TransformPoint(tpAnchor.localPosition); // 원하는 카메라 위치

            Vector3 dir = (worldTarget - worldHead).normalized;
            float dist = Vector3.Distance(worldHead, worldTarget);
            bool obstructed = false;

            if (Physics.SphereCast(
                    worldHead,
                    sphereRadius,
                    dir,
                    out RaycastHit hit,
                    dist,
                    collisionMask,
                    QueryTriggerInteraction.Ignore))
            {
                float safeDist = Mathf.Max(0f, hit.distance - extraBack);
                Vector3 safeWorld = worldHead + dir * safeDist;
                tpLocalTarget = transform.InverseTransformPoint(safeWorld);
                obstructed = true;
            }

            // 벽에 막히면 즉시 당겨서 벽 뚫림 방지, 풀리면 부드럽게 복귀
            float posLerp = obstructed ? obstructionPullInLerp : switchLerp;
            basePosition = Vector3.Lerp(basePosition, tpLocalTarget, dt * posLerp);

            float targetDash = controller != null ? controller.DashAmount01 : 0f;
            dashFov = Mathf.Lerp(dashFov, targetDash * dashFovBoost, dt * 4f);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fovTP + dashFov, dt * switchLerp);
        }

        cam.transform.localPosition = basePosition + ShakeOffset(dt);
    }

    private Vector3 ShakeOffset(float dt)
    {
        if (shakeAmplitude <= 0f) return Vector3.zero;

        // 히트스톱 중에도 흔들리도록 실시간 사용
        shakeTime += Time.unscaledDeltaTime;
        float k = 1f - shakeTime / shakeDuration;
        if (k <= 0f)
        {
            shakeAmplitude = 0f;
            return Vector3.zero;
        }

        float t = Time.unscaledTime * 35f;
        Vector3 n = new Vector3(
            Mathf.PerlinNoise(shakeSeed, t) - 0.5f,
            Mathf.PerlinNoise(shakeSeed + 10f, t) - 0.5f,
            0f) * 2f;
        return n * (shakeAmplitude * k * k);
    }
}
