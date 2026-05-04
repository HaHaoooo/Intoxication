using UnityEngine;

/// <summary>
/// Moves the kid ghost toward the player at a constant speed.
/// The ghost's head always tracks the player regardless of model orientation.
/// Uses distance-based touch detection to deal damage.
/// </summary>
public class KidGhostMover : MonoBehaviour
{
    /// <summary>Movement speed in units per second.</summary>
    public float moveSpeed = 10.5f;

    /// <summary>
    /// Y-axis offset (degrees) applied after LookAt to align the model's head
    /// with the player direction. Tune in the Inspector if the head faces the wrong way.
    /// </summary>
    public float modelFacingYOffset = 0f;

    /// <summary>World-space distance at which the ghost is considered to be touching the player.</summary>
    public float touchDistance = 1.2f;

    private Transform _playerTransform;
    private PlayerHealthController _playerHealth;

    /// <summary>
    /// Initializes the ghost mover with a target.
    /// </summary>
    /// <param name="player">The player's transform to chase.</param>
    /// <param name="speed">Desired movement speed.</param>
    public void Initialize(Transform player, float speed)
    {
        _playerTransform = player;
        moveSpeed = speed;

        if (player != null)
        {
            _playerHealth = player.GetComponent<PlayerHealthController>();
            if (_playerHealth == null)
                _playerHealth = player.GetComponentInChildren<PlayerHealthController>();
        }
    }

    private void Update()
    {
        if (_playerTransform == null) return;

        Vector3 toPlayer = _playerTransform.position - transform.position;
        float distance = toPlayer.magnitude;

        // Move toward the player
        if (distance > 0.0001f)
            transform.position += toPlayer.normalized * (moveSpeed * Time.deltaTime);

        // Head always tracks the player on the horizontal plane
        Vector3 flatDirection = new Vector3(toPlayer.x, 0f, toPlayer.z);
        if (flatDirection.sqrMagnitude > 0.001f)
        {
            Quaternion lookRotation = Quaternion.LookRotation(flatDirection.normalized);
            transform.rotation = lookRotation * Quaternion.Euler(0f, modelFacingYOffset, 0f);
        }

        // Distance-based touch — PlayerHealthController handles the invincibility window
        if (distance <= touchDistance)
            _playerHealth?.TryTakeDamage();
    }
}
