using UnityEngine;

public class EnemyProjectile : MonoBehaviour
{
    public float speed = 20f;
    public float damage = 5f;
    public float lifeTime = 3f;

    private Rigidbody rb;
    private int tileLayerIndex;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        tileLayerIndex = LayerMask.NameToLayer("Tile");
    }
    void OnEnable()
    {

        rb.linearVelocity = transform.forward * speed;

        Invoke(nameof(Deactivate), lifeTime);
    }

    void OnDisable()
    {
        CancelInvoke(nameof(Deactivate));
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger) return;

        if (other.CompareTag("Player"))
        {
            IDamageable target = other.GetComponent<IDamageable>();
            target?.TakeDamage(damage);
            Deactivate(); 
        }
        else if (other.gameObject.layer == tileLayerIndex)
        {
            Deactivate();
        }
    }

    private void Deactivate()
    {
        gameObject.SetActive(false);
    }
}