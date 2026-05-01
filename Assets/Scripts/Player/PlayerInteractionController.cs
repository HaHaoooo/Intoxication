using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Detects IInteractable objects via a center-screen raycast and handles
/// the F-key interaction trigger. Displays a prompt when an interactable
/// is in range and manages the dialogue-active state that locks other systems.
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

    [Tooltip("Max number of colliders checked per raycast (RaycastAll buffer size).")]
    [SerializeField] private int maxRaycastHits = 16;

    [Header("UI")]
    [Tooltip("Text element for the interaction prompt. Auto-created if null.")]
    [SerializeField] private Text promptText;

    [SerializeField] private Color promptColor = new Color(1f, 0.9f, 0.55f, 1f);
    [SerializeField] private Color promptBgColor = new Color(0f, 0f, 0f, 0.55f);

    [Header("Debug")]
    [SerializeField] private bool showDebugRay = true;

    /// <summary>True while a dialogue is actively playing; blocks other input systems.</summary>
    public bool IsInDialogue => _dialogueUI != null && _dialogueUI.IsActive;

    /// <summary>True when aiming at a valid interactable (prompt is showing).</summary>
    public bool IsLookingAtInteractable => IsTargetAlive();

    /// <summary>True on the frame the F key was consumed by this controller.</summary>
    public bool ConsumedInputThisFrame { get; private set; }

    private Camera _mainCam;
    private IInteractable _currentTarget;
    private DialogueUI _dialogueUI;
    private GameObject _promptRoot;
    private RectTransform _promptRect;
    private RaycastHit[] _hitBuffer;
    private Canvas _cachedCanvas;

    /// <summary>Checks whether the current target still exists. Interface references bypass Unity's
    /// fake-null operator, so we cast to Object to detect destroyed MonoBehaviours.</summary>
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

        EnsurePromptUI();
        SetPromptVisible(false);

        if (_mainCam == null)
            Debug.LogWarning("[InteractionController] Camera.main is null at Start — will retry in Update.");
        if (_dialogueUI == null)
            Debug.LogWarning("[InteractionController] DialogueUI.Instance is null at Start.");
    }

    private void Update()
    {
        ConsumedInputThisFrame = false;

        // Lazy-init camera if it wasn't available at Start
        if (_mainCam == null)
        {
            _mainCam = Camera.main;
            if (_mainCam == null) return;
        }

        // Lazy-init dialogue UI
        if (_dialogueUI == null)
        {
            _dialogueUI = DialogueUI.Instance;
        }

        // During active dialogue, hide the interaction prompt and skip detection
        if (IsInDialogue)
        {
            SetPromptVisible(false);
            _currentTarget = null;
            return;
        }

        // Clear stale target if the underlying object was destroyed externally.
        if (!IsTargetAlive())
        {
            _currentTarget = null;
            SetPromptVisible(false);
        }

        DetectInteractable();
        HandleInput();
        UpdatePromptPosition();
    }

    /// <summary>
    /// Positions the interaction prompt near the target in screen space (floating UI).
    /// </summary>
    private void UpdatePromptPosition()
    {
        if (_currentTarget == null || _promptRect == null || !IsTargetAlive()) return;

        // Get the visual center or collider center
        Vector3 targetPos = Vector3.zero;
        if (_currentTarget is MonoBehaviour mb)
        {
            targetPos = mb.transform.position;
            // If it has a renderer, use its bounds for better vertical positioning
            Renderer r = mb.GetComponentInChildren<Renderer>();
            if (r != null) targetPos = r.bounds.center + Vector3.up * 0.5f; // Position slightly above center
        }

        if (targetPos == Vector3.zero) return;

        Vector3 screenPos = _mainCam.WorldToScreenPoint(targetPos);
        
        // If target is behind camera, hide prompt
        if (screenPos.z < 0)
        {
            SetPromptVisible(false);
            return;
        }

        if (_cachedCanvas == null)
            _cachedCanvas = _promptRoot.GetComponentInParent<Canvas>();

        if (_cachedCanvas == null) return;

        RectTransform canvasRect = _cachedCanvas.transform as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect,
            new Vector2(screenPos.x, screenPos.y),
            _cachedCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _mainCam,
            out Vector2 localPoint
        );

        _promptRect.anchoredPosition = localPoint;
    }

    /// <summary>
    /// Uses SphereCast to find the closest IInteractable within range.
    /// SphereCast is more forgiving than a single ray for small NPC colliders.
    /// </summary>
    private void DetectInteractable()
    {
        Ray ray = _mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));

        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * interactionDistance, Color.cyan);
        }

        // Use SphereCast instead of Raycast for easier interaction with small colliders
        float radius = 0.5f; // Increased radius for easier targeting
        int hitCount = Physics.SphereCastNonAlloc(ray, radius, _hitBuffer, interactionDistance, interactableLayerMask);

        IInteractable bestTarget = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _hitBuffer[i];
            float distance = hit.distance;

            // GetComponentInParent is expensive, try TryGetComponent on collider first
            if (!hit.collider.TryGetComponent<IInteractable>(out var interactable))
            {
                interactable = hit.collider.GetComponentInParent<IInteractable>();
            }

            if (interactable != null && interactable.CanInteract() && distance < bestDistance)
            {
                bestTarget = interactable;
                bestDistance = distance;
            }
        }

        if (bestTarget != null)
        {
            _currentTarget = bestTarget;
            SetPromptText(_currentTarget.GetInteractionPrompt());
            SetPromptVisible(true);
        }
        else if (_currentTarget != null)
        {
            _currentTarget = null;
            SetPromptVisible(false);
        }
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
            SetPromptVisible(false);
            _currentTarget = null;
        }
    }

    /// <summary>Auto-creates the interaction prompt UI if not assigned.</summary>
    private void EnsurePromptUI()
    {
        if (promptText != null)
        {
            _promptRoot = promptText.transform.parent != null
                ? promptText.transform.parent.gameObject
                : promptText.gameObject;
            _promptRect = _promptRoot.GetComponent<RectTransform>();
            return;
        }

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[InteractionController] No Canvas found — cannot create prompt UI.");
            return;
        }

        // Background panel
        _promptRoot = new GameObject("InteractionPrompt");
        _promptRoot.transform.SetParent(canvas.transform, false);

        _promptRect = _promptRoot.AddComponent<RectTransform>();
        _promptRect.anchorMin = new Vector2(0.5f, 0.5f);
        _promptRect.anchorMax = new Vector2(0.5f, 0.5f);
        _promptRect.pivot = new Vector2(0.5f, 0.5f);
        _promptRect.sizeDelta = new Vector2(200f, 40f);
        _promptRect.anchoredPosition = new Vector2(0f, -80f);

        Image bg = _promptRoot.AddComponent<Image>();
        bg.color = promptBgColor;

        // Text child
        GameObject textGO = new GameObject("PromptText");
        textGO.transform.SetParent(_promptRoot.transform, false);

        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        promptText = textGO.AddComponent<Text>();
        promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        promptText.fontSize = 20;
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.color = promptColor;
        promptText.raycastTarget = false;

        _promptRoot.SetActive(false);
    }

    private void SetPromptText(string text)
    {
        if (promptText != null)
            promptText.text = text;
    }

    private void SetPromptVisible(bool visible)
    {
        if (_promptRoot != null && _promptRoot.activeSelf != visible)
            _promptRoot.SetActive(visible);
    }
}
