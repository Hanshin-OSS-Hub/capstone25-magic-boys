using UnityEngine;

public class CameraSwitch : MonoBehaviour
{
    [Header("Refs")]
    public Camera cam;
    public Transform fpAnchor;   // 1인칭 위치
    public Transform tpAnchor;   // 3인칭 위치 (어깨 뒤)

    [Header("Settings")]
    public KeyCode toggleKey = KeyCode.V;
    public float switchLerp = 12f; // 전환 부드러움
    public float fovFP = 70f;
    public float fovTP = 65f;

    [Header("Obstruction (3rd)")]
    public LayerMask collisionMask;   // 보통 Default, Environment 등
    public float sphereRadius = 0.2f; // 카메라 충돌 반경
    public float extraBack = 0.1f;    // 여유 거리

    public bool isFirstPerson = true;

    private Vector3 tpLocalTarget; // 3인칭 목표 로컬 위치(충돌 보정 후)

    private void Reset()
    {
        cam = GetComponentInChildren<Camera>();
    }

    private void Start()
    {
        if (cam == null) cam = GetComponentInChildren<Camera>();
        if (cam != null) cam.fieldOfView = isFirstPerson ? fovFP : fovTP;
        if (tpAnchor != null) tpLocalTarget = tpAnchor.localPosition;
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

        if (isFirstPerson)
        {
            cam.transform.localPosition = Vector3.Lerp(
                cam.transform.localPosition,
                fpAnchor.localPosition,
                Time.deltaTime * switchLerp
            );

            cam.fieldOfView = Mathf.Lerp(
                cam.fieldOfView,
                fovFP,
                Time.deltaTime * switchLerp
            );
        }
        else
        {
            tpLocalTarget = tpAnchor.localPosition;

            Vector3 worldHead = transform.TransformPoint(fpAnchor.localPosition);   // 머리 기준
            Vector3 worldTarget = transform.TransformPoint(tpAnchor.localPosition); // 원하는 카메라 위치

            Vector3 dir = (worldTarget - worldHead).normalized;
            float dist = Vector3.Distance(worldHead, worldTarget);

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
            }

            cam.transform.localPosition = Vector3.Lerp(
                cam.transform.localPosition,
                tpLocalTarget,
                Time.deltaTime * switchLerp
            );

            cam.fieldOfView = Mathf.Lerp(
                cam.fieldOfView,
                fovTP,
                Time.deltaTime * switchLerp
            );
        }
    }
}
