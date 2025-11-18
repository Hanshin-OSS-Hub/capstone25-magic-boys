using UnityEngine;

public class SimplePlayerMover : MonoBehaviour, IDamageable
{
    public float moveSpeed = 5f;
    public float maxHP = 100f;
    public float currentHP;

    void Start()
    {
        currentHP = maxHP;
    }


    void Update()
    {
        // 1. 키보드 입력 받기 (W,A,S,D)
        float horizontal = Input.GetAxis("Horizontal"); // A, D
        float vertical = Input.GetAxis("Vertical");     // W, S

        // 2. 이동 방향 계산 (플레이어의 "로컬" 방향 기준)
        Vector3 moveDirection = new Vector3(horizontal, 0, vertical);

        // 3. 실제 이동 (Space.Self를 사용해야 '내가 보는 방향'으로 이동)
        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.Self);

        if (Input.GetKeyDown(KeyCode.F))
        {
            // 카메라 시야의 정중앙(0.5, 0.5)으로 광선을 생성
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100f))
            {
                IDamageable target = hit.collider.GetComponent<IDamageable>();
                if (target != null)
                {
                    target.TakeDamage(4);
                    Debug.Log("플레이어 공격! " + hit.collider.name + "에게 4 데미지!");
                }
            }
        }
    }


    public void TakeDamage(float damage)
    {
        currentHP -= damage;
        Debug.Log($"플레이어 피격, 남은 HP: {currentHP}");
        if (currentHP <= 0)
        {
            Debug.Log("플레이어 사망");
        }
    }



}