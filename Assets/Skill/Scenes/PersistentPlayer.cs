using UnityEngine;

public class PersistentPlayer : MonoBehaviour
{
    public static PersistentPlayer Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.Log("[PersistentPlayer] 중복 Player 제거");
            Destroy(gameObject);
            return;
        }

        Instance = this;

        transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    public static void HideCurrent()
    {
        if (Instance == null) return;

        Instance.gameObject.SetActive(false);
    }

    public static void ShowCurrent()
    {
        if (Instance == null) return;

        Instance.gameObject.SetActive(true);
    }

    public static void DestroyCurrent()
    {
        if (Instance == null) return;

        GameObject target = Instance.gameObject;
        Instance = null;

        Destroy(target);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}