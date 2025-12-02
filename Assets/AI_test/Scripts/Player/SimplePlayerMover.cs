using UnityEngine;

public class SimplePlayerMover : MonoBehaviour, IDamageable
{
    // public float moveSpeed = 5f;
    public float maxHP = 100f;
    public float currentHP;
    public int currentExp = 0;

    void Start()
    {
        currentHP = maxHP;
    }


    void Update()
    {
        // // 1. Ű���� �Է� �ޱ� (W,A,S,D)
        // float horizontal = Input.GetAxis("Horizontal"); // A, D
        // float vertical = Input.GetAxis("Vertical");     // W, S

        // // 2. �̵� ���� ��� (�÷��̾��� "����" ���� ����)
        // Vector3 moveDirection = new Vector3(horizontal, 0, vertical);

        // // 3. ���� �̵� (Space.Self�� ����ؾ� '���� ���� ����'���� �̵�)
        // transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.Self);

        if (Input.GetKeyDown(KeyCode.F))
        {
            // ī�޶� �þ��� ���߾�(0.5, 0.5)���� ������ ����
            Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 100f))
            {
                IDamageable target = hit.collider.GetComponent<IDamageable>();
                if (target != null)
                {
                    target.TakeDamage(4);
                    Debug.Log("�÷��̾� ����! " + hit.collider.name + "���� 4 ������!");
                }
            }
        }
    }


    public void TakeDamage(float damage)
    {
        currentHP -= damage;
        Debug.Log($"�÷��̾� �ǰ�, ���� HP: {currentHP}");
        if (currentHP <= 0)
        {
            Debug.Log("�÷��̾� ���");
        }
    }

    public void AddExp(int amount)
    {
        currentExp += amount;
        Debug.Log($"����ġ ȹ��! +{amount} (���� �� ����ġ: {currentExp})");

    }

}