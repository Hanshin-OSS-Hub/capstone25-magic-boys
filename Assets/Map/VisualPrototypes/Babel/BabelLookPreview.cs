using UnityEngine;

// 별도 시안 씬의 외형 비교 및 정면 보기. 게임 씬에는 추가하지 않는다.
public class BabelLookPreview : MonoBehaviour
{
    public BabelMageAppearance appearance;
    public CameraSwitch cameraSwitch;
    public Camera portraitCamera;
    private StarterAssets.ThirdPersonController movement;
    private bool inspecting;
    private bool movementWasEnabled;

    private void Start()
    {
        movement = appearance.GetComponent<StarterAssets.ThirdPersonController>();
        portraitCamera.enabled = false;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F8)) appearance.Show(!appearance.dressed);
        if (Input.GetKeyDown(KeyCode.F9)) Inspect(!inspecting);
    }

    public void Inspect(bool value)
    {
        if (inspecting == value) return;
        inspecting = value;
        if (value)
        {
            movementWasEnabled = movement.enabled;
            movement.enabled = false;
        }
        else movement.enabled = movementWasEnabled;
        cameraSwitch.enabled = !value;
        cameraSwitch.cam.enabled = !value;
        portraitCamera.enabled = value;
        UpdatePortrait();
    }

    private void LateUpdate()
    {
        if (inspecting) UpdatePortrait();
    }

    private void UpdatePortrait()
    {
        Transform player = appearance.transform;
        portraitCamera.transform.position = player.position + player.forward * 2.8f + player.right * 0.7f + Vector3.up * 1.35f;
        portraitCamera.transform.LookAt(player.position + Vector3.up * 1.02f);
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(16, 82, 440, 30), "F8: Original / Outfit     F9: Front inspection / Play");
    }
}
