using UnityEngine;

/// <summary>
/// Handles ghost-specific health behaviour: destroys the ghost GameObject on death.
/// Requires a HealthComponent on the same GameObject.
/// </summary>
[RequireComponent(typeof(HealthComponent))]
public class GhostHealthController : MonoBehaviour
{
    private HealthComponent _health;

    private void Awake()
    {
        _health = GetComponent<HealthComponent>();
    }

    private void Start()
    {
        _health.OnDeath += HandleDeath;
    }

    private void HandleDeath()
    {
        Destroy(gameObject);
    }
}
