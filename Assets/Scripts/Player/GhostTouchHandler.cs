using UnityEngine;

/// <summary>
/// Detects when the kid ghost's trigger collider overlaps the player and
/// delegates damage to PlayerHealthController (which handles invincibility and death).
/// Requires a Collider with isTrigger = true on the same GameObject.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GhostTouchHandler : MonoBehaviour
{
    private const string PlayerTag = "Player";

    private void Start()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(PlayerTag)) return;

        PlayerHealthController playerHealth = other.GetComponent<PlayerHealthController>();
        if (playerHealth == null)
            playerHealth = other.GetComponentInParent<PlayerHealthController>();

        playerHealth?.TryTakeDamage();
    }
}
