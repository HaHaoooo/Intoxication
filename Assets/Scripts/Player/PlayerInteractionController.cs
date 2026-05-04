using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Detects IInteractable objects via a center-screen SphereCast and handles
/// the F-key interaction trigger. Displays a prompt label at the bottom-center
/// of the screen when an interactable is in range.
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

    [Header("Prompt UI")]
    [Tooltip("Optional: assign an existing Text element. If left empty, one is created automatically.")]
    [SerializeField] private Text promptText;

    [SerializeField] private Color promptTextColor = new Color(1f, 0.92f, 0.55f, 1f);
    [SerializeField] private Color promptBgColor = new Color(0f, 0f, 0f, 0.55f);

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
    private GameObject _promptRoot;

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

        BuildPromptUI();
        SetPromptVisible(false);

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

        // During active dialogue, hide prompt and skip detection
        if (IsInDialogue)
        {
            SetPromptVisible(false);
            _currentTarget = null;
            return;
        }

        // Clear stale target if the underlying object was destroyed
        if (!IsTargetAlive())
        {
            _currentTarget = null;
            SetPromptVisible(false);
        }

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

    // -------------------------------------------------------------------------
    // Prompt UI helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Builds a fixed bottom-center prompt panel if no Text was assigned in the Inspector.
    /// </summary>
    private void BuildPromptUI()
    {
        if (promptText != null)
        {
            _promptRoot = promptText.transform.parent != null
                ? promptText.transform.parent.gameObject
                : promptText.gameObject;
            return;
        }

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[InteractionController] No Canvas found — prompt UI skipped.");
            return;
        }

        // Background pill, anchored to bottom-center
        _promptRoot = new GameObject("InteractionPrompt");
        _promptRoot.transform.SetParent(canvas.transform, false);

        RectTransform bgRect = _promptRoot.AddComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0f);
        bgRect.anchorMax = new Vector2(0.5f, 0f);
        bgRect.pivot = new Vector2(0.5f, 0f);
        bgRect.sizeDelta = new Vector2(220f, 44f);
        bgRect.anchoredPosition = new Vector2(0f, 60f);

        Image bg = _promptRoot.AddComponent<Image>();
        bg.color = promptBgColor;

        // Text child
        GameObject textGO = new GameObject("Label");
        textGO.transform.SetParent(_promptRoot.transform, false);

        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        promptText = textGO.AddComponent<Text>();
        promptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        promptText.fontSize = 22;
        promptText.fontStyle = FontStyle.Bold;
        promptText.alignment = TextAnchor.MiddleCenter;
        promptText.color = promptTextColor;
        promptText.raycastTarget = false;
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
