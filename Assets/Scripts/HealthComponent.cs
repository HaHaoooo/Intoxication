using System;
using UnityEngine;

/// <summary>
/// General-purpose health container. Fires events on change and on death.
/// Attach alongside PlayerHealthController or GhostHealthController for behaviour.
/// </summary>
public class HealthComponent : MonoBehaviour
{
    [Header("Settings")]
    public float maxHealth = 100f;

    public float CurrentHealth { get; private set; }
    public bool IsAlive => CurrentHealth > 0f;

    /// <summary>Fired whenever health changes. Passes (currentHealth, maxHealth).</summary>
    public event Action<float, float> OnHealthChanged;

    /// <summary>Fired once when health reaches zero.</summary>
    public event Action OnDeath;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    private void OnEnable()
    {
        // Re-initialize if CurrentHealth was never set (e.g. added via editor tooling)
        if (CurrentHealth <= 0f)
            CurrentHealth = maxHealth;
    }

    /// <summary>Reduces health by amount. Fires OnDeath if health reaches zero.</summary>
    public void TakeDamage(float amount)
    {
        if (!IsAlive || amount <= 0f) return;

        CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0f)
            OnDeath?.Invoke();
    }

    /// <summary>Restores health by amount, capped to maxHealth.</summary>
    public void Heal(float amount)
    {
        if (!IsAlive || amount <= 0f) return;

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
}
