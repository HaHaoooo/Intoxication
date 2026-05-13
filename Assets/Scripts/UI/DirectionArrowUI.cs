using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Singleton. Displays exactly one pulsing, screen-edge directional arrow with
/// a text label that guides the player toward the current story waypoint.
/// Call SetTarget() to redirect it; call Hide() to remove it.
/// The arrow disappears automatically when the player enters hideRadius of the target.
/// Only one target is ever active at a time — calling SetTarget() replaces the previous one.
/// </summary>
public class DirectionArrowUI : MonoBehaviour
{
    public static DirectionArrowUI Instance { get; private set; }

    // ── Inspector ────────────────────────────────────────────────────────────

    [Header("Arrow Style")]
    [SerializeField] private Color arrowColor  = new Color(1f, 0.85f, 0.2f, 1f);
    [SerializeField] private float arrowSize   = 72f;

    [Header("Label")]
    [SerializeField] private Color labelColor  = new Color(1f, 0.9f, 0.5f, 1f);
    [SerializeField] private int   labelFontSize = 18;

    [Header("Pulse")]
    [SerializeField] private float pulseSpeed    = 2.5f;
    [SerializeField] private float pulseMinScale = 0.85f;
    [SerializeField] private float pulseMaxScale = 1.2f;

    [Header("Behaviour")]
    [Tooltip("Arrow hides when the player is this close to the target.")]
    [SerializeField] private float hideRadius  = 4f;
    [Tooltip("Distance in pixels from the screen edge.")]
    [SerializeField] private float edgePadding = 80f;

    // ── Runtime state ─────────────────────────────────────────────────────────

    private Transform      _playerTransform;
    private Camera         _camera;
    private GameObject     _arrowRoot;
    private RectTransform  _arrowRect;
    private Text           _labelText;
    private Text           _shadowText;
    private Vector3        _targetWorldPos;
    private bool           _active;
    private float          _pulseTimer;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _playerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;
        _camera = Camera.main;
        BuildArrow();
        SetVisible(false);
    }

    private void LateUpdate()
    {
        if (!_active || _arrowRoot == null) return;

        // Auto-hide when close enough
        if (_playerTransform != null &&
            (_playerTransform.position - _targetWorldPos).sqrMagnitude < hideRadius * hideRadius)
        {
            SetVisible(false);
            return;
        }

        // Pulse scale
        _pulseTimer += Time.deltaTime * pulseSpeed;
        float pulse = Mathf.Lerp(pulseMinScale, pulseMaxScale, (Mathf.Sin(_pulseTimer) + 1f) * 0.5f);
        _arrowRect.localScale = new Vector3(pulse, pulse, 1f);

        UpdateArrowTransform();
        SetVisible(true);
    }

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Point the arrow at a world-space position and show it.
    /// Replaces any previous target — only one arrow ever shows.
    /// </summary>
    public void SetTarget(Vector3 worldPosition, string label = "This way")
    {
        _targetWorldPos = worldPosition;
        _active         = true;
        _pulseTimer     = 0f;

        if (_labelText  != null) _labelText.text  = label;
        if (_shadowText != null) _shadowText.text  = label;
        SetVisible(true);
    }

    /// <summary>Hide the arrow immediately.</summary>
    public void Hide()
    {
        _active = false;
        SetVisible(false);
    }

    // ── Internals ─────────────────────────────────────────────────────────────

    private void UpdateArrowTransform()
    {
        if (_camera == null) _camera = Camera.main;
        if (_camera == null) return;

        Vector3 screenPos = _camera.WorldToScreenPoint(_targetWorldPos);

        // Flip if target is behind camera
        if (screenPos.z < 0f)
            screenPos = new Vector3(Screen.width - screenPos.x, Screen.height - screenPos.y, 0f);

        Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);
        Vector2 dir          = new Vector2(screenPos.x - screenCenter.x, screenPos.y - screenCenter.y);

        float halfW = screenCenter.x - edgePadding;
        float halfH = screenCenter.y - edgePadding;

        bool onScreen = Mathf.Abs(dir.x) < halfW && Mathf.Abs(dir.y) < halfH && screenPos.z > 0f;

        if (onScreen)
        {
            float s = Mathf.Min(halfW / (Mathf.Abs(dir.x) + 0.001f), halfH / (Mathf.Abs(dir.y) + 0.001f));
            _arrowRect.anchoredPosition = dir * Mathf.Min(s, 0.75f);
        }
        else
        {
            float s = Mathf.Min(halfW / (Mathf.Abs(dir.x) + 0.001f), halfH / (Mathf.Abs(dir.y) + 0.001f));
            _arrowRect.anchoredPosition = dir * s;
        }

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg - 90f;
        _arrowRect.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    private void SetVisible(bool visible)
    {
        if (_arrowRoot != null && _arrowRoot.activeSelf != visible)
            _arrowRoot.SetActive(visible);
    }

    private void BuildArrow()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[DirectionArrowUI] No Canvas found — arrow will not appear.");
            return;
        }

        _arrowRoot = new GameObject("DirectionArrow");
        _arrowRoot.transform.SetParent(canvas.transform, false);

        _arrowRect             = _arrowRoot.AddComponent<RectTransform>();
        _arrowRect.anchorMin   = new Vector2(0.5f, 0.5f);
        _arrowRect.anchorMax   = new Vector2(0.5f, 0.5f);
        _arrowRect.pivot       = new Vector2(0.5f, 0.5f);
        _arrowRect.sizeDelta   = new Vector2(arrowSize, arrowSize);
        _arrowRect.anchoredPosition = Vector2.zero;

        Image img         = _arrowRoot.AddComponent<Image>();
        img.color         = arrowColor;
        img.raycastTarget = false;
        img.sprite        = BuildArrowSprite();

        // Drop-shadow label (behind)
        _shadowText = CreateLabel("ArrowLabelShadow", new Vector2(1f, -5f),
                                  new Color(0f, 0f, 0f, 0.7f));
        // Foreground label
        _labelText  = CreateLabel("ArrowLabel", new Vector2(0f, -4f), labelColor);
    }

    private Text CreateLabel(string goName, Vector2 offset, Color color)
    {
        GameObject go = new GameObject(goName);
        go.transform.SetParent(_arrowRoot.transform, false);

        RectTransform r = go.AddComponent<RectTransform>();
        r.anchorMin        = new Vector2(0.5f, 0f);
        r.anchorMax        = new Vector2(0.5f, 0f);
        r.pivot            = new Vector2(0.5f, 1f);
        r.sizeDelta        = new Vector2(140f, 26f);
        r.anchoredPosition = offset;

        Text t = go.AddComponent<Text>();
        t.font          = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text          = "This way";
        t.fontSize      = labelFontSize;
        t.fontStyle     = FontStyle.Bold;
        t.color         = color;
        t.alignment     = TextAnchor.UpperCenter;
        t.raycastTarget = false;
        return t;
    }

    private static Sprite BuildArrowSprite()
    {
        const int size = 64;
        Texture2D tex    = new Texture2D(size, size, TextureFormat.RGBA32, false);
        Color[]   pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        for (int y = 0; y < size; y++)
        {
            float t         = y / (float)(size - 1);
            float halfWidth = t * (size * 0.5f - 2f) + 2f;
            int   cx        = size / 2;
            int   left      = Mathf.RoundToInt(cx - halfWidth);
            int   right     = Mathf.RoundToInt(cx + halfWidth);
            for (int x = left; x <= right; x++)
                if (x >= 0 && x < size) pixels[y * size + x] = Color.white;
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
    }
}
