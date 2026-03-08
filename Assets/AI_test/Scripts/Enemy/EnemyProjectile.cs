using System.Transactions;
using UnityEngine;


// 원거리 공격 투사체 스크립트 파일
public class EnemyProjectile : MonoBehaviour
{
    public float speed = 20f;
    public float damage = 5f;
    public float lifeTime = 3f;

    void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    void Update()
    {
        transform.Translate(Vector3.forward * speed * Time.deltaTime);
    }


    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            IDamageable target = other.GetComponent<IDamageable>();
            if (target != null)
            {
                target.TakeDamage(damage);
                //Debug.Log("원거리 공격! " + damage + " 데미지!");
            }
            Destroy(gameObject);
        }
    }


}
