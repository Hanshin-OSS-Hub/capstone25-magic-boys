using System.Collections;
using StarterAssets;
using UnityEngine;
using UnityEngine.SceneManagement;

// HP가 0이 되면: 조작 정지 → 사망 애니메이션 → 화면 어두워짐 + GAME OVER.
// R: 다시 시작, Enter: 메인 메뉴. (ThirdPersonController가 자동으로 붙여준다)
public class PlayerDeath : MonoBehaviour
{
    [Tooltip("애니메이터의 사망 상태 이름 (Tools/Magic Boys/Setup Death Animation)")]
    public string deathState = "Death";
    public float gameOverDelay = 1.8f;
    public string mainMenuScene = "MainMenu";
    public KeyCode restartKey = KeyCode.R;
    public KeyCode menuKey = KeyCode.Return; // Esc(일시정지), M(지도)과 겹치지 않게

    public bool IsDead { get; private set; }

    private PlayerStats stats;
    private Animator animator;
    private float fade;
    private bool showGameOver;
    private GUIStyle titleStyle, hintStyle;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        animator = GetComponent<Animator>();
    }

    void OnEnable()
    {
        if (stats != null) stats.OnDied += HandleDied;
    }

    void OnDisable()
    {
        if (stats != null) stats.OnDied -= HandleDied;
    }

    private void HandleDied()
    {
        if (IsDead) return;
        IsDead = true;

        var tpc = GetComponent<ThirdPersonController>();
        if (tpc != null) tpc.IsDead = true;

        // 공격/구르기/마법/락온 정지
        foreach (var mb in new MonoBehaviour[]
                 {
                     GetComponent<PlayerAttack>(), GetComponent<PlayerRoll>(),
                     GetComponent<MagicAttack>(), GetComponent<LockOnTarget>()
                 })
        {
            if (mb != null) mb.enabled = false;
        }
        var attack = GetComponent<PlayerAttack>();
        if (attack != null) attack.DisableHammerCollider();

        if (animator != null)
        {
            int hash = Animator.StringToHash(deathState);
            if (animator.HasState(0, hash))
                animator.CrossFadeInFixedTime(hash, 0.15f, 0);
            else
                Debug.LogWarning("[PlayerDeath] 애니메이터에 Death 상태가 없습니다. Tools/Magic Boys/Setup Death Animation 을 실행하세요.");
        }

        StartCoroutine(GameOverRoutine());
    }

    private IEnumerator GameOverRoutine()
    {
        yield return new WaitForSecondsRealtime(gameOverDelay);
        showGameOver = true;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        while (true)
        {
            fade = Mathf.MoveTowards(fade, 1f, Time.unscaledDeltaTime * 1.2f);

            if (fade > 0.6f)
            {
                if (Input.GetKeyDown(restartKey))
                {
                    LeaveTo(SceneManager.GetActiveScene().name);
                    yield break;
                }
                if (Input.GetKeyDown(menuKey) && Application.CanStreamedLevelBeLoaded(mainMenuScene))
                {
                    LeaveTo(mainMenuScene);
                    yield break;
                }
            }
            yield return null;
        }
    }

    private void LeaveTo(string sceneName)
    {
        if (stats != null) stats.Heal(stats.maxHP);
        Time.timeScale = 1f;
        showGameOver = false;

        // 플레이어는 PersistentPlayer(DontDestroyOnLoad)라 씬을 다시 불러와도 살아남는다.
        // 그대로 두면 새 씬의 멀쩡한 플레이어가 '중복'으로 지워지고 죽은 플레이어 + GAME OVER 화면이 남음
        // → 씬 전환 전에 죽은 플레이어를 정리해서 새 씬의 플레이어가 쓰이게 한다.
        var persistent = GetComponentInParent<PersistentPlayer>();
        if (persistent != null && persistent == PersistentPlayer.Instance)
            PersistentPlayer.DestroyCurrent();

        SceneManager.LoadScene(sceneName);
    }

    void OnGUI()
    {
        if (!showGameOver) return;

        if (titleStyle == null)
        {
            titleStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold
            };
            hintStyle = new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleCenter };
        }
        titleStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.11f);
        hintStyle.fontSize = Mathf.RoundToInt(Screen.height * 0.03f);

        Color prev = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, fade * 0.75f);
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);

        GUI.color = new Color(0.75f, 0.08f, 0.06f, fade);
        GUI.Label(new Rect(0, Screen.height * 0.32f, Screen.width, Screen.height * 0.18f), "GAME OVER", titleStyle);

        GUI.color = new Color(1f, 1f, 1f, Mathf.Clamp01(fade * 2f - 1f));
        GUI.Label(new Rect(0, Screen.height * 0.55f, Screen.width, Screen.height * 0.06f),
            $"{restartKey}: 다시 시작     Enter: 메인 메뉴", hintStyle);
        GUI.color = prev;
    }
}
