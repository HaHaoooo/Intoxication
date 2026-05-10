using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles axe swinging. Enabled by AxePickup when the player picks up the axe.
/// On left mouse click, checks whether the active ghost is within swing range
/// and deals 50 damage to its HealthComponent.
/// Attach to the Player GameObject.
/// </summary>
public class AxeWeapon : MonoBehaviour
{
    private const float DamagePerSwing = 50f;

    [Header("Settings")]
    [Tooltip("Maximum distance from the player at which a swing can hit the ghost.")]
    public float swingRange = 2.5f;

    [Tooltip("Minimum seconds between swings (prevents damage spam on held click).")]
    public float swingCooldown = 0.6f;

    [Header("Visual Feedback")]
    [Tooltip("Procedural swing rotation offset (euler).")]
    public Vector3 swingRotationOffset = new Vector3(45f, 0f, 0f);
    public float swingDuration = 0.2f;

    private float _lastSwingTime = -999f;
    private bool _isEnabled = false;
    private Quaternion _originalLocalRot;
    private bool _isSwinging = false;

    /// <summary>Called by AxePickup to activate the weapon.</summary>
    public void Enable()
    {
        _isEnabled = true;
        enabled = true;
        
        // Find the axe object attached to the hand to get its original rotation
        AxePickup axe = GetComponentInChildren<AxePickup>();
        if (axe != null)
        {
            _originalLocalRot = axe.transform.localRotation;
        }
    }

    private void Awake()
    {
        // Stays dormant until Enable() is called
        enabled = false;
    }

    private void Update()
    {
        if (!_isEnabled) return;
        if (Mouse.current == null) return;

        if (Mouse.current.leftButton.wasPressedThisFrame && !_isSwinging)
        {
            TrySwing();
        }
    }

    /// <summary>
    /// Attempts to deal damage to the nearest ghost within swing range.
    /// Respects the cooldown between swings.
    /// </summary>
    private void TrySwing()
    {
        if (Time.time - _lastSwingTime < swingCooldown)
        {
            Debug.Log("[AxeWeapon] Swing on cooldown.");
            return;
        }

        _lastSwingTime = Time.time;
        StartCoroutine(SwingRoutine());

        // Find the active ghost by its HealthComponent + GhostHealthController
        GhostHealthController ghost = FindNearestGhost();
        if (ghost == null)
        {
            Debug.Log("[AxeWeapon] Swung but no ghost in range.");
            return;
        }

        float distance = Vector3.Distance(transform.position, ghost.transform.position);
        if (distance > swingRange)
        {
            Debug.Log($"[AxeWeapon] Swung — ghost too far ({distance:F2} > {swingRange}).");
            return;
        }

        HealthComponent ghostHealth = ghost.GetComponent<HealthComponent>();
        if (ghostHealth == null || !ghostHealth.IsAlive)
        {
            Debug.Log("[AxeWeapon] Ghost has no health or is already dead.");
            return;
        }

        ghostHealth.TakeDamage(DamagePerSwing);
        Debug.Log($"[AxeWeapon] Hit ghost for {DamagePerSwing} damage. Ghost HP: {ghostHealth.CurrentHealth}");
    }

    private System.Collections.IEnumerator SwingRoutine()
    {
        _isSwinging = true;
        AxePickup axe = GetComponentInChildren<AxePickup>();
        if (axe == null)
        {
            _isSwinging = false;
            yield break;
        }

        Transform axeTransform = axe.transform;
        Quaternion targetRot = _originalLocalRot * Quaternion.Euler(swingRotationOffset);

        // Swing forward
        float elapsed = 0f;
        while (elapsed < swingDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / swingDuration;
            axeTransform.localRotation = Quaternion.Slerp(_originalLocalRot, targetRot, t);
            yield return null;
        }

        // Return back
        elapsed = 0f;
        while (elapsed < swingDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / swingDuration;
            axeTransform.localRotation = Quaternion.Slerp(targetRot, _originalLocalRot, t);
            yield return null;
        }

        axeTransform.localRotation = _originalLocalRot;
        _isSwinging = false;
    }

    /// <summary>Returns the nearest living GhostHealthController in the scene, or null if none.</summary>
    private GhostHealthController FindNearestGhost()
    {
        GhostHealthController[] ghosts = FindObjectsByType<GhostHealthController>(FindObjectsSortMode.None);
        GhostHealthController nearest = null;
        float nearestDist = float.MaxValue;

        foreach (GhostHealthController g in ghosts)
        {
            if (g == null) continue;
            HealthComponent hp = g.GetComponent<HealthComponent>();
            if (hp == null || !hp.IsAlive) continue;

            float dist = Vector3.Distance(transform.position, g.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = g;
            }
        }

        return nearest;
    }
}
