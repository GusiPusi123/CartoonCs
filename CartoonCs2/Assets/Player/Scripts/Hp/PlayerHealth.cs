using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    private int currentHealth;

    [Header("UI")]
    [SerializeField] private Slider healthSlider;

    [Header("Эффекты")]
    [Tooltip("Система частиц, которая проигрывается при получении урона (например, прикреплённая к полоске здоровья)")]
    [SerializeField] private ParticleSystem damageParticles;

    private void Awake()
    {
        currentHealth = maxHealth;
        UpdateSlider();
    }

    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        currentHealth = Mathf.Max(currentHealth, 0);
        UpdateSlider();
        PlayDamageParticles();

        if (currentHealth <= 0)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }

    /// <summary>Восстанавливает здоровье на указанное количество, не превышая maxHealth.</summary>
    public void Heal(int healAmount)
    {
        if (healAmount <= 0) return;

        currentHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
        UpdateSlider();
    }

    /// <summary>Полностью восстанавливает здоровье до максимума.</summary>
    public void HealToFull()
    {
        currentHealth = maxHealth;
        UpdateSlider();
    }

    public int CurrentHealth => currentHealth;
    public int MaxHealth => maxHealth;

    private void PlayDamageParticles()
    {
        if (damageParticles == null) return;

        // Play() с true перезапускает систему с нуля — так частицы будут
        // выбрасываться каждый раз заново, даже если предыдущая порция ещё не долетела.
        damageParticles.Play(true);
    }

    private void UpdateSlider()
    {
        if (healthSlider == null) return;

        healthSlider.maxValue = maxHealth;
        healthSlider.value = currentHealth;
    }
}
