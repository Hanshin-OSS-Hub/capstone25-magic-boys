using UnityEngine;
using TMPro;  // TextMeshPro 사용 시 필요

public class StatAllocationUI : MonoBehaviour
{
    [Header("References")]
    public PlayerStats player;          // PlayerStats 연결
    public TMP_Text statPointText;      // 남은 스탯 포인트 표시용 텍스트

    // ===============================
    // 버튼에서 호출할 함수들
    // ===============================
    public void AddSTR() => TryAdd(StatType.STR);
    public void AddDEX() => TryAdd(StatType.DEX);
    public void AddMAG() => TryAdd(StatType.MAG);
    public void AddLUK() => TryAdd(StatType.LUK);

    // ===============================
    // 내부 처리
    // ===============================
    void TryAdd(StatType type)
    {
        if (!player)
        {
            Debug.LogWarning("⚠️ PlayerStats가 연결되어 있지 않습니다!");
            return;
        }

        bool success = player.AllocateStat(type);

        if (success)
        {
            UpdatePointText(player.statPoints);
            Debug.Log($"✅ {type} 증가! (남은 포인트: {player.statPoints})");
        }
        else
        {
            Debug.Log("❌ 사용할 수 있는 스탯 포인트가 없습니다.");
        }
    }

    void Start()
    {
        if (player != null)
            UpdatePointText(player.statPoints);
    }

    void OnEnable()
    {
        if (player != null)
            player.OnStatPointChanged += UpdatePointText;
    }

    void OnDisable()
    {
        if (player != null)
            player.OnStatPointChanged -= UpdatePointText;
    }

    void UpdatePointText(int left)
    {
        if (statPointText)
            statPointText.text = $"Points: {left}";
    }
}