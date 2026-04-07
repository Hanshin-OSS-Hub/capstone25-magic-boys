using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public float speed = 20f;
    public float damage = 5f;
    public float lifeTime = 3f;

    private Rigidbody rb;
    private int tileLayerIndex;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;

        rb.linearVelocity = transform.forward * speed;

        tileLayerIndex = LayerMask.NameToLayer("Tile");

        Destroy(gameObject, lifeTime);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;

        if (other.CompareTag("Player"))
        {
            IDamageable target = other.GetComponent<IDamageable>();
            target?.TakeDamage(damage);
            Destroy(gameObject);
        }
        else if (other.gameObject.layer == tileLayerIndex)
        {
            Destroy(gameObject);
        }
    }
}