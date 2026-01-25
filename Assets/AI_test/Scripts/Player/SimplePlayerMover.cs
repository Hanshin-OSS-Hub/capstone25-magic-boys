using UnityEngine;

public class SimplePlayerMover : MonoBehaviour, IDamageable
{
     public float moveSpeed = 5f;
    public float maxHP = 100f;
    public float currentHP;
    public int currentExp = 0;

    void Start()
    {
        currentHP = maxHP;
    }


    void Update()
    {
 
         float horizontal = Input.GetAxis("Horizontal"); // A, D
        float vertical = Input.GetAxis("Vertical");     // W, S

       
         Vector3 moveDirection = new Vector3(horizontal, 0, vertical);


         transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.Self);

        if (Input.GetKeyDown(KeyCode.F))
        {
            
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100f))
            {
                IDamageable target = hit.collider.GetComponent<IDamageable>();
                if (target != null)
                {
                    target.TakeDamage(4);
                    Debug.Log("공격! " + hit.collider.name + "에게 데미지");
                }
            }
        }
    }


    public void TakeDamage(float damage)
    {
        currentHP -= damage;
        Debug.Log($"남은 HP: {currentHP}");
        if (currentHP <= 0)
        {
            Debug.Log("사망");
        }
    }

    public void AddExp(int amount)
    {
        currentExp += amount;
        Debug.Log($"경험치 획득! +{amount} 현재 경험치: {currentExp})");

    }

}