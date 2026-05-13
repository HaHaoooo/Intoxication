using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// All possible objective states in sequential order.
/// </summary>
public enum ObjectiveState
{
    FindAWayHome,       // Start of game
    CheckSurroundings,  // After bartender dialogue
    FindProtection,     // After little girl scare
    UseAxe,             // After picking up the axe
    FollowSound,        // After defeating the ghost
    CheckNoise,         // After radio / poster sequence
    LeaveAlley,         // After homeless man dialogue
    TalkToBartender,    // Level 2
    FindHomeless,       // Level 2
    FollowVoice,        // Level 2
    ReturnToBar,        // Level 2
    BreakBottle,        // Level 2 — second visit to the bottle
    None                // Hide UI
}

/// <summary>
/// Singleton UI controller that displays the current game objective
/// at the top-left of the screen. Auto-builds its own panel under any Canvas.
/// Call Set() from any system to update the displayed objective.
/// </summary>
public class ObjectiveUI : MonoBehaviour
{
    public static ObjectiveUI Instance { get; private set; }

    [Header("Style (used only when auto-building)")]
    [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.55f);
    [SerializeField] private Color labelColor = new Color(0.75f, 0.75f, 0.75f, 1f);
    [SerializeField] private Color objectiveColor = Color.white;
    [SerializeField] private int labelFontSize = 13;
    [SerializeField] private int objectiveFontSize = 16;

    private Text _labelText;
    private Text _objectiveText;
    private GameObject _panel;

    private static readonly string[] ObjectiveMessages =
    {
        "Find a way home.",
        "Check your surroundings.",
        "Find something to protect yourself.",
        "Use the axe to stop the creature.",
        "Follow the sound from the street.",
        "Check the noise near the trash cans.",
        "Leave the alley.",
        "Talk to the bartender.",
        "Find the man near the trash can.",
        "Follow the sound of the girl.",
        "Return to the bar one last time.",
        "Break the bottle."
    };

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        BuildUI();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Updates the displayed objective to match the given state.</summary>
    public void Set(ObjectiveState state)
    {
        if (state == ObjectiveState.None)
        {
            if (_panel != null) _panel.SetActive(false);
            return;
        }

        int index = (int)state;
if (index < 0 || index >= ObjectiveMessages.Length)
        {
            Debug.LogWarning($"[ObjectiveUI] No message defined for state {state}.");
            return;
        }

        if (_objectiveText != null)
            _objectiveText.text = ObjectiveMessages[index];

        if (_panel != null && !_panel.activeSelf)
            _panel.SetActive(true);

        Debug.Log($"[ObjectiveUI] Objective updated: {ObjectiveMessages[index]}");
    }

    // -------------------------------------------------------------------------
    // UI Construction
    // -------------------------------------------------------------------------

    private void BuildUI()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[ObjectiveUI] No Canvas found — objective UI skipped.");
            return;
        }

        // Root panel — top-left anchor, fixed size
        _panel = new GameObject("ObjectivePanel");
        _panel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = _panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.sizeDelta = new Vector2(280f, 56f);
        panelRect.anchoredPosition = new Vector2(16f, -16f);

        Image bg = _panel.AddComponent<Image>();
        bg.color = panelColor;
        bg.raycastTarget = false;

        // "OBJECTIVE" label
        GameObject labelGO = new GameObject("Label");
        labelGO.transform.SetParent(_panel.transform, false);

        RectTransform labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin = new Vector2(0f, 1f);
        labelRect.anchorMax = new Vector2(1f, 1f);
        labelRect.pivot = new Vector2(0f, 1f);
        labelRect.sizeDelta = new Vector2(0f, 18f);
        labelRect.anchoredPosition = new Vector2(10f, -6f);

        _labelText = labelGO.AddComponent<Text>();
        _labelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _labelText.text = "OBJECTIVE";
        _labelText.fontSize = labelFontSize;
        _labelText.fontStyle = FontStyle.Bold;
        _labelText.color = labelColor;
        _labelText.alignment = TextAnchor.UpperLeft;
        _labelText.raycastTarget = false;

        // Objective body text
        GameObject bodyGO = new GameObject("ObjectiveText");
        bodyGO.transform.SetParent(_panel.transform, false);

        RectTransform bodyRect = bodyGO.AddComponent<RectTransform>();
        bodyRect.anchorMin = new Vector2(0f, 0f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.offsetMin = new Vector2(10f, 6f);
        bodyRect.offsetMax = new Vector2(-10f, -24f);

        _objectiveText = bodyGO.AddComponent<Text>();
        _objectiveText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _objectiveText.text = ObjectiveMessages[0];
        _objectiveText.fontSize = objectiveFontSize;
        _objectiveText.color = objectiveColor;
        _objectiveText.alignment = TextAnchor.UpperLeft;
        _objectiveText.horizontalOverflow = HorizontalWrapMode.Wrap;
        _objectiveText.verticalOverflow = VerticalWrapMode.Overflow;
        _objectiveText.raycastTarget = false;
    }
}
