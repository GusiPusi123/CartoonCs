using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Projectile : MonoBehaviour
{
    [SerializeField] private int damage = 10; // урон настраивается прямо на префабе пули
    [SerializeField] private float lifeTime = 5f;
    [SerializeField] private GameObject hitEffect;
    [SerializeField] private bool debugLogs = true;

    private void Start()
    {
        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        HandleHit(other);
    }

    private void OnCollisionEnter(Collision collision)
    {
        HandleHit(collision.collider);
    }

    private void HandleHit(Collider other)
    {
        if (debugLogs)
            Debug.Log($"[Projectile] Попал в: {other.name} (тег: {other.tag})");

        if (other.CompareTag("Player"))
        {
            PlayerHealth playerHealth = other.GetComponent<PlayerHealth>();
            if (playerHealth != null)
            {
                playerHealth.TakeDamage(damage);
                if (debugLogs) Debug.Log($"[Projectile] Нанесён урон игроку: {damage}");
            }
            else if (debugLogs)
            {
                Debug.LogWarning("[Projectile] На игроке нет компонента PlayerHealth!");
            }
        }

        if (other.CompareTag("Enemy")) return;

        if (hitEffect != null)
        {
            Instantiate(hitEffect, transform.position, Quaternion.identity);
        }

        Destroy(gameObject);
    }
}