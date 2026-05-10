using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Player;

/// <summary>
/// Player pickup system: detects, picks up, holds, drops, throws, inspects, and drinks Pickupable items.
/// Drinking uses procedural bottle movement (no Animator dependency).
/// </summary>
[DefaultExecutionOrder(1000)]
public class PlayerPickupController : MonoBehaviour
{
    [Header("Hold Point")]
    [SerializeField] private Transform holdPoint;
    [SerializeField] private Vector3 firstPersonHoldOffset = new Vector3(0.3f, -0.35f, 1.0f);
    [SerializeField] private Vector3 thirdPersonHoldOffset = new Vector3(0.4f, 1.2f, 0.6f);

    [Header("Hold & Throw")]
    [SerializeField] private float throwForce = 15f;
    [SerializeField] private float holdSmoothSpeed = 15f;

    [Header("Inspect")]
    [SerializeField] private float inspectRotationSensitivity = 0.5f;
    [SerializeField] private float inspectDistance = 1.2f;

    [Header("Drinking")]
    [SerializeField] private float drinkRaiseTime = 0.5f;
    [SerializeField] private float drinkHoldTime = 1.8f;
    [SerializeField] private float drinkLowerTime = 0.4f;
    [SerializeField] private Vector3 drinkLocalOffset = new Vector3(0.05f, 0.1f, 0.45f);
    [SerializeField] private Vector3 drinkLocalRotation = new Vector3(-80f, 0f, 15f);

    [Header("UI")]
    [SerializeField] private Image crosshair;
    [SerializeField] private Color hoverColor = Color.yellow;
    [SerializeField] private Text drinkPromptText;
    [SerializeField] private float promptLeftOffset = -120f;

    [Header("Audio")]
    [SerializeField] private AudioClip pickupSound;
    [SerializeField] private AudioClip dropSound;
    [SerializeField] private AudioClip throwSound;
    [SerializeField] private AudioClip drinkSound;
    [SerializeField, Range(0f, 1f)] private float pickupVolume = 0.5f;
    [SerializeField, Range(0f, 1f)] private float dropVolume = 0.35f;
    [SerializeField, Range(0f, 1f)] private float throwVolume = 0.4f;
    [SerializeField, Range(0f, 1f)] private float drinkVolume = 0.6f;

    /// <summary>True when holding a drinkable item; blocks F-key camera toggle.</summary>
public bool IsHoldingDrinkable => _heldItem != null && _heldItem.IsDrinkable && !_isDrinking;

    /// <summary>True while the procedural drinking action is playing.</summary>
    public bool IsDrinking => _isDrinking;

    // Cached references
    private Color _defaultCrosshairColor;
    private AudioSource _audioSource;
    private PlayerMovement _movement;
    private PlayerInteractionController _interactionController;
    private Camera _mainCam;
    private RectTransform _promptRect;
    private Canvas _parentCanvas;
    private int _detectLayerMask;

    // Runtime state
    private Pickupable _hoveredItem;
    private Pickupable _heldItem;
    private bool _isInspecting;
    private bool _isDrinking;
    private float _interactCooldownTimer;
    private float _inspectYaw;
    private float _inspectPitch;
    private bool _pendingReparent;

    // Pre-allocated buffers to avoid per-frame allocations
    private const int MAX_SPHERE_CAST_HITS = 16;
    private readonly RaycastHit[] _hitBuffer = new RaycastHit[MAX_SPHERE_CAST_HITS];

    // Procedural drink state
    private float _drinkElapsed;
    private Vector3 _drinkStartPos;
    private Quaternion _drinkStartRot;
    private Vector3 _drinkTargetPos;
    private Quaternion _drinkTargetRot;

    private const float INTERACT_COOLDOWN = 0.25f;

    private enum DrinkPhase { Raise, Hold, Lower }
    private DrinkPhase _drinkPhase;

    // ──────────────────────────── Lifecycle ────────────────────────────

    private void Awake()
    {
        _movement = GetComponent<PlayerMovement>();
        _interactionController = GetComponent<PlayerInteractionController>();

        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.spatialBlend = 0f;

        _detectLayerMask = ~LayerMask.GetMask("Ignore Raycast", "Player", "Character", "Ground", "UI", "Water");
    }

    private void Start()
    {
        _mainCam = Camera.main;

        if (crosshair != null)
            _defaultCrosshairColor = crosshair.color;

        EnsureHoldPointExists();
        EnsureDrinkPromptUI();

        if (holdPoint != null)
        {
            holdPoint.localPosition = _movement != null && _movement.IsFirstPerson
                ? firstPersonHoldOffset
                : thirdPersonHoldOffset;
            holdPoint.localRotation = Quaternion.identity;
        }

        if (_mainCam != null)
        {
            _mainCam.useOcclusionCulling = false;
        }
    }

    private void Update()
    {
        UpdateHoldPointParent();

        // Guard against externally destroyed references
        if (_heldItem != null && !_heldItem)
        {
            _heldItem = null;
            _isDrinking = false;
            _isInspecting = false;
            if (_movement != null) _movement.canRotateCamera = true;
            SetCrosshairVisible(true);
        }

        // Block all pickup interaction while dialogue is active
        if (_interactionController != null && _interactionController.IsInDialogue)
            return;

        if (_isDrinking)
        {
            UpdateDrinking();
            return;
        }

        // Handle Drop/Throw/Drink input if holding something
        HandleHeldActions();

        if (_heldItem != null)
        {
            ProcessInspectMode();
        }

        UpdateDrinkPromptUI();
    }

    private void HandleHeldActions()
    {
        if (_heldItem == null || _isDrinking) return;
        if (_interactCooldownTimer > 0f)
        {
            _interactCooldownTimer -= Time.deltaTime;
            return;
        }

        Keyboard kb = Keyboard.current;
        Mouse ms = Mouse.current;
        if (kb == null || ms == null) return;

        // E to drop
        if (kb.eKey.wasPressedThisFrame)
        {
            Drop();
            _interactCooldownTimer = INTERACT_COOLDOWN;
        }
        // RMB to throw
        else if (ms.rightButton.wasPressedThisFrame)
        {
            Throw();
            _interactCooldownTimer = INTERACT_COOLDOWN;
        }
        // F to drink
        else if (kb.fKey.wasPressedThisFrame && _heldItem.IsDrinkable)
        {
            StartDrinking();
        }
    }

    // ──────────────────────────── Hold Point ────────────────────────────

    private void LateUpdate()
    {
        ExecutePendingReparent();

        // Skip smooth follow for Axe as it is parented and managed by AxePickup/AxeWeapon
        if (_heldItem != null && !_isDrinking && !(_heldItem is AxePickup))
        {
            SmoothFollowHoldPoint();
        }
    }

    // ──────────────────────────── Hold Point ────────────────────────────

    /// <summary>Creates a fallback HoldPoint if none is assigned.</summary>
    private void EnsureHoldPointExists()
    {
        if (holdPoint != null) return;

        if (_mainCam == null)
        {
            Debug.LogWarning("[Pickup] No Main Camera — cannot create HoldPoint.");
            return;
        }

        GameObject go = new GameObject("HoldPoint");
        go.transform.SetParent(_mainCam.transform, false);
        go.transform.localPosition = firstPersonHoldOffset;
        go.transform.localRotation = Quaternion.identity;
        holdPoint = go.transform;
    }

    /// <summary>Re-parents holdPoint between camera (FP) and player body (TP).
    /// Deferred to LateUpdate to avoid Unity 6 TransformHierarchy system conflicts.</summary>
    private void UpdateHoldPointParent()
    {
        if (holdPoint == null || _movement == null) return;

        bool isFP = _movement.IsFirstPerson;
        Transform targetParent = isFP ? (_mainCam != null ? _mainCam.transform : null) : transform;

        if (targetParent != null && holdPoint.parent != targetParent)
        {
            _pendingReparent = true;
        }
    }

    /// <summary>Executes deferred holdPoint reparenting to avoid TransformHierarchy conflicts.</summary>
    private void ExecutePendingReparent()
    {
        if (!_pendingReparent || holdPoint == null || _movement == null) return;
        _pendingReparent = false;

        bool isFP = _movement.IsFirstPerson;
        Transform targetParent = isFP ? (_mainCam != null ? _mainCam.transform : null) : transform;

        if (targetParent != null && holdPoint.parent != targetParent)
        {
            // Use worldPositionStays = false to avoid coordinate recalc errors in Unity 6
            holdPoint.SetParent(targetParent, false);
            holdPoint.localPosition = isFP ? firstPersonHoldOffset : thirdPersonHoldOffset;
            holdPoint.localRotation = Quaternion.identity;
        }
    }

    private void ClearHover()
    {
        // Now handled by PlayerInteractionController
    }

    // ──────────────────────────── Detection ────────────────────────────
    // Detection is now handled by PlayerInteractionController.

    // ──────────────────────────── Input ────────────────────────────
    // Input is now handled by PlayerInteractionController and HandleHeldActions().

    // ──────────────────────────── Pickup / Drop / Throw ────────────────────────────

    /// <summary>Picks up the target item and registers it in the player's inventory.</summary>
    public void Pickup(Pickupable target)
    {
        if (target == null) return;
        _heldItem = target;
        _heldItem.OnPickedUp();

        // Special handling for the Axe: attach to bone instead of holdPoint
        if (_heldItem is AxePickup axe)
        {
            axe.OnAxePickedUp(gameObject);
        }
        else if (holdPoint != null)
        {
            target.transform.position = holdPoint.position;
            target.transform.rotation = holdPoint.rotation;
        }

        // Register in inventory
        if (PlayerInventory.Instance != null)
        {
            var item = new Player.InventoryItem(target.name, null, target.IsDrinkable, target);
            PlayerInventory.Instance.AddItem(item);
        }

        ClearHover();
        PlaySound(pickupSound, pickupVolume);
        SetCrosshairVisible(false);
    }

    /// <summary>Drops the held item at its current position and removes it from inventory.</summary>
    private void Drop()
    {
        if (_heldItem == null) return;

        // Special handling for Axe: disable weapon component
        if (_heldItem is AxePickup)
        {
            AxeWeapon weapon = GetComponent<AxeWeapon>();
            if (weapon != null) weapon.enabled = false;
        }

        RemoveFromInventory(_heldItem);
        ExitInspectIfActive();
        _heldItem.OnReleased();
        _heldItem = null;
        PlaySound(dropSound, dropVolume);
        SetCrosshairVisible(true);
    }

    /// <summary>Throws the held item forward and removes it from inventory.</summary>
    private void Throw()
    {
        if (_heldItem == null) return;

        // Special handling for Axe: disable weapon component
        if (_heldItem is AxePickup)
        {
            AxeWeapon weapon = GetComponent<AxeWeapon>();
            if (weapon != null) weapon.enabled = false;
        }

        Pickupable item = _heldItem;
        RemoveFromInventory(item);
        ExitInspectIfActive();
        item.OnReleased();
        _heldItem = null;

        if (_mainCam != null)
            item.AddThrowForce(_mainCam.transform.forward * throwForce);

        PlaySound(throwSound, throwVolume);
        SetCrosshairVisible(true);
    }

    // ──────────────────────────── Hold (Smooth Follow) ────────────────────────────

    /// <summary>Smoothly moves held item toward holdPoint.</summary>
    private void SmoothFollowHoldPoint()
    {
        if (_heldItem == null || holdPoint == null) return;

        Transform itemTransform = _heldItem.transform;

        if (_isInspecting)
        {
            if (_mainCam == null) return;

            Vector3 camPos = _mainCam.transform.position;
            Vector3 camForward = _mainCam.transform.forward;
            
            if (!float.IsFinite(camPos.x) || !float.IsFinite(camForward.x)) return;

            Vector3 inspectCenter = camPos + camForward * inspectDistance;
            Vector3 visualOffset = _heldItem.VisualCenterOffset;

            Quaternion orbitRotation = Quaternion.Euler(_inspectPitch, _inspectYaw, 0f);
            
            Vector3 targetPos = inspectCenter - orbitRotation * visualOffset;
            
            if (float.IsFinite(targetPos.x))
            {
                itemTransform.rotation = orbitRotation;
                itemTransform.position = targetPos;
            }
        }
        else
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;

            float t = Mathf.Clamp01(holdSmoothSpeed * dt);
            
            Vector3 targetPos = Vector3.Lerp(itemTransform.position, holdPoint.position, t);
            Quaternion targetRot = Quaternion.Slerp(itemTransform.rotation, holdPoint.rotation, t);

            if (float.IsFinite(targetPos.x) && float.IsFinite(targetRot.x))
            {
                itemTransform.position = targetPos;
                itemTransform.rotation = targetRot;
            }
        }
    }

    // ──────────────────────────── Inspect Mode ────────────────────────────

    /// <summary>Handles inspect mode toggle (Tab held = inspect, release = exit).</summary>
    private void ProcessInspectMode()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.tabKey.isPressed)
        {
            EnterInspect();
            UpdateInspectOrbit();
        }
        else if (_isInspecting)
        {
            ExitInspect();
        }
    }

    private void EnterInspect()
    {
        if (_isInspecting) return;
        _isInspecting = true;

        Vector3 currentEuler = _heldItem != null ? _heldItem.transform.eulerAngles : Vector3.zero;
        _inspectYaw = currentEuler.y;
        _inspectPitch = currentEuler.x;

        if (_movement != null)
            _movement.canRotateCamera = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ExitInspect()
    {
        if (!_isInspecting) return;
        _isInspecting = false;

        if (_movement != null)
            _movement.canRotateCamera = true;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void ExitInspectIfActive()
    {
        if (_isInspecting) ExitInspect();
    }

    /// <summary>Accumulates mouse delta into orbit yaw/pitch.</summary>
    private void UpdateInspectOrbit()
    {
        if (Mouse.current == null) return;

        Vector2 delta = Mouse.current.delta.ReadValue();
        _inspectYaw += delta.x * inspectRotationSensitivity;
        _inspectPitch -= delta.y * inspectRotationSensitivity;
    }

    // ──────────────────────────── Procedural Drinking ────────────────────────────

    /// <summary>Begins procedural drinking: raises and tilts the bottle toward mouth.</summary>
    private void StartDrinking()
    {
        if (_isDrinking || _heldItem == null || !_heldItem.IsDrinkable) return;

        _isDrinking = true;
        ExitInspectIfActive();

        // Force first person and block camera controls during drinking
        if (_movement != null)
        {
            _movement.ForceFirstPerson();
            _movement.canRotateCamera = false;
        }

        // Record start pose and compute target pose (relative to camera)
        Transform itemT = _heldItem.transform;
        _drinkStartPos = itemT.position;
        _drinkStartRot = itemT.rotation;

        if (_mainCam != null)
        {
            Transform camT = _mainCam.transform;
            _drinkTargetPos = camT.TransformPoint(drinkLocalOffset);
            _drinkTargetRot = camT.rotation * Quaternion.Euler(drinkLocalRotation);
        }

        _drinkPhase = DrinkPhase.Raise;
        _drinkElapsed = 0f;

        PlaySound(drinkSound, drinkVolume);
        SetDrinkPromptVisible(false);

        Debug.Log($"[Pickup] Started drinking '{_heldItem.name}'.");
    }

    /// <summary>Updates the procedural drinking phases: Raise -> Hold -> Lower -> Finish.</summary>
    private void UpdateDrinking()
    {
        if (_heldItem == null)
        {
            FinishDrinking();
            return;
        }

        _drinkElapsed += Time.deltaTime;
        Transform itemT = _heldItem.transform;

        // Keep target position relative to camera (head tracks with camera)
        if (_mainCam != null)
        {
            Transform camT = _mainCam.transform;
            _drinkTargetPos = camT.TransformPoint(drinkLocalOffset);
            _drinkTargetRot = camT.rotation * Quaternion.Euler(drinkLocalRotation);
        }

        switch (_drinkPhase)
        {
            case DrinkPhase.Raise:
            {
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_drinkElapsed / drinkRaiseTime));
                itemT.position = Vector3.Lerp(_drinkStartPos, _drinkTargetPos, t);
                itemT.rotation = Quaternion.Slerp(_drinkStartRot, _drinkTargetRot, t);

                if (_drinkElapsed >= drinkRaiseTime)
                {
                    _drinkPhase = DrinkPhase.Hold;
                    _drinkElapsed = 0f;
                }
                break;
            }
            case DrinkPhase.Hold:
            {
                // Keep bottle at drinking position, following camera
                itemT.position = _drinkTargetPos;
                itemT.rotation = _drinkTargetRot;

                if (_drinkElapsed >= drinkHoldTime)
                {
                    _drinkPhase = DrinkPhase.Lower;
                    _drinkElapsed = 0f;
                    _drinkStartPos = itemT.position;
                    _drinkStartRot = itemT.rotation;
                }
                break;
            }
            case DrinkPhase.Lower:
            {
                // Lower back toward hold point
                Vector3 lowerTarget = holdPoint != null ? holdPoint.position : _drinkStartPos;
                Quaternion lowerRotTarget = holdPoint != null ? holdPoint.rotation : _drinkStartRot;

                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_drinkElapsed / drinkLowerTime));
                itemT.position = Vector3.Lerp(_drinkStartPos, lowerTarget, t);
                itemT.rotation = Quaternion.Slerp(_drinkStartRot, lowerRotTarget, t);

                if (_drinkElapsed >= drinkLowerTime)
                {
                    FinishDrinking();
                }
                break;
            }
        }
    }

    /// <summary>Ends the drinking action: restores controls, removes from inventory, and destroys the consumed item.</summary>
    private void FinishDrinking()
    {
        _isDrinking = false;

        if (_movement != null)
            _movement.canRotateCamera = true;

        if (_heldItem != null)
        {
            RemoveFromInventory(_heldItem);
            GameObject itemObj = _heldItem.gameObject;
            _heldItem = null;
            Destroy(itemObj);
            Debug.Log("[Pickup] Item consumed and destroyed.");
        }

        SetCrosshairVisible(true);
    }

    /// <summary>Finds and removes the inventory entry matching the given Pickupable.</summary>
    private static void RemoveFromInventory(Pickupable target)
    {
        if (PlayerInventory.Instance == null) return;

        foreach (Player.InventoryItem entry in PlayerInventory.Instance.Items)
        {
            if (entry.sourcePickupable == target)
            {
                PlayerInventory.Instance.RemoveItem(entry);
                return;
            }
        }
    }

    // ──────────────────────────── Drink Prompt UI ────────────────────────────

    /// <summary>Auto-creates the drink prompt UI positioned to the left of the held item.</summary>
    private void EnsureDrinkPromptUI()
    {
        if (drinkPromptText != null)
        {
            CachePromptReferences();
            return;
        }

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[Pickup] No Canvas found — cannot create drink prompt.");
            return;
        }

        _parentCanvas = canvas;

        GameObject promptGO = new GameObject("DrinkPrompt");
        promptGO.transform.SetParent(canvas.transform, false);

        RectTransform bgRect = promptGO.AddComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(180f, 40f);
        bgRect.anchoredPosition = Vector2.zero;

        Image bgImage = promptGO.AddComponent<Image>();
        bgImage.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject textGO = new GameObject("PromptText");
        textGO.transform.SetParent(promptGO.transform, false);

        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        drinkPromptText = textGO.AddComponent<Text>();
        drinkPromptText.text = "[ F ] Drink";
        drinkPromptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        drinkPromptText.fontSize = 20;
        drinkPromptText.alignment = TextAnchor.MiddleCenter;
        drinkPromptText.color = new Color(1f, 0.9f, 0.55f, 1f);

        _promptRect = bgRect;
        promptGO.SetActive(false);
    }

    /// <summary>Caches references if the prompt was assigned in Inspector.</summary>
    private void CachePromptReferences()
    {
        if (drinkPromptText == null) return;
        Transform root = drinkPromptText.transform.parent != null
            ? drinkPromptText.transform.parent
            : drinkPromptText.transform;
        _promptRect = root.GetComponent<RectTransform>();
        _parentCanvas = root.GetComponentInParent<Canvas>();
    }

    /// <summary>Shows/hides and positions the drink prompt to the left of the held item.</summary>
    private void UpdateDrinkPromptUI()
    {
        bool shouldShow = _heldItem != null && _heldItem.IsDrinkable && !_isDrinking && !_isInspecting;
        SetDrinkPromptVisible(shouldShow);

        if (shouldShow && _promptRect != null && _mainCam != null && _parentCanvas != null)
        {
            PositionPromptNextToItem();
        }
    }

    /// <summary>Positions the prompt UI to the left of the held item's visual center in screen space.</summary>
    private void PositionPromptNextToItem()
    {
        // Use renderer bounds center instead of transform.position (pivot) to get the visual center
        Vector3 itemWorldCenter = GetItemVisualCenter();
        
        if (!float.IsFinite(itemWorldCenter.x)) return;

        Vector3 itemScreenPos = _mainCam.WorldToScreenPoint(itemWorldCenter);

        // Item is behind the camera or off screen (safety check)
        if (itemScreenPos.z < 0f || !float.IsFinite(itemScreenPos.x)) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _parentCanvas.transform as RectTransform,
            new Vector2(itemScreenPos.x + promptLeftOffset, itemScreenPos.y),
            _parentCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _mainCam,
            out Vector2 localPoint
        );

        if (float.IsFinite(localPoint.x))
        {
            _promptRect.anchoredPosition = localPoint;
        }
    }

    /// <summary>Returns the visual center of the held item using cached offset data.</summary>
    private Vector3 GetItemVisualCenter()
    {
        if (_heldItem == null) return Vector3.zero;
        return _heldItem.GetVisualCenterWorld();
    }

    private void SetDrinkPromptVisible(bool visible)
    {
        if (drinkPromptText == null) return;
        GameObject promptRoot = drinkPromptText.transform.parent != null
            ? drinkPromptText.transform.parent.gameObject
            : drinkPromptText.gameObject;
        if (promptRoot.activeSelf != visible)
            promptRoot.SetActive(visible);
    }

    // ──────────────────────────── Utility ────────────────────────────

    private void PlaySound(AudioClip clip, float volume)
    {
        if (clip != null && _audioSource != null)
            _audioSource.PlayOneShot(clip, volume);
    }

    private void SetCrosshairColor(Color color)
    {
        if (crosshair != null)
            crosshair.color = color;
    }

    private void SetCrosshairVisible(bool visible)
    {
        if (crosshair != null)
            crosshair.gameObject.SetActive(visible);
    }
}