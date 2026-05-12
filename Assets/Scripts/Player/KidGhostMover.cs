using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Moves the kid ghost toward the player using NavMeshAgent.
/// The ghost's head always tracks the player regardless of model orientation.
/// Uses distance-based touch detection to deal damage.
/// </summary>
public class KidGhostMover : MonoBehaviour
{
    [Header("Navigation")]
    public float moveSpeed = 10.5f;
    public float acceleration = 20f;
    public float angularSpeed = 120f;

    /// <summary>
    /// Y-axis offset (degrees) applied after LookAt to align the model's head
    /// with the player direction.
    /// </summary>
    public float modelFacingYOffset = -90f;

    /// <summary>World-space distance at which the ghost is considered to be touching the player.</summary>
    public float touchDistance = 1.2f;

    private Transform _playerTransform;
    private PlayerHealthController _playerHealth;
    private Animator _animator;
    private NavMeshAgent _agent;
    private bool _isStunned;
    private float _stunTimer;
    private static readonly int IsChasingHash = Animator.StringToHash("IsChasing");

    private void Start()
    {
        HealthComponent health = GetComponent<HealthComponent>();
        if (health != null)
        {
            health.OnHealthChanged += (curr, max) => HandleDamage();
        }
    }

    private void HandleDamage()
    {
        // Stun for 0.2 seconds when hit
        _isStunned = true;
        _stunTimer = 0.2f;
        if (_agent != null && _agent.isOnNavMesh) _agent.isStopped = true;
        if (_animator != null) _animator.SetBool(IsChasingHash, false);
    }

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

        _agent = GetComponent<NavMeshAgent>();
        if (_agent == null) _agent = gameObject.AddComponent<NavMeshAgent>();

        // Setup agent properties
        _agent.speed = moveSpeed;
        _agent.acceleration = acceleration;
        _agent.angularSpeed = angularSpeed;
        _agent.stoppingDistance = 0.5f;
        _agent.autoBraking = false;
        _agent.updateRotation = false; // Disable auto-rotation to avoid conflict with manual rotation

        if (player != null)
        {
            _playerHealth = player.GetComponent<PlayerHealthController>();
            if (_playerHealth == null)
                _playerHealth = player.GetComponentInChildren<PlayerHealthController>();
        }

        // Snap to NavMesh
        if (_agent != null)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 5.0f, NavMesh.AllAreas))
            {
                _agent.Warp(hit.position);
            }
            else
            {
                Debug.LogWarning($"[KidGhostMover] {name} could not find NavMesh near {transform.position}. Ghost might not move.");
            }
        }
    }

    private void Update()
    {
        if (_isStunned)
        {
            _stunTimer -= Time.deltaTime;
            if (_stunTimer <= 0)
            {
                _isStunned = false;
                // Resume movement
                if (_agent != null && _agent.isOnNavMesh) _agent.isStopped = false;
            }
            return;
        }

        if (_playerTransform == null)
        {
            if (_animator != null) _animator.SetBool(IsChasingHash, false);
            if (_agent != null && _agent.isOnNavMesh) _agent.isStopped = true;
            return;
        }

        // Continuous Pathfinding
        if (_agent != null && _agent.isOnNavMesh)
        {
            // Only update destination if player moved a tiny bit to save performance
            // but always keep the agent moving
            _agent.SetDestination(_playerTransform.position);
            
            // Ensure agent is not stopped
            if (_agent.isStopped) _agent.isStopped = false;
        }

        Vector3 toPlayer = _playerTransform.position - transform.position;
        float distance = toPlayer.magnitude;

        // Visual Rotation (Head always tracks player direction)
        Vector3 lookDir = new Vector3(toPlayer.x, 0f, toPlayer.z);
        if (lookDir.sqrMagnitude > 0.001f)
        {
            Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized) * Quaternion.Euler(0f, modelFacingYOffset, 0f);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 10f * Time.deltaTime);
        }

        // Animator state
        if (_animator != null)
        {
            // Ghost is chasing if it has a path and is trying to reach the player
            // Even if velocity is low (e.g. turning), keep chase animation if distance > stopping distance
            bool isChasing = _agent != null && _agent.isOnNavMesh && !_agent.isStopped && (_agent.velocity.magnitude > 0.1f || distance > _agent.stoppingDistance + 0.1f);
            _animator.SetBool(IsChasingHash, isChasing);
        }

        // Damage detection
        if (distance <= touchDistance)
            _playerHealth?.TryTakeDamage();
    }
}
