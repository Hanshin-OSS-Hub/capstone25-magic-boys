using UnityEngine;

public class PuzzleSocket : MonoBehaviour
{
    [Header("Socket Info")]
    [SerializeField] private int socketIndex = 1;
    [SerializeField] private SequentialPuzzleManager puzzleManager;

    [Header("Required Object")]
    [SerializeField] private string requiredTag = "Movable";
    [SerializeField] private Rigidbody requiredSpecificObject;

    [Header("Snap")]
    [SerializeField] private bool snapObjectToSocket = true;
    [SerializeField] private Transform snapPoint;

    private bool isSolved = false;

    private void OnTriggerEnter(Collider other)
    {
        TrySolveFromCollider(other);
    }

    private void OnTriggerStay(Collider other)
    {
        TrySolveFromCollider(other);
    }

    private void TrySolveFromCollider(Collider other)
    {
        if (isSolved)
            return;

        Rigidbody rb = other.GetComponentInParent<Rigidbody>();
        if (rb == null)
            return;

        if (!IsCorrectObject(rb))
            return;

        if (puzzleManager == null)
        {
            Debug.LogWarning("[PuzzleSocket] puzzleManager가 연결되지 않았습니다.");
            return;
        }

        if (!puzzleManager.CanSolveSocket(socketIndex))
        {
            Debug.Log("[PuzzleSocket] 아직 이 소켓을 해결할 순서가 아닙니다.");
            return;
        }

        Solve(rb);
    }

    private bool IsCorrectObject(Rigidbody rb)
    {
        if (requiredSpecificObject != null)
            return rb == requiredSpecificObject;

        return rb.CompareTag(requiredTag);
    }

    private void Solve(Rigidbody rb)
    {
        isSolved = true;

        if (snapObjectToSocket)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;

            if (snapPoint != null)
            {
                rb.transform.position = snapPoint.position;
                rb.transform.rotation = snapPoint.rotation;
            }
            else
            {
                rb.transform.position = transform.position;
            }
        }

        puzzleManager.SolveSocket(socketIndex);
        Debug.Log("[PuzzleSocket] 소켓 해결 완료: " + socketIndex);
    }
}