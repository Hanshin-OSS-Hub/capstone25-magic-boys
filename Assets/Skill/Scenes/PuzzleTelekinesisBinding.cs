using Mediapipe.Unity.Sample.HandLandmarkDetection;
using UnityEngine;
using UnityEngine.UI;

// Bind before gestures are consumed, after the travelling player becomes active.
[DefaultExecutionOrder(-100)]
public sealed class PuzzleTelekinesisBinding : MonoBehaviour
{
    [SerializeField] private ThumbUpHandLandmarkerRunner runner;
    [SerializeField] private Image telekinesisFillImage;

    private MagicAttackk boundAttack;
    private Camera boundCamera;

    private void Awake()
    {
        if (runner == null) runner = GetComponent<ThumbUpHandLandmarkerRunner>();
        if (runner != null) runner.SetTelekinesisTarget(null);
    }

    private void Update()
    {
        PersistentPlayer player = PersistentPlayer.Instance;
        if (runner == null || player == null || !player.gameObject.activeInHierarchy)
        {
            Unbind();
            return;
        }

        // Never use a camera on the inactive, scene-local player or test player.
        Camera camera = null;
        foreach (Camera candidate in player.GetComponentsInChildren<Camera>())
        {
            if (!candidate.isActiveAndEnabled) continue;
            camera = candidate;
            if (candidate.CompareTag("MainCamera")) break;
        }
        if (camera == null)
        {
            Unbind();
            return;
        }

        MagicAttackk attack = player.GetComponent<MagicAttackk>();
        if (attack == null) attack = player.gameObject.AddComponent<MagicAttackk>();
        if (boundAttack == attack && boundCamera == camera && attack.isActiveAndEnabled)
            return;

        Unbind();
        boundAttack = attack;
        boundCamera = camera;
        attack.enabled = true;
        attack.BindSceneContext(camera, telekinesisFillImage);
        runner.SetTelekinesisTarget(attack);
        Debug.Log($"[PuzzleTelekinesis] Bound player={player.name}#{player.transform.GetInstanceID()} " +
            $"magic={attack.GetInstanceID()} camera={camera.name}#{camera.GetInstanceID()}", player);
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void Unbind()
    {
        if (runner != null) runner.SetTelekinesisTarget(null);
        if (boundAttack != null) boundAttack.ReleaseSceneContext(telekinesisFillImage);
        boundAttack = null;
        boundCamera = null;
    }
}
