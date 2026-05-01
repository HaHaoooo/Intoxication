using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Self-building health bar that subscribes to a HealthComponent and renders
/// a fill bar + numeric label inside the scene Canvas.
/// Fill is driven by localScale.x on the fill rect — reliable across all Unity UI setups.
/// Automatically cleans up its Canvas child when the host GameObject is destroyed.
/// </summary>
public class HealthBarUI : MonoBehaviour
{
    [Header("Appearance")]
    public string barLabel = "Health";
    public Color fillColor = new Color(0.85f, 0.15f, 0.15f, 1f);

    [Header("Screen Layout")]
    public Vector2 screenAnchorMin = new Vector2(0f, 0f);
    public Vector2 screenAnchorMax = new Vector2(0f, 0f);
    public Vector2 screenPivot    = new Vector2(0f, 0f);
    public Vector2 screenOffset   = new Vector2(20f, 20f);
    public Vector2 barSize        = new Vector2(220f, 26f);

    private RectTransform _fillRect;
    private Text          _valueText;
    private GameObject    _uiRoot;

    private void Start()
    {
        BuildBar();

        HealthComponent health = GetComponent<HealthComponent>();
        if (health != null)
            BindTo(health);
    }

    private void OnDestroy()
    {
        if (_uiRoot != null)
            Destroy(_uiRoot);
    }

    /// <summary>Subscribes to the given HealthComponent and shows the bar.</summary>
    public void BindTo(HealthComponent health)
    {
        health.OnHealthChanged += UpdateBar;
        health.OnDeath         += OnOwnerDeath;

        // Immediately reflect current state
        UpdateBar(health.CurrentHealth, health.maxHealth);

        if (_uiRoot != null)
            _uiRoot.SetActive(true);
    }

    private void UpdateBar(float current, float max)
    {
        float ratio = max > 0f ? Mathf.Clamp01(current / max) : 0f;

        // Scale the fill rect from its left edge (pivot.x = 0)
        if (_fillRect != null)
            _fillRect.localScale = new Vector3(ratio, 1f, 1f);

        // Update numeric label
        if (_valueText != null)
            _valueText.text = $"{barLabel}  {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    private void OnOwnerDeath()
    {
        if (_uiRoot != null)
            _uiRoot.SetActive(false);
    }

    private void BuildBar()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[HealthBarUI] No Canvas found — health bar will not appear.");
            return;
        }

        // ── Root panel ────────────────────────────────────────────────────────
        _uiRoot = new GameObject($"{barLabel}_HealthBar");
        _uiRoot.transform.SetParent(canvas.transform, false);

        RectTransform rootRect    = _uiRoot.AddComponent<RectTransform>();
        rootRect.anchorMin        = screenAnchorMin;
        rootRect.anchorMax        = screenAnchorMax;
        rootRect.pivot            = screenPivot;
        rootRect.anchoredPosition = screenOffset;
        rootRect.sizeDelta        = barSize;

        Image bg   = _uiRoot.AddComponent<Image>();
        bg.color   = new Color(0.08f, 0.08f, 0.08f, 0.85f);

        // ── Fill bar ──────────────────────────────────────────────────────────
        GameObject fillGO = new GameObject("Fill");
        fillGO.transform.SetParent(_uiRoot.transform, false);

        _fillRect           = fillGO.AddComponent<RectTransform>();
        _fillRect.anchorMin = new Vector2(0f, 0f);
        _fillRect.anchorMax = new Vector2(1f, 1f);
        _fillRect.offsetMin = new Vector2(3f,  3f);
        _fillRect.offsetMax = new Vector2(-3f, -3f);
        _fillRect.pivot     = new Vector2(0f, 0.5f); // scale shrinks from left edge

        Image fillImg         = fillGO.AddComponent<Image>();
        fillImg.color         = fillColor;
        fillImg.raycastTarget = false;

        // ── Numeric label ─────────────────────────────────────────────────────
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(_uiRoot.transform, false);

        RectTransform labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin     = Vector2.zero;
        labelRect.anchorMax     = Vector2.one;
        labelRect.offsetMin     = Vector2.zero;
        labelRect.offsetMax     = Vector2.zero;

        _valueText               = labelGO.AddComponent<Text>();
        _valueText.font          = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _valueText.fontSize      = 12;
        _valueText.fontStyle     = FontStyle.Bold;
        _valueText.alignment     = TextAnchor.MiddleCenter;
        _valueText.color         = Color.white;
        _valueText.raycastTarget = false;

        // Hidden until BindTo is called
        _uiRoot.SetActive(false);
    }
}
