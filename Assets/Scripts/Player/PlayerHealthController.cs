using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Handles player-specific health behaviour:
/// — Damage intake with a 1-second invincibility window after each hit.
/// — Death sequence: restarts the scene after a short delay.
/// Requires a HealthComponent on the same GameObject.
/// </summary>
[RequireComponent(typeof(HealthComponent))]
public class PlayerHealthController : MonoBehaviour
{
    private const float RestartDelay = 2.5f;

    [Header("Settings")]
    [Tooltip("Damage dealt to the player per ghost touch.")]
    public float damagePerHit = 20f;

    [Tooltip("Seconds the player is invincible after being hit.")]
    public float invincibilityDuration = 1f;

    private HealthComponent _health;
    private bool _isInvincible;

    private void Awake()
    {
        _health = GetComponent<HealthComponent>();
    }

    private void Start()
    {
        _health.OnDeath += HandleDeath;
    }

    /// <summary>
    /// Attempts to deal one hit of damage to the player.
    /// Returns false and does nothing if invincibility is active.
    /// </summary>
    public bool TryTakeDamage()
    {
        if (_isInvincible || !_health.IsAlive) return false;

        _health.TakeDamage(damagePerHit);

        if (_health.IsAlive)
            StartCoroutine(InvincibilityWindow());

        return true;
    }

    private IEnumerator InvincibilityWindow()
    {
        _isInvincible = true;
        yield return new WaitForSeconds(invincibilityDuration);
        _isInvincible = false;
    }

    private void HandleDeath()
    {
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        Debug.Log("[PlayerHealth] Player died. Restarting scene...");
        yield return new WaitForSeconds(RestartDelay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
