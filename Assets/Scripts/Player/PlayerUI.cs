using UnityEngine;
using UnityEngine.UI;
using TMPro; 

public class PlayerUI : MonoBehaviour
{
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

    void OnEnable()
    {
        if (player == null) return;
        player.OnHPChanged += UpdateHP;
        player.OnMPChanged += UpdateMP;
        player.OnExpChanged += UpdateExp;
        player.OnDied += OnPlayerDied;
    }

    void OnDisable()
    {
        if (player == null) return;
        player.OnHPChanged -= UpdateHP;
        player.OnMPChanged -= UpdateMP;
        player.OnExpChanged -= UpdateExp;
        player.OnDied -= OnPlayerDied;
    }

    void Start()
    {
        if (player == null) return;
        UpdateHP(player.currentHP, player.maxHP);
        UpdateMP(player.currentMP, player.maxMP);
        UpdateExp(player.currentExp, player.expToNext, player.level);
    }

    void UpdateHP(int cur, int max)
    {
        if (hpSlider) { hpSlider.maxValue = max; hpSlider.value = cur; }
        if (hpText) hpText.text = $"{cur} / {max}";
    }

    void UpdateMP(int cur, int max)
    {
        if (mpSlider) { mpSlider.maxValue = max; mpSlider.value = cur; }
        if (mpText) mpText.text = $"{cur} / {max}";
    }

    void UpdateExp(int cur, int need, int level)
    {
        if (expSlider) { expSlider.maxValue = need; expSlider.value = cur; }
        if (expText) expText.text = $"{cur} / {need}";
        if (levelText) levelText.text = $"Lv. {level}";
    }

    void OnPlayerDied()
    {
        // 필요시 게임오버 UI 표시
        Debug.Log("Game Over");
    }
}