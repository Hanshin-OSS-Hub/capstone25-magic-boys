using UnityEngine;
using UnityEngine.SceneManagement;

public class EndingPortal : MonoBehaviour
{
    [SerializeField]
    private KeyCode interactionKey = KeyCode.E;

    private bool isPlayerClose = false;

    void Update()
    {
        if (isPlayerClose && Input.GetKeyDown(interactionKey))
        {
            LoadEndingScene();
        }
    }

    private void LoadEndingScene()
    {
        SceneManager.LoadScene("EndingScene");
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerClose = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerClose = false;
        }
    }
}