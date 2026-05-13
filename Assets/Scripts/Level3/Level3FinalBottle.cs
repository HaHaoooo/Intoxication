using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// The sole alcohol bottle in the Level 3 bedroom.
///
/// Does NOT use the standard IInteractable system. Instead it manages its own
/// proximity detection, dual-prompt overlay, and key handling:
///   [C] Break it  — scatters the bottle and triggers the true ending.
///   [F] Drink it  — triggers the bad ending and restarts from Level 1.
///
/// Remains locked (no prompt shown) until Level3SequenceController.Unlock() is
/// called after the player drops the axe. Disables any Pickupable on the same
/// GameObject while active so the normal pickup system cannot steal the F key.
/// </summary>
public class Level3FinalBottle : MonoBehaviour
{
    public static Level3FinalBottle Instance { get; private set; }

    [Tooltip("How close the player must be before the prompts appear.")]
    [SerializeField] private float activationRadius = 3.5f;

    // ── State ─────────────────────────────────────────────────────────────────

    private bool _isLocked  = true;
    private bool _used      = false;
    private bool _inRange   = false;
    private Transform _playerTransform;
    private Pickupable _pickupable;

    // ── Prompt UI ─────────────────────────────────────────────────────────────

    private GameObject _promptRoot;
    private Text       _breakLabel;
    private Text       _drinkLabel;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        _pickupable = GetComponent<Pickupable>();
    }

    private void Start()
    {
        // Permanently remove the Pickupable so PlayerInteractionController cannot
        // detect it as an IInteractable (GetComponent ignores enabled state).
        if (_pickupable != null)
        {
            Destroy(_pickupable);
            _pickupable = null;
        }

        GameObject playerGO = GameObject.FindWithTag("Player");
        if (playerGO != null) _playerTransform = playerGO.transform;

        BuildPromptUI();
        SetPromptVisible(false);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        if (_promptRoot != null) Destroy(_promptRoot);
    }

    // ── Public gate ───────────────────────────────────────────────────────────

    /// <summary>Called by Level3SequenceController after the player drops the axe.</summary>
    public void Unlock() => _isLocked = false;

    // ── Per-frame logic ───────────────────────────────────────────────────────

    private void Update()
    {
        if (_used || _isLocked || _playerTransform == null) return;

        bool nowInRange = Vector3.Distance(transform.position, _playerTransform.position) <= activationRadius;

        if (nowInRange != _inRange)
        {
            _inRange = nowInRange;
            SetPromptVisible(_inRange);
        }

        if (!_inRange) return;

        Keyboard kb = Keyboard.current;
        if (kb == null) return;

        if (kb.cKey.wasPressedThisFrame)
        {
            Commit(OnBreak);
        }
        else if (kb.fKey.wasPressedThisFrame)
        {
            Commit(OnDrink);
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private void Commit(System.Action callback)
    {
        _used = true;
        SetPromptVisible(false);
        callback();
    }

    private void OnBreak()
    {
        ScatterBottle();
        Level3SequenceController.Instance?.OnBottleBroken();
    }

    private void OnDrink()
    {
        Level3SequenceController.Instance?.OnBottleDrunk();
    }

    /// <summary>Physically scatters bottle child segments to simulate shattering.</summary>
    private void ScatterBottle()
    {
        foreach (Transform child in transform)
        {
            Rigidbody rb = child.gameObject.AddComponent<Rigidbody>();
            rb.AddExplosionForce(3f, transform.position, 0.5f);
        }
    }

    // ── Prompt UI construction ────────────────────────────────────────────────

    private void BuildPromptUI()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        _promptRoot = new GameObject("FinalBottlePrompt");
        _promptRoot.transform.SetParent(canvas.transform, false);

        // Background panel — bottom centre, two-line height
        RectTransform bgRect = _promptRoot.AddComponent<RectTransform>();
        bgRect.anchorMin        = new Vector2(0.5f, 0f);
        bgRect.anchorMax        = new Vector2(0.5f, 0f);
        bgRect.pivot            = new Vector2(0.5f, 0f);
        bgRect.sizeDelta        = new Vector2(340f, 72f);
        bgRect.anchoredPosition = new Vector2(0f, 60f);

        Image bg = _promptRoot.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.62f);

        // Top line  —  [ C ] Break it
        _breakLabel = CreateLabel(_promptRoot.transform,
            anchorMin: new Vector2(0f, 0.52f), anchorMax: Vector2.one,
            color: new Color(1f, 0.82f, 0.4f, 1f));

        // Bottom line  —  [ F ] Drink it
        _drinkLabel = CreateLabel(_promptRoot.transform,
            anchorMin: Vector2.zero, anchorMax: new Vector2(1f, 0.48f),
            color: new Color(0.75f, 0.75f, 0.75f, 1f));

        _breakLabel.text = "[ C ]  Break it";
        _drinkLabel.text = "[ F ]  Drink it";
    }

    private static Text CreateLabel(Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        GameObject go = new GameObject("Label");
        go.transform.SetParent(parent, false);

        RectTransform rect = go.AddComponent<RectTransform>();
        rect.anchorMin  = anchorMin;
        rect.anchorMax  = anchorMax;
        rect.offsetMin  = new Vector2(12f, 0f);
        rect.offsetMax  = new Vector2(-12f, 0f);

        Text label = go.AddComponent<Text>();
        label.font              = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize          = 22;
        label.fontStyle         = FontStyle.Bold;
        label.alignment         = TextAnchor.MiddleCenter;
        label.color             = color;
        label.horizontalOverflow = HorizontalWrapMode.Wrap;
        label.verticalOverflow   = VerticalWrapMode.Overflow;
        label.raycastTarget     = false;
        return label;
    }

    private void SetPromptVisible(bool visible)
    {
        if (_promptRoot != null) _promptRoot.SetActive(visible);
    }
}
