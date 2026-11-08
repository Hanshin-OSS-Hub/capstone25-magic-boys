using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// 화면 좌측 상단 미니맵 (씬 수정 없이 게임 시작 시 자동 생성)
// - 던전 생성이 끝나면 NavMesh(걸을 수 있는 바닥)를 그대로 지도 모양으로 사용 → 방/복도/보스방 모양이 정확하고 조명과 무관
// - 가본 곳만 밝혀짐 (안개), 카메라가 보는 방향이 항상 위쪽
// - 가운데 화살표 = 플레이어, 빨간 점 = 몬스터, 큰 점 = 보스
// - N: 미니맵 켜기/끄기, M(전체 지도)·사망·던전이 없는 씬에서는 자동으로 숨김
public class Minimap : MonoBehaviour
{
    [Header("표시")]
    public KeyCode toggleKey = KeyCode.N;
    public float panelSize = 230f;          // 화면 픽셀 (1080p 기준)
    public float pixelsPerMeter = 2.6f;     // 확대 비율
    public float revealRadius = 14f;        // 플레이어 주변 이 거리 안의 바닥이 지도에 밝혀짐
    public float enemyRadius = 30f;

    [Header("색")]
    public Color floorColor = new Color(0.86f, 0.74f, 0.52f, 0.9f);
    public Color backgroundColor = new Color(0.05f, 0.04f, 0.04f, 0.78f);
    public Color frameColor = new Color(0.75f, 0.58f, 0.3f, 1f);
    public Color playerColor = new Color(0.35f, 0.85f, 1f, 1f);
    public Color enemyColor = new Color(1f, 0.25f, 0.15f, 1f);
    public Color bossColor = new Color(1f, 0.1f, 0.4f, 1f);

    static Minimap instance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Boot()
    {
        if (instance != null) return;
        var go = new GameObject("[Minimap]");
        DontDestroyOnLoad(go);
        go.AddComponent<Minimap>();
    }

    // UI
    Canvas canvas;
    RectTransform panel, content, arrow;
    MinimapFloorGraphic floor;
    readonly List<Image> dots = new List<Image>();
    Sprite whiteSprite, dotSprite, arrowSprite;

    // 상태
    bool userVisible = true;
    DungeonGenerator generator;
    GameObject overheadCamera;
    Transform player;
    PlayerDeath playerDeath;
    float nextPlayerSearch, nextRevealCheck, nextEnemyScan, nextMeshCheck;
    int lastNavVertexCount = -1;

    Vector3[] navVerts;
    int[] navTris;
    bool[] revealed;
    readonly Collider[] enemyHits = new Collider[64];
    readonly List<(Vector3 pos, bool boss)> enemies = new List<(Vector3, bool)>();

    void Awake()
    {
        instance = this;
        BuildUI();
        SceneManager.sceneLoaded += OnSceneLoaded;
        ResetForScene();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (instance == this) instance = null;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single) ResetForScene();
    }

    void ResetForScene()
    {
        generator = null;
        overheadCamera = null;
        player = null;
        playerDeath = null;
        navVerts = null; navTris = null; revealed = null;
        lastNavVertexCount = -1;
        if (floor != null) floor.SetMesh(null, null, null);

        var g = GameObject.Find("Generator");
        if (g != null) generator = g.GetComponent<DungeonGenerator>();
        overheadCamera = GameObject.Find("OverheadCamera");
    }

    // ───────────────────────── 매 프레임 ─────────────────────────
    void Update()
    {
        if (Input.GetKeyDown(toggleKey)) userVisible = !userVisible;

        bool dungeonReady = generator != null && generator.dungeonState == DungeonGenState.completed;
        FindPlayer();

        bool show = userVisible && dungeonReady && player != null && player.gameObject.activeInHierarchy
                    && (playerDeath == null || !playerDeath.IsDead)
                    && (overheadCamera == null || !overheadCamera.activeInHierarchy);
        canvas.enabled = show;
        if (!show) return;

        float now = Time.unscaledTime;

        // NavMesh가 바뀌었으면(퍼즐 방 추가 등) 다시 읽기
        if (navVerts == null || now >= nextMeshCheck)
        {
            nextMeshCheck = now + 2f;
            RefreshNavMesh();
        }

        if (now >= nextRevealCheck)
        {
            nextRevealCheck = now + 0.2f;
            if (Reveal(player.position)) floor.SetMesh(navVerts, navTris, revealed, pixelsPerMeter, floorColor);
        }

        if (now >= nextEnemyScan)
        {
            nextEnemyScan = now + 0.15f;
            ScanEnemies(player.position);
        }

        // 지도 회전/이동: 카메라 정면이 위쪽, 플레이어가 가운데
        Transform cam = Camera.main != null ? Camera.main.transform : player;
        float camYaw = cam.eulerAngles.y;
        Quaternion rot = Quaternion.Euler(0f, 0f, camYaw);
        Vector2 p = new Vector2(player.position.x, player.position.z) * pixelsPerMeter;
        content.localRotation = rot;
        content.anchoredPosition = -(Vector2)(rot * p);

        arrow.localRotation = Quaternion.Euler(0f, 0f, camYaw - player.eulerAngles.y);

        UpdateDots();
    }

    void FindPlayer()
    {
        if (player != null && player.gameObject.activeInHierarchy) return;
        if (Time.unscaledTime < nextPlayerSearch) return;
        nextPlayerSearch = Time.unscaledTime + 0.5f;

        var go = GameObject.FindGameObjectWithTag("Player");
        player = go != null ? go.transform : null;
        playerDeath = null;
        if (go != null)
        {
            playerDeath = go.GetComponentInParent<PlayerDeath>();
            if (playerDeath == null) playerDeath = go.GetComponentInChildren<PlayerDeath>();
        }
    }

    // ───────────────────────── 지도 데이터 ─────────────────────────
    void RefreshNavMesh()
    {
        NavMeshTriangulation tri = NavMesh.CalculateTriangulation();
        if (tri.vertices.Length == lastNavVertexCount && navVerts != null) return;
        lastNavVertexCount = tri.vertices.Length;

        // 이미 밝혀진 곳은 유지 (삼각형 중심 좌표로 다시 매칭)
        var keep = new HashSet<Vector3Int>();
        if (navVerts != null)
            for (int t = 0; t < revealed.Length; t++)
                if (revealed[t]) keep.Add(Key(Centroid(navVerts, navTris, t)));

        navVerts = tri.vertices;
        navTris = tri.indices;
        revealed = new bool[navTris.Length / 3];
        for (int t = 0; t < revealed.Length; t++)
            revealed[t] = keep.Contains(Key(Centroid(navVerts, navTris, t)));

        Reveal(player.position);
        floor.SetMesh(navVerts, navTris, revealed, pixelsPerMeter, floorColor);
    }

    static Vector3 Centroid(Vector3[] v, int[] idx, int t) => (v[idx[t * 3]] + v[idx[t * 3 + 1]] + v[idx[t * 3 + 2]]) / 3f;
    static Vector3Int Key(Vector3 c) => new Vector3Int(Mathf.RoundToInt(c.x * 2f), Mathf.RoundToInt(c.y * 2f), Mathf.RoundToInt(c.z * 2f));

    bool Reveal(Vector3 pos)
    {
        if (revealed == null) return false;
        bool changed = false;
        float r2 = revealRadius * revealRadius;
        for (int t = 0; t < revealed.Length; t++)
        {
            if (revealed[t]) continue;
            Vector3 a = navVerts[navTris[t * 3]], b = navVerts[navTris[t * 3 + 1]], c = navVerts[navTris[t * 3 + 2]];
            // 다른 층(위/아래)의 바닥은 밝히지 않음
            float minY = Mathf.Min(a.y, Mathf.Min(b.y, c.y)), maxY = Mathf.Max(a.y, Mathf.Max(b.y, c.y));
            if (pos.y < minY - 4f || pos.y > maxY + 4f) continue;
            if (FlatDist2(pos, ClosestPointOnTriangleXZ(pos, a, b, c)) <= r2) { revealed[t] = true; changed = true; }
        }
        return changed;
    }

    static float FlatDist2(Vector3 a, Vector2 b) { float dx = a.x - b.x, dz = a.z - b.y; return dx * dx + dz * dz; }

    // XZ 평면에서 삼각형 위 가장 가까운 점 (큰 삼각형도 정확히 판정)
    static Vector2 ClosestPointOnTriangleXZ(Vector3 p3, Vector3 a3, Vector3 b3, Vector3 c3)
    {
        Vector2 p = new Vector2(p3.x, p3.z), a = new Vector2(a3.x, a3.z), b = new Vector2(b3.x, b3.z), c = new Vector2(c3.x, c3.z);
        Vector2 ab = b - a, ac = c - a, ap = p - a;
        float d1 = Vector2.Dot(ab, ap), d2 = Vector2.Dot(ac, ap);
        if (d1 <= 0f && d2 <= 0f) return a;
        Vector2 bp = p - b; float d3 = Vector2.Dot(ab, bp), d4 = Vector2.Dot(ac, bp);
        if (d3 >= 0f && d4 <= d3) return b;
        float vc = d1 * d4 - d3 * d2;
        if (vc <= 0f && d1 >= 0f && d3 <= 0f) return a + ab * (d1 / (d1 - d3));
        Vector2 cp = p - c; float d5 = Vector2.Dot(ab, cp), d6 = Vector2.Dot(ac, cp);
        if (d6 >= 0f && d5 <= d6) return c;
        float vb = d5 * d2 - d1 * d6;
        if (vb <= 0f && d2 >= 0f && d6 <= 0f) return a + ac * (d2 / (d2 - d6));
        float va = d3 * d6 - d5 * d4;
        if (va <= 0f && (d4 - d3) >= 0f && (d5 - d6) >= 0f) return b + (c - b) * ((d4 - d3) / ((d4 - d3) + (d5 - d6)));
        float denom = 1f / (va + vb + vc);
        return a + ab * (vb * denom) + ac * (vc * denom);
    }

    // ───────────────────────── 몬스터 표시 ─────────────────────────
    void ScanEnemies(Vector3 pos)
    {
        enemies.Clear();
        int enemyLayer = LayerMask.NameToLayer("Enemy"), bossLayer = LayerMask.NameToLayer("Boss");
        int mask = (enemyLayer >= 0 ? 1 << enemyLayer : 0) | (bossLayer >= 0 ? 1 << bossLayer : 0);
        if (mask == 0) return;

        int n = Physics.OverlapSphereNonAlloc(pos, enemyRadius, enemyHits, mask, QueryTriggerInteraction.Ignore);
        var seen = new HashSet<Transform>();
        for (int i = 0; i < n; i++)
        {
            var col = enemyHits[i];
            if (col == null) continue;
            // 몬스터 하나에 콜라이더가 여러 개여도 점은 하나
            var agent = col.GetComponentInParent<NavMeshAgent>();
            Transform root = agent != null ? agent.transform : col.transform.root;
            if (!seen.Add(root)) continue;
            enemies.Add((root.position, root.gameObject.layer == bossLayer || col.gameObject.layer == bossLayer));
        }
    }

    void UpdateDots()
    {
        while (dots.Count < enemies.Count)
        {
            var img = MakeImage("Dot", content, dotSprite, enemyColor);
            dots.Add(img);
        }
        for (int i = 0; i < dots.Count; i++)
        {
            bool on = i < enemies.Count;
            dots[i].enabled = on;
            if (!on) continue;
            var (pos, boss) = enemies[i];
            var rt = dots[i].rectTransform;
            rt.anchoredPosition = new Vector2(pos.x, pos.z) * pixelsPerMeter;
            rt.sizeDelta = Vector2.one * (boss ? 16f : 8f);
            dots[i].color = boss ? bossColor : enemyColor;
        }
    }

    // ───────────────────────── UI 생성 ─────────────────────────
    void BuildUI()
    {
        whiteSprite = MakeSprite(4, (x, y, s) => 1f);
        dotSprite = MakeSprite(32, (x, y, s) =>
        {
            float d = Vector2.Distance(new Vector2(x, y), new Vector2(s / 2f - 0.5f, s / 2f - 0.5f)) / (s / 2f);
            return Mathf.Clamp01((1f - d) * 6f);
        });
        arrowSprite = MakeSprite(64, (x, y, s) =>
        {
            // 위쪽을 가리키는 화살촉
            float u = (x + 0.5f) / s - 0.5f, v = (y + 0.5f) / s;
            float half = (1f - v) * 0.45f;                       // 위로 갈수록 좁아짐
            float notch = Mathf.Clamp01(0.35f - Mathf.Abs(u) * 0.8f); // 아래 가운데 홈
            if (v < notch) return 0f;
            return Mathf.Abs(u) <= half ? 1f : 0f;
        });

        var cgo = new GameObject("MinimapCanvas");
        cgo.transform.SetParent(transform, false);
        canvas = cgo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 40;
        var scaler = cgo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 1f;

        // 테두리 + 배경
        var frame = MakeImage("Frame", cgo.transform, whiteSprite, frameColor).rectTransform;
        frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(0f, 1f); // 왼쪽 위 (오른쪽 위는 웹캠 화면)
        frame.anchoredPosition = new Vector2(24f, -24f);
        frame.sizeDelta = Vector2.one * (panelSize + 6f);

        var bg = MakeImage("Panel", frame, whiteSprite, backgroundColor);
        panel = bg.rectTransform;
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = Vector2.one * panelSize;
        bg.gameObject.AddComponent<RectMask2D>();

        var contentGo = new GameObject("Content", typeof(RectTransform));
        content = contentGo.GetComponent<RectTransform>();
        content.SetParent(panel, false);
        content.anchorMin = content.anchorMax = content.pivot = new Vector2(0.5f, 0.5f);
        content.sizeDelta = Vector2.zero;

        var floorGo = new GameObject("Floor", typeof(RectTransform), typeof(CanvasRenderer));
        var frt = floorGo.GetComponent<RectTransform>();
        frt.SetParent(content, false);
        frt.anchorMin = frt.anchorMax = frt.pivot = new Vector2(0.5f, 0.5f);
        frt.sizeDelta = Vector2.one * 100000f; // 마스크 컬링에 걸리지 않게 크게 (실제 모양은 메쉬 좌표로 그림)
        floor = floorGo.AddComponent<MinimapFloorGraphic>();
        floor.raycastTarget = false;

        var arrowImg = MakeImage("Player", panel, arrowSprite, playerColor);
        arrow = arrowImg.rectTransform;
        arrow.sizeDelta = new Vector2(18f, 22f);

        canvas.enabled = false;
    }

    static Image MakeImage(string name, Transform parent, Sprite sprite, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static Sprite MakeSprite(int size, System.Func<int, int, int, float> alpha)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha(x, y, size)));
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
