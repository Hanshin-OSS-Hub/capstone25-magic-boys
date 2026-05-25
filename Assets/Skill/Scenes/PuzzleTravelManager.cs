using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PuzzleTravelManager : MonoBehaviour
{
    public static PuzzleTravelManager Instance { get; private set; }

    [Header("Player")]
    [SerializeField] private string playerTag = "Player";

    [Header("Puzzle Spawn")]
    [Tooltip("퍼즐방 씬 안에 만들 빈 오브젝트 이름")]
    [SerializeField] private string puzzleSpawnPointName = "PuzzleSpawnPoint";

    [Tooltip("태그 방식으로 찾고 싶으면 사용. 태그가 없어도 문제 없게 처리됨")]
    [SerializeField] private string puzzleSpawnTag = "PuzzleSpawn";

    private string savedDungeonSceneName;
    private string currentPuzzleSceneName;

    private Vector3 savedReturnPosition;
    private Quaternion savedReturnRotation;

    private Transform playerTransform;

    private bool hasReturnData = false;
    private bool pendingMoveToPuzzleSpawn = false;
    private bool pendingReturnToDungeon = false;
    private bool isLoading = false;

    public bool IsLoading => isLoading;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    public void EnterPuzzleRoom(string puzzleSceneName, Transform player, Transform returnPoint = null)
    {
        if (isLoading) return;

        if (string.IsNullOrEmpty(puzzleSceneName))
        {
            Debug.LogError("[PuzzleTravelManager] puzzleSceneName이 비어있음");
            return;
        }

        if (player == null)
        {
            Debug.LogError("[PuzzleTravelManager] Player를 찾지 못함");
            return;
        }

        playerTransform = player;

        savedDungeonSceneName = SceneManager.GetActiveScene().name;
        currentPuzzleSceneName = puzzleSceneName;

        if (returnPoint != null)
        {
            savedReturnPosition = returnPoint.position;
            savedReturnRotation = returnPoint.rotation;
        }
        else
        {
            savedReturnPosition = player.position;
            savedReturnRotation = player.rotation;
        }

        hasReturnData = true;

        playerTransform.SetParent(null);
        DontDestroyOnLoad(playerTransform.gameObject);

        pendingMoveToPuzzleSpawn = true;
        pendingReturnToDungeon = false;

        Debug.Log($"[PuzzleTravelManager] 퍼즐방 이동: {savedDungeonSceneName} -> {puzzleSceneName}");

        StartCoroutine(LoadSceneRoutine(puzzleSceneName));
    }

    public void ReturnToDungeon()
    {
        if (isLoading) return;

        if (!hasReturnData || string.IsNullOrEmpty(savedDungeonSceneName))
        {
            Debug.LogError("[PuzzleTravelManager] 돌아갈 던전 정보가 없음");
            return;
        }

        pendingMoveToPuzzleSpawn = false;
        pendingReturnToDungeon = true;

        Debug.Log($"[PuzzleTravelManager] 던전 복귀: {savedDungeonSceneName}");

        StartCoroutine(LoadSceneRoutine(savedDungeonSceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        isLoading = true;

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);

        while (!op.isDone)
        {
            yield return null;
        }

        isLoading = false;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (pendingMoveToPuzzleSpawn && scene.name == currentPuzzleSceneName)
        {
            MovePlayerToPuzzleSpawn();
            pendingMoveToPuzzleSpawn = false;
        }

        if (pendingReturnToDungeon && scene.name == savedDungeonSceneName)
        {
            TeleportPlayer(savedReturnPosition, savedReturnRotation);
            pendingReturnToDungeon = false;
        }
    }

    private void MovePlayerToPuzzleSpawn()
    {
        Transform spawnPoint = FindPuzzleSpawnPoint();

        if (spawnPoint == null)
        {
            Debug.LogWarning("[PuzzleTravelManager] PuzzleSpawnPoint를 찾지 못해서 현재 위치 유지");
            return;
        }

        TeleportPlayer(spawnPoint.position, spawnPoint.rotation);
    }

    private Transform FindPuzzleSpawnPoint()
    {
        if (!string.IsNullOrEmpty(puzzleSpawnPointName))
        {
            GameObject point = GameObject.Find(puzzleSpawnPointName);
            if (point != null)
                return point.transform;
        }

        if (!string.IsNullOrEmpty(puzzleSpawnTag))
        {
            try
            {
                GameObject point = GameObject.FindWithTag(puzzleSpawnTag);
                if (point != null)
                    return point.transform;
            }
            catch
            {
                // 태그가 등록 안 되어 있어도 에러 안 나게 무시
            }
        }

        return null;
    }

    private void TeleportPlayer(Vector3 position, Quaternion rotation)
    {
        Transform player = GetPlayerTransform();

        if (player == null)
        {
            Debug.LogError("[PuzzleTravelManager] 이동시킬 Player를 찾지 못함");
            return;
        }

        CharacterController controller = player.GetComponent<CharacterController>();
        bool controllerWasEnabled = controller != null && controller.enabled;

        if (controllerWasEnabled)
            controller.enabled = false;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        player.SetPositionAndRotation(position, rotation);

        if (controllerWasEnabled)
            controller.enabled = true;

        Debug.Log($"[PuzzleTravelManager] Player 위치 이동 완료: {position}");
    }

    private Transform GetPlayerTransform()
    {
        if (playerTransform != null)
            return playerTransform;

        GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObj != null)
        {
            playerTransform = playerObj.transform;
            return playerTransform;
        }

        return null;
    }
}