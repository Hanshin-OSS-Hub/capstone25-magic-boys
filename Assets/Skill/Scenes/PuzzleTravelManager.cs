using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PuzzleTravelManager : MonoBehaviour
{
    public static PuzzleTravelManager Instance { get; private set; }
    [Header("Player")]
    [SerializeField] private string playerTag = "Player";
    [Header("Puzzle Spawn")]
    [SerializeField] private string puzzleSpawnPointName = "PuzzleSpawnPoint";
    [SerializeField] private string puzzleSpawnTag = "PuzzleSpawn";

    // 현재 세션의 던전만 보관한다. Single 로드로 떠날 때는 함께 해제된다.
    private Scene dungeonScene;
    private Scene puzzleScene;
    private readonly List<GameObject> suspendedDungeonRoots = new List<GameObject>();
    private Transform playerTransform;
    private Vector3 savedReturnPosition;
    private Quaternion savedReturnRotation;
    private Vector3 departurePosition;
    private Quaternion departureRotation;
    private string currentPuzzleSceneName;
    private string dungeonBeforeTravel;
    private bool hasReturnData;
    private bool inPuzzle;
    private bool isLoading;
    private int transitionNumber;
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
        SceneManager.sceneUnloaded += OnSceneUnloaded;
    }

    private void OnDestroy()
    {
        if (Instance != this) return;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneUnloaded -= OnSceneUnloaded;
        Instance = null;
    }

    public void EnterPuzzleRoom(string sceneName, Transform player, Transform returnPoint = null,
        Transform entryPortal = null)
    {
        if (isLoading || inPuzzle) return;
        if (player == null || string.IsNullOrEmpty(sceneName)) return;
        if (!player.root.CompareTag(playerTag)) return;
        Scene source = entryPortal != null ? entryPortal.gameObject.scene : SceneManager.GetActiveScene();
        DungeonGenerator generator = FindInScene<DungeonGenerator>(source);
        if (generator == null || generator.dungeonState != DungeonGenState.completed)
        {
            Debug.LogWarning("[PuzzleTravel] 생성 완료된 던전에서만 진입할 수 있습니다.");
            return;
        }
        if (SceneManager.GetSceneByName(sceneName).isLoaded)
        {
            Debug.LogError("[PuzzleTravel] 목적 퍼즐 씬이 이미 로드되어 있습니다: " + sceneName);
            return;
        }

        playerTransform = player.root;
        departurePosition = playerTransform.position;
        departureRotation = playerTransform.rotation;
        savedReturnPosition = returnPoint != null ? returnPoint.position : departurePosition;
        savedReturnRotation = returnPoint != null ? returnPoint.rotation : departureRotation;
        dungeonScene = source;
        currentPuzzleSceneName = sceneName;
        dungeonBeforeTravel = DescribeDungeon(source);
        hasReturnData = true;
        isLoading = true;
        transitionNumber++;

        // 음성 객체 둘만 기존 플레이어의 수명에 맞춘다. 던전 전체를 DDOL로 옮기지 않는다.
        VoiceCommandSystem voice = FindInScene<VoiceCommandSystem>(source);
        if (voice != null) voice.KeepWithPlayer(playerTransform);
        playerTransform.SetParent(null);
        DontDestroyOnLoad(playerTransform.gameObject);
        playerTransform.gameObject.SetActive(false);
        suspendedDungeonRoots.Clear();
        foreach (GameObject root in source.GetRootGameObjects())
        {
            if (!root.activeSelf) continue;
            suspendedDungeonRoots.Add(root);
            root.SetActive(false);
        }
        Debug.Log($"[PuzzleTravel #{transitionNumber}] 진입 {dungeonBeforeTravel} " +
            $"portal={(entryPortal != null ? entryPortal.GetInstanceID() : 0)} " +
            $"player={playerTransform.GetInstanceID()} return={savedReturnPosition} -> {sceneName}");
        StartCoroutine(EnterRoutine());
    }

    private IEnumerator EnterRoutine()
    {
        AsyncOperation load = null;
        try { load = SceneManager.LoadSceneAsync(currentPuzzleSceneName, LoadSceneMode.Additive); }
        catch (System.Exception error) { Debug.LogError("[PuzzleTravel] 퍼즐 로드 실패: " + error.Message); }
        if (load != null) yield return load;
        // sceneLoaded에서는 Start가 아직 끝나지 않았다. 배치는 한 프레임 뒤에 한다.
        yield return null;
        if (!puzzleScene.IsValid() || !puzzleScene.isLoaded)
        {
            yield return CancelEntry("퍼즐 씬을 로드하지 못했습니다.");
            yield break;
        }
        Transform spawn = FindPuzzleSpawnPoint(puzzleScene);
        Physics.SyncTransforms();
        if (spawn == null || !TryPlacePlayer(puzzleScene, spawn.position, spawn.rotation))
        {
            yield return CancelEntry("퍼즐 스폰 지점 또는 해당 씬의 안전한 바닥을 찾지 못했습니다.");
            yield break;
        }
        inPuzzle = true;
        playerTransform.gameObject.SetActive(true);
        yield return null;
        isLoading = false;
    }

    public void ReturnToDungeon()
    {
        if (isLoading) return;
        if (!inPuzzle || !hasReturnData || !dungeonScene.IsValid() || !dungeonScene.isLoaded)
        {
            Debug.LogError("[PuzzleTravel] 보관 중인 던전이 없습니다. 좌표만으로 새 던전을 만들지 않습니다.");
            return;
        }
        isLoading = true;
        transitionNumber++;
        playerTransform.gameObject.SetActive(false);
        StartCoroutine(ReturnRoutine());
    }

    private IEnumerator ReturnRoutine()
    {
        SceneManager.SetActiveScene(dungeonScene);
        yield return SceneManager.UnloadSceneAsync(puzzleScene);
        puzzleScene = default;
        ResumeDungeon();
        yield return null;
        Physics.SyncTransforms();
        bool placed = TryPlacePlayer(dungeonScene, savedReturnPosition, savedReturnRotation)
            || TryPlacePlayer(dungeonScene, departurePosition, departureRotation);
        string after = DescribeDungeon(dungeonScene);
        Debug.Log($"[PuzzleTravel #{transitionNumber}] 던전 복귀 {after} " +
            $"동일던전={after == dungeonBeforeTravel} 배치={placed}");
        FinishReturn(placed);
    }

    private IEnumerator CancelEntry(string reason)
    {
        Debug.LogError("[PuzzleTravel] 진입 취소: " + reason);
        SceneManager.SetActiveScene(dungeonScene);
        if (puzzleScene.IsValid() && puzzleScene.isLoaded)
            yield return SceneManager.UnloadSceneAsync(puzzleScene);
        puzzleScene = default;
        ResumeDungeon();
        yield return null;
        Physics.SyncTransforms();
        FinishReturn(TryPlacePlayer(dungeonScene, departurePosition, departureRotation));
    }

    private void ResumeDungeon()
    {
        foreach (GameObject root in suspendedDungeonRoots)
            if (root != null) root.SetActive(true);
        suspendedDungeonRoots.Clear();
    }

    private void FinishReturn(bool placed)
    {
        inPuzzle = false;
        hasReturnData = false;
        currentPuzzleSceneName = null;
        isLoading = false;
        if (placed) playerTransform.gameObject.SetActive(true);
        else Debug.LogError("[PuzzleTravel] 안전한 복귀 바닥이 없습니다. 추락 방지를 위해 플레이어를 정지했습니다.");
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!isLoading || inPuzzle || mode != LoadSceneMode.Additive || scene.name != currentPuzzleSceneName) return;
        puzzleScene = scene;
        // 퍼즐 Start에서 생성하는 객체도 퍼즐 씬에 속하도록 먼저 바꾼다.
        SceneManager.SetActiveScene(scene);
    }

    private void OnSceneUnloaded(Scene scene)
    {
        if (dungeonScene.handle == 0 || scene.handle != dungeonScene.handle) return;
        StopAllCoroutines();
        suspendedDungeonRoots.Clear();
        hasReturnData = inPuzzle = isLoading = false;
        dungeonScene = puzzleScene = default;
        currentPuzzleSceneName = null;
    }

    private Transform FindPuzzleSpawnPoint(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        foreach (Transform candidate in root.GetComponentsInChildren<Transform>(true))
        {
            if (!candidate.gameObject.activeInHierarchy) continue;
            if (candidate.name == puzzleSpawnPointName ||
                (!string.IsNullOrEmpty(puzzleSpawnTag) && candidate.gameObject.tag == puzzleSpawnTag))
                return candidate;
        }
        return null;
    }

    private bool TryPlacePlayer(Scene scene, Vector3 anchor, Quaternion rotation)
    {
        if (playerTransform == null) return false;
        CharacterController controller = playerTransform.GetComponent<CharacterController>();
        RaycastHit[] hits = Physics.RaycastAll(anchor + Vector3.up * 2f, Vector3.down, 12f,
            ~0, QueryTriggerInteraction.Ignore);
        System.Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject.scene.handle != scene.handle || hit.normal.y < 0.5f
                || hit.point.y > anchor.y + 0.5f) continue;
            Vector3 position = anchor;
            float scaleY = Mathf.Abs(playerTransform.lossyScale.y);
            float bottom = controller != null ? (controller.center.y - controller.height * 0.5f) * scaleY : 0f;
            float clearance = controller != null ? Mathf.Max(0.05f, controller.skinWidth) : 0.05f;
            position.y = hit.point.y - bottom + clearance;
            if (controller != null && IsBlocked(scene, controller, position, rotation)) continue;

            bool controllerEnabled = controller != null && controller.enabled;
            if (controllerEnabled) controller.enabled = false;
            Rigidbody body = playerTransform.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            playerTransform.SetPositionAndRotation(position, rotation);
            StarterAssets.ThirdPersonController movement = playerTransform.GetComponent<StarterAssets.ThirdPersonController>();
            if (movement != null) movement.ResetAfterTeleport();
            Physics.SyncTransforms();
            if (controllerEnabled) controller.enabled = true;
            Debug.Log($"[PuzzleTravel #{transitionNumber}] 배치 scene={scene.name}#{scene.handle} " +
                $"player={playerTransform.GetInstanceID()} anchor={anchor} position={position} " +
                $"floor={hit.collider.name}#{hit.collider.GetInstanceID()}");
            return true;
        }
        return false;
    }

    private bool IsBlocked(Scene scene, CharacterController controller, Vector3 position, Quaternion rotation)
    {
        Vector3 scale = playerTransform.lossyScale;
        float radius = controller.radius * Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.z));
        float halfHeight = Mathf.Max(radius, controller.height * Mathf.Abs(scale.y) * 0.5f);
        Vector3 center = position + rotation * Vector3.Scale(controller.center, scale);
        Vector3 offset = Vector3.up * (halfHeight - radius);
        foreach (Collider obstacle in Physics.OverlapCapsule(center - offset, center + offset,
            Mathf.Max(0.01f, radius - 0.02f), ~0, QueryTriggerInteraction.Ignore))
        {
            if (obstacle.gameObject.scene.handle == scene.handle &&
                !obstacle.transform.IsChildOf(playerTransform)) return true;
        }
        return false;
    }

    private static T FindInScene<T>(Scene scene) where T : Component
    {
        if (!scene.IsValid() || !scene.isLoaded) return null;
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            T component = root.GetComponentInChildren<T>(true);
            if (component != null) return component;
        }
        return null;
    }

    private static string DescribeDungeon(Scene scene)
    {
        DungeonGenerator generator = FindInScene<DungeonGenerator>(scene);
        if (generator == null) return scene.name + " 생성기 없음";
        int layout = 17;
        unchecked
        {
            foreach (Tile tile in generator.genneratedTiles)
                if (tile.tile != null)
                    layout = layout * 31 + tile.tile.GetInstanceID() + tile.tile.position.GetHashCode()
                        + tile.tile.rotation.GetHashCode();
        }
        return $"{scene.name}#{scene.handle} generator={generator.GetInstanceID()} " +
            $"seed={DungeonGenerator.saveSeed} rooms={generator.genneratedTiles.Count} layout={layout:X8}";
    }
}
