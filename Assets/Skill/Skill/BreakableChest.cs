using UnityEngine;

public class BreakableChest : MonoBehaviour
{
    public int hp = 1;
    private StagePuzzleChestReward reward;

    void Awake()
    {
        reward = GetComponent<StagePuzzleChestReward>();
    }

    public void TakeDamage(int damage)
    {
        hp -= damage;
        Debug.Log("상자 데미지 받음, 현재 hp: " + hp);

        if (hp <= 0)
        {
            BreakChest();
        }
    }

    void BreakChest()
    {
        Debug.Log("상자 파괴됨");

        if (reward != null)
            reward.GiveReward();
        else
            Debug.LogWarning("StagePuzzleChestReward 없음");

        Destroy(gameObject);
    }
}