using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using Player;

/// <summary>
/// Renders the player's inventory in a simple grid panel.
/// Press E to toggle open / closed.
/// Pauses time and hides/shows the cursor while open.
/// Rebuilds slot cards whenever the underlying inventory data changes.
/// </summary>
public sealed class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }

    // ─── Inspector ────────────────────────────────────────────────────────────

    [Header("Panel")]
    [Tooltip("Root panel GameObject that contains all inventory UI elements.")]
    [SerializeField] private GameObject panelRoot;

    [Header("Grid")]
    [Tooltip("Parent RectTransform that holds the generated slot cards (use a Grid Layout Group).")]
    [SerializeField] private RectTransform slotsContainer;

    [Header("Slot Prefab")]
    [Tooltip("Prefab for a single inventory slot card. Must have an Image child named 'Icon' and a Text child named 'Label'.")]
    [SerializeField] private GameObject slotPrefab;

    [Header("Empty State")]
    [Tooltip("Label shown when the inventory has no items.")]
    [SerializeField] private Text emptyLabel;

    [Header("Close Button")]
    [Tooltip("Optional close button — pressing it hides the inventory.")]
    [SerializeField] private Button closeButton;

    [Header("Style")]
    [SerializeField] private Color panelBackgroundColor = new Color(0.05f, 0.05f, 0.05f, 0.92f);
    [SerializeField] private Color slotBackgroundColor  = new Color(0.15f, 0.15f, 0.15f, 1f);
    [SerializeField] private Color labelColor           = new Color(0.9f, 0.85f, 0.75f, 1f);
    [SerializeField] private Color titleColor           = new Color(1f, 0.9f, 0.6f, 1f);

    // ─── State ────────────────────────────────────────────────────────────────

    /// <summary>True while the inventory panel is visible.</summary>
    public bool IsOpen { get; private set; }

    private PlayerInventory _inventory;
    private PlayerMovement _playerMovement;
    private readonly List<GameObject> _spawnedSlots = new List<GameObject>();

    // ─── Constants ────────────────────────────────────────────────────────────

    private const string SLOT_ICON_NAME  = "Icon";
    private const string SLOT_LABEL_NAME = "Label";
    private const string TITLE_TEXT      = "Inventory";
    private const int SLOT_FONT_SIZE     = 14;
    private const int TITLE_FONT_SIZE    = 26;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        EnsurePanelExists();

        if (closeButton != null)
            closeButton.onClick.AddListener(Hide);
    }

    private void Start()
    {
        _inventory = PlayerInventory.Instance;
        _playerMovement = FindAnyObjectByType<PlayerMovement>();

        if (_inventory != null)
            _inventory.OnInventoryChanged += RebuildSlots;

        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_inventory != null)
            _inventory.OnInventoryChanged -= RebuildSlots;

        if (Instance == this) Instance = null;
    }

    private void Update()
    {
        if (Keyboard.current == null) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
            Toggle();
    }

    // ─── Public API ───────────────────────────────────────────────────────────

    /// <summary>Opens the inventory panel.</summary>
    public void Show()
    {
        if (IsOpen) return;
        IsOpen = true;

        if (panelRoot != null)
            panelRoot.SetActive(true);

        RebuildSlots();
        LockPlayer(true);
    }

    /// <summary>Closes the inventory panel.</summary>
    public void Hide()
    {
        if (!IsOpen) return;
        IsOpen = false;

        if (panelRoot != null)
            panelRoot.SetActive(false);

        LockPlayer(false);
    }

    /// <summary>Toggles the inventory panel open/closed.</summary>
    public void Toggle()
    {
        if (IsOpen) Hide();
        else Show();
    }

    // ─── Slot Building ────────────────────────────────────────────────────────

    /// <summary>Destroys all existing slot cards and rebuilds them from the current inventory.</summary>
    private void RebuildSlots()
    {
        // Clear old slots
        foreach (GameObject slot in _spawnedSlots)
        {
            if (slot != null) Destroy(slot);
        }
        _spawnedSlots.Clear();

        if (_inventory == null) return;

        bool hasItems = _inventory.Items.Count > 0;

        if (emptyLabel != null)
            emptyLabel.gameObject.SetActive(!hasItems);

        for (int i = 0; i < _inventory.Items.Count; i++)
        {
            InventoryItem item = _inventory.Items[i];
            GameObject slotGO = CreateSlot(item);
            _spawnedSlots.Add(slotGO);
        }
    }

    /// <summary>Instantiates or procedurally builds a single slot card for the given item.</summary>
    private GameObject CreateSlot(InventoryItem item)
    {
        GameObject slotGO;

        if (slotPrefab != null)
        {
            slotGO = Instantiate(slotPrefab, slotsContainer);
        }
        else
        {
            slotGO = BuildSlotProcedurally(item);
            return slotGO;
        }

        // Populate prefab children
        Image iconImage = slotGO.transform.Find(SLOT_ICON_NAME)?.GetComponent<Image>();
        Text labelText  = slotGO.transform.Find(SLOT_LABEL_NAME)?.GetComponent<Text>();

        if (iconImage != null)
        {
            iconImage.enabled = item.icon != null;
            if (item.icon != null)
                iconImage.sprite = item.icon;
        }

        if (labelText != null)
            labelText.text = item.displayName;

        return slotGO;
    }

    /// <summary>Procedurally builds a slot card when no prefab is assigned.</summary>
    private GameObject BuildSlotProcedurally(InventoryItem item)
    {
        GameObject card = new GameObject($"Slot_{item.displayName}", typeof(RectTransform));
        card.transform.SetParent(slotsContainer, false);

        // Card background
        Image bg = card.AddComponent<Image>();
        bg.color = slotBackgroundColor;

        // Icon (top half)
        if (item.icon != null)
        {
            GameObject iconGO = new GameObject(SLOT_ICON_NAME, typeof(RectTransform));
            iconGO.transform.SetParent(card.transform, false);

            RectTransform iconRect = iconGO.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.1f, 0.35f);
            iconRect.anchorMax = new Vector2(0.9f, 0.95f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;

            Image iconImg = iconGO.AddComponent<Image>();
            iconImg.sprite = item.icon;
            iconImg.preserveAspect = true;
        }

        // Label (bottom strip)
        GameObject labelGO = new GameObject(SLOT_LABEL_NAME, typeof(RectTransform));
        labelGO.transform.SetParent(card.transform, false);

        RectTransform labelRect = labelGO.GetComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 0f);
        labelRect.anchorMax = new Vector2(1f, 0.38f);
        labelRect.offsetMin = new Vector2(4f, 2f);
        labelRect.offsetMax = new Vector2(-4f, -2f);

        Text label = labelGO.AddComponent<Text>();
        label.text = item.displayName;
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = SLOT_FONT_SIZE;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = labelColor;

        return card;
    }

    // ─── Player Lock ──────────────────────────────────────────────────────────

    /// <summary>Freezes or unfreezes player movement and camera look when the inventory is open.</summary>
    private void LockPlayer(bool locked)
    {
        // Cursor
        Cursor.lockState = locked ? CursorLockMode.None : CursorLockMode.Locked;
        Cursor.visible   = locked;

        // Freeze movement
        if (_playerMovement != null)
            _playerMovement.enabled = !locked;
    }

    // ─── Procedural Panel Construction ────────────────────────────────────────

    /// <summary>
    /// Builds the entire inventory panel procedurally when no panel prefab has been assigned via the Inspector.
    /// Produces: a full-screen darkened overlay > a centred card > title > grid > empty label.
    /// </summary>
    private void EnsurePanelExists()
    {
        if (panelRoot != null && slotsContainer != null) return;

        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[InventoryUI] No Canvas found — cannot build procedural panel.");
            return;
        }

        // ── Overlay (full-screen semi-transparent background) ──────────────
        GameObject overlay = new GameObject("InventoryOverlay", typeof(RectTransform));
        overlay.transform.SetParent(canvas.transform, false);
        panelRoot = overlay;

        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayBg = overlay.AddComponent<Image>();
        overlayBg.color = new Color(0f, 0f, 0f, 0.65f);

        // ── Card (centred panel) ───────────────────────────────────────────
        GameObject card = new GameObject("InventoryCard", typeof(RectTransform));
        card.transform.SetParent(overlay.transform, false);

        RectTransform cardRect = card.GetComponent<RectTransform>();
        cardRect.anchorMin = new Vector2(0.5f, 0.5f);
        cardRect.anchorMax = new Vector2(0.5f, 0.5f);
        cardRect.pivot     = new Vector2(0.5f, 0.5f);
        cardRect.sizeDelta = new Vector2(600f, 480f);
        cardRect.anchoredPosition = Vector2.zero;

        Image cardBg = card.AddComponent<Image>();
        cardBg.color = panelBackgroundColor;

        // ── Title ──────────────────────────────────────────────────────────
        GameObject titleGO = new GameObject("Title", typeof(RectTransform));
        titleGO.transform.SetParent(card.transform, false);

        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 1f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.pivot     = new Vector2(0.5f, 1f);
        titleRect.sizeDelta = new Vector2(0f, 50f);
        titleRect.anchoredPosition = Vector2.zero;

        Text title = titleGO.AddComponent<Text>();
        title.text      = TITLE_TEXT;
        title.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        title.fontSize  = TITLE_FONT_SIZE;
        title.fontStyle = FontStyle.Bold;
        title.alignment = TextAnchor.MiddleCenter;
        title.color     = titleColor;

        // ── Close button (top-right X) ────────────────────────────────────
        GameObject closeGO = new GameObject("CloseButton", typeof(RectTransform));
        closeGO.transform.SetParent(card.transform, false);

        RectTransform closeRect = closeGO.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1f, 1f);
        closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot     = new Vector2(1f, 1f);
        closeRect.sizeDelta = new Vector2(44f, 44f);
        closeRect.anchoredPosition = Vector2.zero;

        Image closeBg = closeGO.AddComponent<Image>();
        closeBg.color = new Color(0.7f, 0.15f, 0.15f, 0.85f);

        closeButton = closeGO.AddComponent<Button>();

        GameObject closeLabel = new GameObject("Label", typeof(RectTransform));
        closeLabel.transform.SetParent(closeGO.transform, false);
        RectTransform closeLabelRect = closeLabel.GetComponent<RectTransform>();
        closeLabelRect.anchorMin = Vector2.zero;
        closeLabelRect.anchorMax = Vector2.one;
        closeLabelRect.offsetMin = Vector2.zero;
        closeLabelRect.offsetMax = Vector2.zero;

        Text closeText = closeLabel.AddComponent<Text>();
        closeText.text      = "✕";
        closeText.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        closeText.fontSize  = 22;
        closeText.fontStyle = FontStyle.Bold;
        closeText.alignment = TextAnchor.MiddleCenter;
        closeText.color     = Color.white;

        closeButton.onClick.AddListener(Hide);

        // ── Grid content area ──────────────────────────────────────────────
        GameObject gridGO = new GameObject("SlotsContainer", typeof(RectTransform));
        gridGO.transform.SetParent(card.transform, false);

        RectTransform gridRect = gridGO.GetComponent<RectTransform>();
        gridRect.anchorMin = Vector2.zero;
        gridRect.anchorMax = Vector2.one;
        gridRect.offsetMin = new Vector2(10f, 10f);
        gridRect.offsetMax = new Vector2(-10f, -56f);

        GridLayoutGroup grid = gridGO.AddComponent<GridLayoutGroup>();
        grid.cellSize        = new Vector2(120f, 140f);
        grid.spacing         = new Vector2(10f, 10f);
        grid.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        grid.startAxis       = GridLayoutGroup.Axis.Horizontal;
        grid.childAlignment  = TextAnchor.UpperLeft;
        grid.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 4;

        slotsContainer = gridRect;

        // ── Empty state label ─────────────────────────────────────────────
        GameObject emptyGO = new GameObject("EmptyLabel", typeof(RectTransform));
        emptyGO.transform.SetParent(card.transform, false);

        RectTransform emptyRect = emptyGO.GetComponent<RectTransform>();
        emptyRect.anchorMin = new Vector2(0f, 0.3f);
        emptyRect.anchorMax = new Vector2(1f, 0.7f);
        emptyRect.offsetMin = Vector2.zero;
        emptyRect.offsetMax = Vector2.zero;

        emptyLabel = emptyGO.AddComponent<Text>();
        emptyLabel.text      = "— 背包是空的 —";
        emptyLabel.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        emptyLabel.fontSize  = 20;
        emptyLabel.alignment = TextAnchor.MiddleCenter;
        emptyLabel.color     = new Color(0.6f, 0.6f, 0.6f, 0.8f);
    }
}
