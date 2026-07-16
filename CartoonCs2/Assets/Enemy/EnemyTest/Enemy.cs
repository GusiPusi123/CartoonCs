using UnityEngine;

public class Enemy : MonoBehaviour
{
    public float health = 100f;

    public void TakeDamage(float damage)
    {
        health -= damage;
        Debug.Log("Enemy took damage, current health: " + health);
        if (health <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        // Можно добавить анимацию смерти или эффект
        Destroy(gameObject);
    }
}