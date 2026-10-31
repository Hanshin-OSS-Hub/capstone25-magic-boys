using UnityEngine;

// 이 비교 씬에서만 사용하는 조작 도구. 실제 던전 생성 목록은 변경하지 않는다.
public class BabelRoomPreview : MonoBehaviour
{
    public GameObject originalRoom;
    public GameObject babelRoom;
    public Transform player;
    private bool showBabel = true;

    private void Start()
    {
        Show(true);
        ResetView();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F6)) Show(!showBabel);
        if (Input.GetKeyDown(KeyCode.F7)) ResetView();
    }

    private void Show(bool babel)
    {
        showBabel = babel;
        originalRoom.SetActive(!babel);
        babelRoom.SetActive(babel);
        Physics.SyncTransforms();
    }

    private void ResetView()
    {
        var controller = player.GetComponent<CharacterController>();
        if (controller != null) controller.enabled = false;
        player.SetPositionAndRotation(new Vector3(0, 0.12f, -5.5f), Quaternion.identity);
        player.GetComponent<StarterAssets.ThirdPersonController>()?.ResetAfterTeleport();
        Physics.SyncTransforms();
        if (controller != null) controller.enabled = true;
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(16, 16, 440, 62), "");
        GUI.Label(new Rect(28, 23, 420, 23), showBabel ? "BABEL / AFTER - layered stone sanctuary" : "ORIGINAL / BEFORE - Room I");
        GUI.Label(new Rect(28, 45, 420, 23), "F6: Before/After   F7: Reset position   V: 1st/3rd person");
    }
}
