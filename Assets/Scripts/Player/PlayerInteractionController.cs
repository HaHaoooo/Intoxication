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

    [Header("UI Reference")]
    [Tooltip("Optional: assign an existing Text element. If left empty, one is created automatically.")]
    [SerializeField] private Text promptText;

    [Header("Visual Styling")]
[SerializeField] private Color promptTextColor = new Color(1f, 0.92f, 0.55f, 1f);
    [SerializeField] private Color promptBgColor = new Color(0f, 0f, 0f, 0.55f);

    public bool IsInDialogue => _dialogueUI != null && _dialogueUI.IsActive;
    public bool IsLookingAtInteractable => _currentTarget != null;
    public bool ConsumedInputThisFrame { get; private set; }

    private Camera _mainCam;
    private IInteractable _currentTarget;
    private DialogueUI _dialogueUI;
    private PlayerPickupController _pickupController;
    private RaycastHit[] _hitBuffer;
    private GameObject _promptRoot;

    private void Start()
    {
        _mainCam = Camera.main;
        _dialogueUI = DialogueUI.Instance;
        _pickupController = GetComponent<PlayerPickupController>();
        _hitBuffer = new RaycastHit[maxRaycastHits];

        BuildPromptUI();
        SetPromptVisible(false);
    }

    private void Update()
    {
        ConsumedInputThisFrame = false;

        if (_mainCam == null)
        {
            _mainCam = Camera.main;
            if (_mainCam == null) return;
        }

        if (IsInDialogue)
        {
            SetPromptVisible(false);
            _currentTarget = null;
            return;
        }

        DetectInteractable();
        HandleInput();
    }

    private void DetectInteractable()
    {
        Ray ray = _mainCam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        int hitCount = Physics.SphereCastNonAlloc(ray, detectionRadius, _hitBuffer, interactionDistance, interactableLayerMask);

        IInteractable bestTarget = null;
        IInteractable bestLocked = null;
        float bestDistance = float.MaxValue;
        float bestLockedDistance = float.MaxValue;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = _hitBuffer[i];
            if (!hit.collider.TryGetComponent<IInteractable>(out var interactable))
                interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable == null) continue;

            if (interactable.CanInteract() && hit.distance < bestDistance)
            {
                bestTarget = interactable;
                bestDistance = hit.distance;
            }
            else if (!interactable.CanInteract() && interactable is ILockable && hit.distance < bestLockedDistance)
            {
                // Track the closest locked-but-visible interactable for hint display
                bestLocked = interactable;
                bestLockedDistance = hit.distance;
            }
        }

        if (bestTarget != _currentTarget)
        {
            // Reset highlight of old target if it was a Pickupable
            if (_currentTarget is Pickupable oldP) oldP.SetHighlight(false);

            _currentTarget = bestTarget;

            if (_currentTarget != null)
            {
                if (_currentTarget is Pickupable newP) newP.SetHighlight(true);

                string key = _currentTarget.GetPreferredKey().ToString();
                string action = _currentTarget.GetActionName();
                SetPromptText($"[ {key} ] {action}");
                SetPromptVisible(true);
            }
            else if (bestLocked != null)
            {
                // Show the storyline hint instead of an interaction prompt
                string hint = StorylineManager.Instance != null
                    ? StorylineManager.Instance.GetLockedHint()
                    : null;
                SetPromptText(hint ?? "Not available yet.");
                SetPromptVisible(true);
            }
            else
            {
                SetPromptVisible(false);
            }
        }
        else if (_currentTarget == null && bestLocked != null)
        {
            // Keep updating hint text even if locked target hasn't changed
            string hint = StorylineManager.Instance != null
                ? StorylineManager.Instance.GetLockedHint()
                : null;
            SetPromptText(hint ?? "Not available yet.");
            SetPromptVisible(true);
        }
        else if (_currentTarget == null && bestLocked == null)
        {
            SetPromptVisible(false);
        }
    }

    private void HandleInput()
    {
        if (_currentTarget == null) return;
        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        InteractionKey preferred = _currentTarget.GetPreferredKey();
        bool pressed = false;

        if (preferred == InteractionKey.E && kb.eKey.wasPressedThisFrame) pressed = true;
        if (preferred == InteractionKey.F && kb.fKey.wasPressedThisFrame) pressed = true;

        if (pressed)
        {
            ConsumedInputThisFrame = true;
            
            // If it's a pickupable, delegate to PickupController.
            // Guard: do not pick up a second item while already holding one.
            if (_currentTarget is Pickupable p && _pickupController != null)
            {
                if (_pickupController.IsHoldingItem)
                {
                    Debug.Log("[InteractionController] Already holding an item — ignoring pickup.");
                }
                else
                {
                    _pickupController.Pickup(p);
                }
            }
            else
            {
                _currentTarget.OnInteract();
            }
            
            _currentTarget = null;
            SetPromptVisible(false);
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
