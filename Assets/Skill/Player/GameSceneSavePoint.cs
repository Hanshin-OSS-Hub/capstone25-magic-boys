using UnityEngine;
using UnityEngine.SceneManagement;

public class GameSceneSavePoint : MonoBehaviour
{
    public const string LastSceneKey = "LAST_GAME_SCENE";

    public bool saveOnStart = true;

    void Start()
    {
        if (saveOnStart)
            SaveCurrentScene();
    }

    public void SaveCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;

        PlayerPrefs.SetString(LastSceneKey, sceneName);
        PlayerPrefs.Save();

        Debug.Log("[GameSceneSavePoint] Saved scene: " + sceneName);
    }

    public static bool HasSavedScene()
    {
        return PlayerPrefs.HasKey(LastSceneKey);
    }

    public static void ResetSavedScene()
    {
        PlayerPrefs.DeleteKey(LastSceneKey);
        PlayerPrefs.Save();
    }
}