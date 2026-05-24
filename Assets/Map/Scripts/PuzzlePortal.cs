using UnityEngine;

public class PuzzlePortal : MonoBehaviour
{
    [Header("Portal Type")]
    [SerializeField] private bool isReturnPortal = false;

    [Header("Scene Settings")]
    [SerializeField] private string puzzleSceneName = "PuzzleScene";

    [Header("Return Settings")]
    [Tooltip("던전으로 돌아왔을 때 배치될 위치. 던전 포탈 근처에 빈 오브젝트로 만들어두면 좋음")]
    [SerializeField] private Transform returnPoint;

    [Header("Interaction Settings")]
    [SerializeField] private KeyCode interactKey = KeyCode.E;
    [SerializeField] private string playerTag = "Player";

    private bool playerInside = false;
    private Transform currentPlayer;

    private void OnTriggerEnter(Collider other)
    {
        if (!TryGetPlayer(other, out Transform player)) return;

        playerInside = true;
        currentPlayer = player;

        Debug.Log("[PuzzlePortal] 플레이어 진입 - E 입력 가능");
    }

    private void OnTriggerExit(Collider other)
    {
        if (!TryGetPlayer(other, out Transform player)) return;
        if (player != currentPlayer) return;

        playerInside = false;
        currentPlayer = null;
    }

    private void Update()
    {
        if (!playerInside) return;
        if (!Input.GetKeyDown(interactKey)) return;

        if (PuzzleTravelManager.Instance == null)
        {
            Debug.LogError("[PuzzlePortal] PuzzleTravelManager가 씬에 없음");
            return;
        }

        if (PuzzleTravelManager.Instance.IsLoading) return;

        if (isReturnPortal)
        {
            PuzzleTravelManager.Instance.ReturnToDungeon();
        }
        else
        {
            PuzzleTravelManager.Instance.EnterPuzzleRoom(
                puzzleSceneName,
                currentPlayer,
                returnPoint
            );
        }
    }

    private bool TryGetPlayer(Collider other, out Transform player)
    {
        if (other.CompareTag(playerTag))
        {
            player = other.transform;
            return true;
        }

        Transform root = other.transform.root;

        if (root.CompareTag(playerTag))
        {
            player = root;
            return true;
        }

        player = null;
        return false;
    }

    private void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = isReturnPortal
            ? new Color(1f, 0.6f, 0f, 0.3f)
            : new Color(0f, 0.8f, 1f, 0.3f);

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