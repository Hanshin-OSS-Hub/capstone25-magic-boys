using UnityEngine;
using UnityEngine.Rendering;

// 플레이어 외형과 시점별 표시만 교체한다. 기존 리그·전투·음성 참조는 그대로 둔다.
public class BabelMageAppearance : MonoBehaviour
{
    public Renderer originalBody;
    public Renderer dressedBody;
    public GameObject clothing;
    public Renderer hammer;
    public Material[] originalHammerMaterials;
    public Material[] mageHammerMaterials;
    public MeshFilter hammerMeshFilter;
    public Mesh originalHammerMesh;
    public Mesh dressedHammerMesh;
    public bool dressed = true;
    public Renderer[] headRenderers;
    private CameraSwitch cameraSwitch;

    private void Awake()
    {
        cameraSwitch = GetComponentInChildren<CameraSwitch>(true);
        Show(dressed);
    }

    private void LateUpdate()
    {
        bool firstPerson = cameraSwitch != null && cameraSwitch.cam.enabled && cameraSwitch.isFirstPerson;
        foreach (Renderer renderer in headRenderers)
            renderer.shadowCastingMode = firstPerson ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
    }

    public void Show(bool value)
    {
        dressed = value;
        originalBody.enabled = !value;
        dressedBody.enabled = value;
        clothing.SetActive(value);
        hammer.sharedMaterials = value ? mageHammerMaterials : originalHammerMaterials;
        if (hammerMeshFilter != null)
            hammerMeshFilter.sharedMesh = value ? dressedHammerMesh : originalHammerMesh;
    }
}
