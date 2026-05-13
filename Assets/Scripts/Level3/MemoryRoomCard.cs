using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A small world-space card that floats above a story object in the memory room.
/// It displays a short title and description, and optionally shows dialogue when examined.
/// The card is always visible so the player can read the context without interaction.
/// </summary>
[RequireComponent(typeof(ExaminableClue))]
public class MemoryRoomCard : MonoBehaviour
{
    private const float CardWidth       = 220f;
    private const float CardHeight      = 70f;
    private const float HeightAboveBase = 1.2f;
    private const float CardScale       = 0.004f;

    [Header("Card Label")]
    [Tooltip("Short title shown in bold at the top of the card.")]
    [SerializeField] private string cardTitle = "Memory";

    [Header("Card Offset")]
    [Tooltip("Additional Y offset above the object pivot.")]
    [SerializeField] private float yOffset = 0f;

    private Canvas    _canvas;
    private Transform _cameraTransform;

    private void Start()
    {
        _cameraTransform = Camera.main?.transform;
        BuildCard();
    }

    private void LateUpdate()
    {
        // Always face the camera so the card stays readable from any angle.
        if (_canvas != null && _cameraTransform != null)
            _canvas.transform.forward = _cameraTransform.forward;
    }

    // ── Card construction ─────────────────────────────────────────────────────

    private void BuildCard()
    {
        // World-space Canvas attached directly to this GameObject's position
        GameObject canvasGO = new GameObject($"Card_{gameObject.name}");
        canvasGO.transform.SetParent(transform, false);
        canvasGO.transform.localPosition = new Vector3(0f, HeightAboveBase + yOffset, 0f);
        canvasGO.transform.localRotation = Quaternion.identity;
        canvasGO.transform.localScale    = Vector3.one * CardScale;

        _canvas = canvasGO.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.WorldSpace;

        RectTransform canvasRect  = canvasGO.GetComponent<RectTransform>();
        canvasRect.sizeDelta      = new Vector2(CardWidth, CardHeight);

        // Background panel
        GameObject panel = new GameObject("Panel");
        panel.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin      = Vector2.zero;
        panelRect.anchorMax      = Vector2.one;
        panelRect.offsetMin      = Vector2.zero;
        panelRect.offsetMax      = Vector2.zero;

        Image bg  = panel.AddComponent<Image>();
        bg.color  = new Color(0f, 0f, 0f, 0.72f);
        bg.raycastTarget = false;

        // Title text
        AddText(canvasGO.transform, "Title",
            cardTitle,
            FontStyle.Bold,
            fontSize:  18,
            anchorMin: new Vector2(0f, 0.52f),
            anchorMax: new Vector2(1f, 1f),
            offset:    new Vector2(8f, -4f));

        // Examine hint
        AddText(canvasGO.transform, "Hint",
            "[F] to examine",
            FontStyle.Italic,
            fontSize:  11,
            anchorMin: new Vector2(0f, 0f),
            anchorMax: new Vector2(1f, 0.45f),
            offset:    new Vector2(8f, 2f));
    }

    private static void AddText(Transform parent, string name, string content,
                                FontStyle style, int fontSize,
                                Vector2 anchorMin, Vector2 anchorMax, Vector2 offset)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform r = go.AddComponent<RectTransform>();
        r.anchorMin = anchorMin;
        r.anchorMax = anchorMax;
        r.offsetMin = new Vector2(offset.x, offset.y);
        r.offsetMax = new Vector2(-offset.x, -offset.y);

        Text t = go.AddComponent<Text>();
        t.font          = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.text          = content;
        t.fontSize      = fontSize;
        t.fontStyle     = style;
        t.color         = Color.white;
        t.alignment     = TextAnchor.UpperLeft;
        t.horizontalOverflow = HorizontalWrapMode.Wrap;
        t.verticalOverflow   = VerticalWrapMode.Overflow;
        t.raycastTarget = false;
    }
}
