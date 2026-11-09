using Cinemachine;
using UnityEngine;

/// <summary>A continuous, bounds-relative shot using the existing Cinemachine targets.</summary>
public sealed class TowerEndingCameraPath : MonoBehaviour
{
    [SerializeField] private Transform tower;
    [SerializeField] private CinemachineVirtualCamera virtualCamera;
    [SerializeField, Min(1f)] private float duration = 16f;
    [SerializeField, Min(0f)] private float clearanceInTowerHeights = 0.05f;

    [Header("Curves: normalized shot time (0 to 1)")]
    [SerializeField] private AnimationCurve azimuth = SmoothCurve(180f, 195f, 240f, 295f, 310f);
    [SerializeField] private AnimationCurve radius = SmoothCurve(0.78f, 0.88f, 0.96f, 0.82f, 0.76f);
    [SerializeField] private AnimationCurve height = SmoothCurve(0.035f, 0.10f, 0.34f, 1.02f, 1.10f);
    [SerializeField] private AnimationCurve lookHeight = SmoothCurve(0.50f, 0.51f, 0.60f, 0.88f, 0.90f);
    [SerializeField] private AnimationCurve fieldOfView = SmoothCurve(54f, 52f, 48f, 44f, 42f);

    private Bounds towerBounds;
    private float safeRadius;
    private bool initialized;

    public float Duration => duration;
    public Bounds TowerBounds => towerBounds;

    public bool Begin()
    {
        initialized = false;
        if (tower == null || virtualCamera == null || virtualCamera.Follow == null || virtualCamera.LookAt == null)
        {
            Debug.LogError("[Ending Camera] Assign the tower, virtual camera, Follow and LookAt targets.", this);
            return false;
        }

        bool found = false;
        foreach (Renderer renderer in tower.GetComponentsInChildren<Renderer>())
        {
            if (!renderer.enabled) continue;
            if (!found) { towerBounds = renderer.bounds; found = true; }
            else towerBounds.Encapsulate(renderer.bounds);
        }
        if (!found || towerBounds.size.y <= 0.001f)
        {
            Debug.LogError("[Ending Camera] The tower has no visible renderer bounds.", this);
            return false;
        }

        // The whole mesh is inside this cylinder. Staying outside it prevents
        // intersections even at the broad base and during the rising orbit.
        Vector3 extents = towerBounds.extents;
        safeRadius = new Vector2(extents.x, extents.z).magnitude
            + towerBounds.size.y * clearanceInTowerHeights;
        initialized = true;
        EvaluateTime(0f);
        virtualCamera.PreviousStateIsValid = false;
        return true;
    }

    public void EvaluateTime(float seconds)
    {
        if (!initialized) return;
        float t = Mathf.Clamp01(seconds / Mathf.Max(1f, duration));
        float towerHeight = towerBounds.size.y;
        Vector3 baseCenter = new Vector3(towerBounds.center.x, towerBounds.min.y, towerBounds.center.z);
        float angle = azimuth.Evaluate(t) * Mathf.Deg2Rad;
        float distance = Mathf.Max(safeRadius, radius.Evaluate(t) * towerHeight);

        // Follow is a descendant of LookAt in the original scene: move LookAt
        // first, then assign the final Follow world position to avoid offsets.
        virtualCamera.LookAt.position = baseCenter + Vector3.up * (lookHeight.Evaluate(t) * towerHeight);
        virtualCamera.Follow.position = baseCenter + new Vector3(
            Mathf.Sin(angle) * distance, height.Evaluate(t) * towerHeight, Mathf.Cos(angle) * distance);
        virtualCamera.m_Lens.FieldOfView = Mathf.Clamp(fieldOfView.Evaluate(t), 25f, 75f);
    }

    // Shape-preserving cubic tangents keep velocity continuous through each
    // phase without overshooting the height/radius keys. Only the ends stop.
    private static AnimationCurve SmoothCurve(params float[] values)
    {
        float[] times = { 0f, 3f / 16f, 7f / 16f, 13.5f / 16f, 1f };
        var keys = new Keyframe[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            float tangent = 0f;
            if (i > 0 && i < values.Length - 1)
            {
                float previousSpan = times[i] - times[i - 1];
                float nextSpan = times[i + 1] - times[i];
                float previousSlope = (values[i] - values[i - 1]) / previousSpan;
                float nextSlope = (values[i + 1] - values[i]) / nextSpan;
                if (previousSlope * nextSlope > 0f)
                {
                    float w1 = 2f * nextSpan + previousSpan;
                    float w2 = nextSpan + 2f * previousSpan;
                    tangent = (w1 + w2) / (w1 / previousSlope + w2 / nextSlope);
                }
            }
            keys[i] = new Keyframe(times[i], values[i], tangent, tangent);
        }
        return new AnimationCurve(keys);
    }
}
