using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerUI : MonoBehaviour
{
    [Header("Target")]
    public PlayerStats player;

    [Header("HP")]
    public Slider hpSlider;
    public TMP_Text hpText;

    [Header("MP")]
    public Slider mpSlider;
    public TMP_Text mpText;

    [Header("EXP / Level")]
    public Slider expSlider;
    public TMP_Text expText;
    public TMP_Text levelText;

    private PlayerStats subscribedPlayer;

    private void OnEnable()
    {
        RebindPlayer();
    }

    private void Start()
    {
        RebindPlayer();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void RebindPlayer()
    {
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
            {
                player = playerObj.GetComponent<PlayerStats>();

                if (player == null)
                    player = playerObj.GetComponentInChildren<PlayerStats>();
            }
        }

        if (player == null)
        {
            Debug.LogWarning("[PlayerUI] PlayerStats를 찾지 못했습니다.");
            return;
        }

        Subscribe();
        RefreshAll();
    }

    private void Subscribe()
    {
        if (subscribedPlayer == player) return;

        Unsubscribe();

        subscribedPlayer = player;

        subscribedPlayer.OnHPChanged += UpdateHP;
        subscribedPlayer.OnMPChanged += UpdateMP;
        subscribedPlayer.OnExpChanged += UpdateExp;
        subscribedPlayer.OnDied += OnPlayerDied;
    }

    private void Unsubscribe()
    {
        if (subscribedPlayer == null) return;

        subscribedPlayer.OnHPChanged -= UpdateHP;
        subscribedPlayer.OnMPChanged -= UpdateMP;
        subscribedPlayer.OnExpChanged -= UpdateExp;
        subscribedPlayer.OnDied -= OnPlayerDied;

        subscribedPlayer = null;
    }

    private void RefreshAll()
    {
        if (player == null) return;

        UpdateHP(player.currentHP, player.maxHP);
        UpdateMP(player.currentMP, player.maxMP);
        UpdateExp(player.currentExp, player.expToNext, player.level);
    }

    private void UpdateHP(int cur, int max)
    {
        if (hpSlider)
        {
            hpSlider.maxValue = max;
            hpSlider.value = cur;
        }

        if (hpText)
            hpText.text = $"{cur} / {max}";
    }

    private void UpdateMP(int cur, int max)
    {
        if (mpSlider)
        {
            mpSlider.maxValue = max;
            mpSlider.value = cur;
        }

        if (mpText)
            mpText.text = $"{cur} / {max}";
    }

    private void UpdateExp(int cur, int need, int level)
    {
        if (expSlider)
        {
            expSlider.maxValue = need;
            expSlider.value = cur;
        }

        if (expText)
            expText.text = $"{cur} / {need}";

        if (levelText)
            levelText.text = $"Lv. {level}";
    }

    private void OnPlayerDied()
    {
        Debug.Log("Game Over");
    }
}