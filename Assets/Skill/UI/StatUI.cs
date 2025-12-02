using UnityEngine;
using TMPro;

public class StatUI : MonoBehaviour
{
    [Header("References")]
    public PlayerStats player;

    [Header("Stats")]
    public TMP_Text strText;
    public TMP_Text dexText;
    public TMP_Text magText;
    public TMP_Text lukText;

    [Header("Critical Info")]
    public TMP_Text critChanceText;   // 크리티컬 확률 (%)
    public TMP_Text critDamageText;   // 크리티컬 배수 (x배)

    void OnEnable()
    {
        if (!player) return;
        player.OnStatPointChanged += OnStatChanged;
        Refresh();
    }

    void OnDisable()
    {
        if (!player) return;
        player.OnStatPointChanged -= OnStatChanged;
    }

    void Start()
    {
        Refresh();
    }

    void OnStatChanged(int _)
    {
        Refresh();
    }

    void Refresh()
    {
        if (!player) return;

        Set(strText, $"STR {player.STR}");
        Set(dexText, $"DEX {player.DEX}");
        Set(magText, $"MAG {player.MAG}");
        Set(lukText, $"LUK {player.LUK}");

        Set(critChanceText, $"Crit {player.critChance * 100f:F1}%");
        Set(critDamageText, $"Crit Dmg {player.critMultiplier:F2}x");
    }

    void Set(TMP_Text t, string v)
    {
        if (t) t.text = v;
    }
}