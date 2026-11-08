using UnityEngine;

// VfxKit 코루틴/화면 섬광 실행용 (자동 생성, 직접 붙일 필요 없음)
public class VfxRunner : MonoBehaviour
{
    private static VfxRunner instance;
    public static VfxRunner Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("[VfxKit]");
                go.hideFlags = HideFlags.HideInHierarchy;
                DontDestroyOnLoad(go);
                instance = go.AddComponent<VfxRunner>();
            }
            return instance;
        }
    }

    void OnGUI() { VfxKit.DrawFlash(); }
}
