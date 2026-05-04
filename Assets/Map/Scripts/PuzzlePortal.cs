using UnityEngine;
using UnityEngine.SceneManagement;


public class PuzzlePortal : MonoBehaviour
{
    [Header("Scene Settings")]
    [Tooltip("전환할 씬 이름 (Build Settings에 등록 필요)")]
    [SerializeField] string puzzleSceneName = "PuzzleScene";

    [Header("Interaction Settings")]
    [SerializeField] KeyCode interactKey = KeyCode.E;
    [Tooltip("플레이어 태그 (Player 태그 사용)")]
    [SerializeField] string playerTag = "Player";

    bool playerInside = false;


    void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInside = true;
        Debug.Log("[PuzzleRoomTrigger] 플레이어 진입 - E 키로 씬 전환 가능");
    }

    void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag)) return;

        playerInside = false;
    }


    void Update()
    {
        if (playerInside && Input.GetKeyDown(interactKey))
        {
            EnterPuzzleScene();
        }
    }


    void EnterPuzzleScene()
    {
        if (string.IsNullOrEmpty(puzzleSceneName))
        {
            Debug.LogError("[PuzzleRoomTrigger] puzzleSceneName이 비어있습니다!");
            return;
        }
        GameObject player = GameObject.FindWithTag(playerTag);
        player.transform.SetParent(null);   // 부모에서 분리 (필수)
        DontDestroyOnLoad(player);          // 씬 전환 후에도 유지

        Debug.Log($"[PuzzleRoomTrigger] 씬 전환 → {puzzleSceneName}");

        SceneManager.LoadScene(puzzleSceneName);
    }


    void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = playerInside
            ? new Color(0f, 1f, 0.4f, 0.35f)
            : new Color(0f, 0.8f, 1f, 0.25f);

        if (col is BoxCollider box)
        {
            Matrix4x4 old = Gizmos.matrix;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
            Gizmos.matrix = old;
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.DrawSphere(
                transform.TransformPoint(sphere.center),
                sphere.radius * transform.lossyScale.x
            );
        }
    }
}