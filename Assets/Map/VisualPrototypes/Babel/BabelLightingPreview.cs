using UnityEngine;
using UnityEngine.Rendering;

// 시안 씬에서만 밝은 조명과 어두운 조명을 비교한다.
public class BabelLightingPreview : MonoBehaviour
{
    public Light[] lights;
    public float[] brightIntensities;
    public float[] darkIntensities;
    public bool dark = true;

    private void Start() => Show(dark);
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F10)) Show(!dark);
    }

    public void Show(bool value)
    {
        dark = value;
        for (int i = 0; i < lights.Length; i++)
            if (lights[i] != null) lights[i].intensity = value ? darkIntensities[i] : brightIntensities[i];
        RenderSettings.ambientSkyColor = value ? new Color(.17f,.21f,.27f) : new Color(.36f,.4f,.46f);
        RenderSettings.ambientEquatorColor = value ? new Color(.105f,.135f,.18f) : new Color(.26f,.3f,.35f);
        RenderSettings.ambientGroundColor = value ? new Color(.065f,.08f,.105f) : new Color(.17f,.19f,.22f);
        var probe = new SphericalHarmonicsL2();
        probe.AddAmbientLight(value ? new Color(.17f,.21f,.27f) : new Color(.38f,.42f,.47f));
        RenderSettings.ambientProbe = probe;
    }

    private void OnGUI()
    {
        GUI.Box(new Rect(16, 116, 440, 28), dark ? "F10: Dark ruins / Bright comparison" : "F10: Bright comparison / Dark ruins");
    }
}
