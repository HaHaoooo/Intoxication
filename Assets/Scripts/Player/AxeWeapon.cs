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
    private const float DamagePerSwing = 10f;

    [Header("Settings")]
    [Tooltip("Maximum distance from the player at which a swing can hit the ghost.")]
    public float swingRange = 2.5f;

    [Tooltip("Minimum seconds between swings (prevents damage spam on held click).")]
    public float swingCooldown = 0.6f;

    [Header("Visual Feedback")]
    [Tooltip("Procedural swing rotation offset (euler).")]
    public Vector3 swingRotationOffset = new Vector3(30f, -40f, 20f);
    public float swingDuration = 0.15f;
    public float returnDuration = 0.3f;

    private float _lastSwingTime = -999f;
    private bool _isEnabled = false;
    private Quaternion _originalLocalRot;
    private Vector3 _originalLocalPos;
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
            _originalLocalPos = axe.transform.localPosition;
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

        // Use new input system polling for consistency or the Attack action if preferred
        bool attackPressed = false;
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            attackPressed = true;
        
        if (attackPressed && !_isSwinging)
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
            return;
        }

        _lastSwingTime = Time.time;
        StartCoroutine(SwingRoutine());

        // Find the active ghost by its HealthComponent + GhostHealthController
        GhostHealthController ghost = FindNearestGhost();
        if (ghost == null) return;

        float distance = Vector3.Distance(transform.position, ghost.transform.position);
        if (distance > swingRange) return;

        HealthComponent ghostHealth = ghost.GetComponent<HealthComponent>();
        if (ghostHealth == null || !ghostHealth.IsAlive) return;

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
        Vector3 targetPos = _originalLocalPos + new Vector3(0.1f, -0.1f, 0.2f); // Slight thrust forward

        // Swing forward (fast)
        float elapsed = 0f;
        while (elapsed < swingDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / swingDuration;
            // Use an easing curve for "snap"
            float easedT = t * t * (3f - 2f * t); 
            axeTransform.localRotation = Quaternion.Slerp(_originalLocalRot, targetRot, easedT);
            axeTransform.localPosition = Vector3.Lerp(_originalLocalPos, targetPos, easedT);
            yield return null;
        }

        // Return back (slower)
        elapsed = 0f;
        while (elapsed < returnDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / returnDuration;
            float easedT = t * t * (3f - 2f * t);
            axeTransform.localRotation = Quaternion.Slerp(targetRot, _originalLocalRot, easedT);
            axeTransform.localPosition = Vector3.Lerp(targetPos, _originalLocalPos, easedT);
            yield return null;
        }

        axeTransform.localRotation = _originalLocalRot;
        axeTransform.localPosition = _originalLocalPos;
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
