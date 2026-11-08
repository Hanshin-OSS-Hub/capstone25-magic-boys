using UnityEngine;
using StarterAssets;

// Tab: 화면 중앙에 가장 가까운 적/보스를 락온, 다시 누르면 해제.
// 대상이 죽거나(비활성) 너무 멀어지면 자동 해제. 대상 위에 표시를 그린다.
public class LockOnTarget : MonoBehaviour
{
    public string bossTag = "Boss";
    public KeyCode toggleKey = KeyCode.Tab;

    [Header("Targeting")]
    [Tooltip("락온 가능한 최대 거리")]
    public float lockOnRange = 25f;
    [Tooltip("이 거리보다 멀어지면 자동 해제")]
    public float breakRange = 35f;
    [Tooltip("락온 대상으로 찾을 레이어")]
    public LayerMask targetLayers;
    [Tooltip("카메라 정면 기준 이 각도 안의 대상만 선택")]
    public float maxScreenAngle = 70f;

    [Header("Marker")]
    public bool showMarker = true;
    public Color markerColor = new Color(1f, 0.35f, 0.25f, 0.95f);
    public float markerSize = 26f;

    private ThirdPersonController controller;
    private Transform bossTarget;
    private Collider targetCollider;
    private bool isLockedOn = false;
    private Camera cam;
    private Texture2D markerTexture;

    public bool IsLockedOn => isLockedOn;
    public Transform CurrentTarget => bossTarget;

    void Start()
    {
        controller = GetComponent<ThirdPersonController>();
        if (controller == null)
        {
            Debug.LogError("LockOnTarget: ThirdPersonController를 찾을 수 없습니다.");
        }

        if (targetLayers.value == 0)
            targetLayers = LayerMask.GetMask("Enemy", "Boss");
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            if (!isLockedOn)
            {
                TryLockOn();
            }
            else
            {
                Unlock();
            }
        }

        if (isLockedOn)
        {
            if (bossTarget == null || !bossTarget.gameObject.activeInHierarchy ||
                Vector3.Distance(transform.position, bossTarget.position) > breakRange)
            {
                Unlock();
                return;
            }

            controller.LockOnPoint = GetAimPoint();
        }
    }

    void TryLockOn()
    {
        cam = Camera.main;
        Transform best = FindBestTarget();

        if (best == null)
        {
            Debug.Log("LockOnTarget: 락온할 대상을 찾지 못했습니다.");
            return;
        }

        bossTarget = best;
        targetCollider = best.GetComponentInChildren<Collider>();
        isLockedOn = true;

        controller.IsLockOn = true;
        controller.LockOnTarget = bossTarget;
        controller.LockOnPoint = GetAimPoint();

        controller.LockCameraPosition = true;
    }

    Transform FindBestTarget()
    {
        Vector3 origin = transform.position;
        Vector3 camForward = cam != null ? cam.transform.forward : transform.forward;
        camForward.y = 0f;
        camForward.Normalize();

        Transform best = null;
        float bestScore = float.MaxValue;

        void Consider(Transform t)
        {
            if (t == null || t == transform || !t.gameObject.activeInHierarchy) return;
            Vector3 to = t.position - origin;
            float dist = to.magnitude;
            if (dist > lockOnRange) return;
            to.y = 0f;
            float angle = to.sqrMagnitude > 0.01f ? Vector3.Angle(camForward, to) : 0f;
            if (angle > maxScreenAngle) return;
            // 화면 중앙에 가까울수록, 가까울수록 우선
            float score = angle * 1.0f + dist * 1.5f;
            if (score < bestScore)
            {
                bestScore = score;
                best = t;
            }
        }

        // 보스 태그 (기존 동작 유지)
        foreach (var boss in GameObject.FindGameObjectsWithTag(bossTag))
            Consider(boss.transform);

        // 주변 적 (IDamageable 을 가진 루트)
        var hits = Physics.OverlapSphere(origin, lockOnRange, targetLayers, QueryTriggerInteraction.Collide);
        foreach (var h in hits)
        {
            var dmg = h.GetComponentInParent<IDamageable>() as MonoBehaviour;
            Consider(dmg != null ? dmg.transform : h.transform.root);
        }

        return best;
    }

    Vector3 GetAimPoint()
    {
        if (targetCollider != null && targetCollider.enabled)
        {
            Bounds b = targetCollider.bounds;
            // 너무 큰 보스는 가슴 높이 정도를 바라봄
            return new Vector3(b.center.x, Mathf.Min(b.center.y, b.min.y + 2.2f), b.center.z);
        }
        return bossTarget.position + Vector3.up * 1.2f;
    }

    void Unlock()
    {
        isLockedOn = false;
        bossTarget = null;
        targetCollider = null;

        controller.IsLockOn = false;
        controller.LockOnTarget = null;
        controller.LockOnPoint = null;
        controller.LockCameraPosition = false;
    }

    void OnGUI()
    {
        if (!showMarker || !isLockedOn || bossTarget == null) return;
        if (cam == null) cam = Camera.main;
        if (cam == null) return;

        Vector3 sp = cam.WorldToScreenPoint(GetAimPoint());
        if (sp.z <= 0f) return;

        if (markerTexture == null) markerTexture = CreateRingTexture(64);

        float pulse = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.08f;
        float size = markerSize * pulse;
        Rect r = new Rect(sp.x - size * 0.5f, Screen.height - sp.y - size * 0.5f, size, size);

        Color prev = GUI.color;
        GUI.color = markerColor;
        GUI.DrawTexture(r, markerTexture);
        GUI.color = prev;
    }

    static Texture2D CreateRingTexture(int size)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        float c = (size - 1) * 0.5f;
        float outer = c, inner = c * 0.68f, dot = c * 0.16f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(c, c));
                float a = 0f;
                if (d <= outer && d >= inner) a = Mathf.Clamp01(Mathf.Min(outer - d, d - inner));
                if (d <= dot) a = Mathf.Clamp01(dot - d);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
            }
        }
        tex.Apply();
        return tex;
    }

    void OnDestroy()
    {
        if (markerTexture != null) Destroy(markerTexture);
    }
}
