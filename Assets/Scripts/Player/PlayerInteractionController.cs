using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Detects IInteractable objects via a center-screen raycast and handles
/// the F-key interaction trigger. Manages the dialogue-active state that locks other systems.
/// Runs before PlayerMovement to ensure F-key priority.
/// </summary>
[DefaultExecutionOrder(-100)]
public class PlayerInteractionController : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("Maximum distance to detect interactable objects.")]
    [SerializeField] private float interactionDistance = 6f;

    [Tooltip("Layers to include when raycasting for interactables.")]
    [SerializeField] private LayerMask interactableLayerMask = ~0;

    [Tooltip("Max number of colliders checked per raycast (SphereCast buffer size).")]
    [SerializeField] private int maxRaycastHits = 16;

    [Header("Detection Tuning")]
    [Tooltip("Radius of the SphereCast used for interaction detection.")]
    [SerializeField] private float detectionRadius = 0.5f;

    [Header("Debug")]
    [SerializeField] private bool showDebugRay = true;

    /// <summary>True while a dialogue is actively playing; blocks other input systems.</summary>
    public bool IsInDialogue => _dialogueUI != null && _dialogueUI.IsActive;

    /// <summary>True when aiming at a valid interactable.</summary>
    public bool IsLookingAtInteractable => IsTargetAlive();

    /// <summary>True on the frame the F key was consumed by this controller.</summary>
    public bool ConsumedInputThisFrame { get; private set; }

    private Camera _mainCam;
    private IInteractable _currentTarget;
    private DialogueUI _dialogueUI;
    private RaycastHit[] _hitBuffer;

    /// <summary>Checks whether the current target still exists.</summary>
    private bool IsTargetAlive()
    {
        if (_currentTarget == null) return false;
        return !(_currentTarget is Object unityObj) || unityObj != null;
    }

    private void Start()
    {
        _mainCam = Camera.main;
        _dialogueUI = DialogueUI.Instance;
        _hitBuffer = new RaycastHit[maxRaycastHits];

        if (_mainCam == null)
            Debug.LogWarning("[InteractionController] Camera.main is null at Start — will retry in Update.");
        if (_dialogueUI == null)
            Debug.LogWarning("[InteractionController] DialogueUI.Instance is null at Start.");
    }

    private void Update()
    {
        ConsumedInputThisFrame = false;

        if (_mainCam == null)
        {
            _mainCam = Camera.main;
            if (_mainCam == null) return;
        }

        if (_dialogueUI == null)
            _dialogueUI = DialogueUI.Instance;

        // During active dialogue, skip detection
        if (IsInDialogue)
        {
            _currentTarget = null;
            return;
        }

        // Clear stale target if the underlying object was destroyed
        if (!IsTargetAlive())
            _currentTarget = null;

        DetectInteractable();
        HandleInput();
    }

    /// <summary>
    /// Uses SphereCast to find the closest IInteractable within range.
    /// </summary>
    private void DetectInteractable()
    {
        Ray ray = _mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (showDebugRay)
            Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.cyan);

        int hitCount = Physics.SphereCastNonAlloc(ray, detectionRadius, _hitBuffer, interactionDistance, interactableLayerMask);

        IInteractable bestTarget = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _hitBuffer[i];
            float distance = hit.distance;

            if (!hit.collider.TryGetComponent<IInteractable>(out var interactable))
                interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null && interactable.CanInteract() && distance < bestDistance)
            {
                bestTarget = interactable;
                bestDistance = distance;
            }
        }

        _currentTarget = bestTarget;
    }

    /// <summary>Checks for F-key press to trigger the current interactable.</summary>
    private void HandleInput()
    {
        if (_currentTarget == null) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current.fKey.wasPressedThisFrame)
        {
            ConsumedInputThisFrame = true;
            Debug.Log("[InteractionController] F key pressed — triggering interaction.");
            _currentTarget.OnInteract();
            _currentTarget = null;
        }
    }
}
