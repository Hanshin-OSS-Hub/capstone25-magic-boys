using UnityEngine;

public class BreakableChest : MonoBehaviour
{
    public int hp = 3;

    [Header("Optional VFX")]
    public GameObject breakVfx;

    bool broken;
    StagePuzzleChestReward reward;

    void Awake()
    {
        reward = GetComponent<StagePuzzleChestReward>();
    }

    public void TakeDamage(int dmg)
    {
        if (broken) return;
        if (dmg <= 0) return;

        hp -= dmg;
        if (hp <= 0)
        {
            broken = true;

            if (breakVfx)
                Instantiate(breakVfx, transform.position, Quaternion.identity);

            reward?.GiveReward();

            Destroy(gameObject);
        }
    }
}
