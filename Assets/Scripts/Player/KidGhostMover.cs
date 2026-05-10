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
    private Animator _animator;
    private MonsterProceduralAnimator _monsterAnimator;
    private static readonly int IsChasingHash = Animator.StringToHash("IsChasing");

    /// <summary>
    /// Initializes the ghost mover with a target.
    /// </summary>
    /// <param name="player">The player's transform to chase.</param>
    /// <param name="speed">Desired movement speed.</param>
    public void Initialize(Transform player, float speed)
    {
        _playerTransform = player;
        moveSpeed = speed;
        
        _animator = GetComponent<Animator>();
        if (_animator == null) _animator = GetComponentInChildren<Animator>();

        _monsterAnimator = GetComponent<MonsterProceduralAnimator>();
        if (_monsterAnimator == null) _monsterAnimator = GetComponentInChildren<MonsterProceduralAnimator>();

        if (player != null)
        {
            _playerHealth = player.GetComponent<PlayerHealthController>();
            if (_playerHealth == null)
                _playerHealth = player.GetComponentInChildren<PlayerHealthController>();
        }
    }

    private void Update()
    {
        if (_playerTransform == null)
        {
            if (_animator != null) _animator.SetBool(IsChasingHash, false);
            if (_monsterAnimator != null) _monsterAnimator.UpdateSpeed(0f);
            return;
        }

        Vector3 playerPos = _playerTransform.position;
        Vector3 myPos = transform.position;

        if (!float.IsFinite(playerPos.x) || !float.IsFinite(playerPos.y) || !float.IsFinite(playerPos.z) ||
            !float.IsFinite(myPos.x) || !float.IsFinite(myPos.y) || !float.IsFinite(myPos.z))
        {
            if (_animator != null) _animator.SetBool(IsChasingHash, false);
            if (_monsterAnimator != null) _monsterAnimator.UpdateSpeed(0f);
            return;
        }

        Vector3 toPlayer = playerPos - myPos;
        float distance = toPlayer.magnitude;

        // Move toward the player
        bool isMoving = false;
        float dt = Time.deltaTime;
        if (distance > 0.0001f && float.IsFinite(dt) && dt > 0)
        {
            float step = moveSpeed * dt;
            if (float.IsFinite(step) && step > 0)
            {
                // Rotation
                if (toPlayer != Vector3.zero)
                {
                    Quaternion targetRot = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 5f * dt);
                }

                transform.position += toPlayer.normalized * step;
                isMoving = true;
            }
        }

        if (_animator != null)
        {
            _animator.SetBool(IsChasingHash, isMoving);
        }

        if (_monsterAnimator != null)
        {
            _monsterAnimator.UpdateSpeed(isMoving ? moveSpeed : 0f);
        }

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

        /// <summary>Pushes the ghost away from the source position.</summary>
        public void ApplyKnockback(Vector3 sourcePos, float force)
        {
        Vector3 direction = (transform.position - sourcePos).normalized;
        direction.y = 0; // Keep on ground
        transform.position += direction * force;
        }
        }
