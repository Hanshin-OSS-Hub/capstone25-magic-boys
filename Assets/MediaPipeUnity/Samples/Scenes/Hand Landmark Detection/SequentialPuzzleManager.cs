using UnityEngine;

public class SequentialPuzzleManager : MonoBehaviour
{
    [Header("Stage Objects")]
    [SerializeField] private GameObject bridgeOrStairsToShow;
    [SerializeField] private GameObject wallToHide;

    private bool firstSolved = false;
    private bool secondSolved = false;

    void Start()
    {
        if (bridgeOrStairsToShow != null)
            bridgeOrStairsToShow.SetActive(false);

        if (wallToHide != null)
            wallToHide.SetActive(true);
    }

    public bool CanSolveSocket(int socketIndex)
    {
        if (socketIndex == 1)
            return true;

        if (socketIndex == 2)
            return firstSolved;

        return false;
    }

    public void SolveSocket(int socketIndex)
    {
        if (socketIndex == 1 && !firstSolved)
        {
            firstSolved = true;

            if (bridgeOrStairsToShow != null)
                bridgeOrStairsToShow.SetActive(true);

            Debug.Log("[Puzzle] 1번 퍼즐 해결 - 계단/다리 생성");
            return;
        }

        if (socketIndex == 2 && firstSolved && !secondSolved)
        {
            secondSolved = true;

            if (wallToHide != null)
                wallToHide.SetActive(false);

            Debug.Log("[Puzzle] 2번 퍼즐 해결 - 벽 제거");
        }
    }
}