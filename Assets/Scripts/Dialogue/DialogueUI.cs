using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Manages the dialogue panel UI. Auto-creates a child panel under the Canvas.
/// The DialogueUI component itself stays on an always-active parent so Update runs reliably.
/// Displays lines one at a time; the player advances by pressing LMB or Space.
/// Fires events when dialogue starts and ends.
/// </summary>
public class DialogueUI : MonoBehaviour
{
    [Header("UI References (auto-created if null)")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Text speakerText;
    [SerializeField] private Text bodyText;
    [SerializeField] private Text hintText;

    [Header("Panel Style")]
    [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.75f);
    [SerializeField] private int speakerFontSize = 22;
    [SerializeField] private int bodyFontSize = 18;
    [SerializeField] private int hintFontSize = 14;
    [SerializeField] private float panelHeight = 160f;

    /// <summary>True while a dialogue sequence is actively displayed.</summary>
    public bool IsActive { get; private set; }

    /// <summary>Fired when a dialogue sequence begins.</summary>
    public event Action OnDialogueStarted;

    /// <summary>Fired when the last line is dismissed.</summary>
    public event Action OnDialogueEnded;

    private DialogueData _currentData;
    private int _currentLineIndex;
    private Coroutine _typingCoroutine;
    private bool _isTyping;
    private string _fullText;
    private readonly StringBuilder _typewriterBuffer = new StringBuilder(256);
    private WaitForSecondsRealtime _typewriterWait;

    private static DialogueUI _instance;

    [Header("Typewriter Settings")]
    [SerializeField] private float charactersPerSecond = 30f;

    private float _lastCps = -1f;

    // Tracks how many dialogue sessions are open so nested chains don't
    // restore time-scale until the outermost dialogue is fully finished.
    private int _freezeDepth;

    /// <summary>Returns the singleton instance, creating one if needed.</summary>
    public static DialogueUI Instance
    {
        get
        {
            if (_instance != null) return _instance;

            _instance = FindAnyObjectByType<DialogueUI>();
            if (_instance != null) return _instance;

            Canvas canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[DialogueUI] No Canvas found in the scene.");
                return null;
            }

            // Create the manager object directly under Canvas (always active)
            GameObject managerGO = new GameObject("DialogueUIManager");
            managerGO.transform.SetParent(canvas.transform, false);
            // Add a RectTransform so it's a valid UI object but invisible and zero-sized
            RectTransform managerRect = managerGO.AddComponent<RectTransform>();
            managerRect.sizeDelta = Vector2.zero;

            _instance = managerGO.AddComponent<DialogueUI>();
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;

        if (dialoguePanel == null)
        {
            BuildUI();
        }

        dialoguePanel.SetActive(false);
    }

    private void OnDestroy()
    {
        // Clear static reference to prevent stale references after scene transitions.
        if (_instance == this)
            _instance = null;
    }

    private void Update()
    {
        if (!IsActive) return;

        bool advance = (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                     || (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame);

        if (advance)
        {
            if (_isTyping)
            {
                // Skip typing and show full text
                if (_typingCoroutine != null)
                {
                    StopCoroutine(_typingCoroutine);
                    _typingCoroutine = null;
                }
                bodyText.text = _fullText;
                _isTyping = false;
            }
            else
            {
                // Move to next line
                AdvanceLine();
            }
        }
    }

    /// <summary>Begins displaying a dialogue sequence from the first line.</summary>
    public void StartDialogue(DialogueData data)
    {
        if (data == null || data.lines == null || data.lines.Length == 0)
        {
            Debug.LogWarning("[DialogueUI] DialogueData is empty or null.");
            return;
        }

        _currentData = data;
        _currentLineIndex = 0;
        IsActive = true;

        // Freeze game time so nothing moves or attacks during dialogue.
        // Increment depth so chained dialogues don't double-unfreeze.
        _freezeDepth++;
        Time.timeScale = 0f;

        dialoguePanel.SetActive(true);
        DisplayCurrentLine();

        OnDialogueStarted?.Invoke();
        Debug.Log("[DialogueUI] Dialogue started.");
    }

    /// <summary>Advances to the next line, or ends the dialogue if on the last line.</summary>
    private void AdvanceLine()
    {
        _currentLineIndex++;

        if (_currentLineIndex >= _currentData.lines.Length)
        {
            EndDialogue();
            return;
        }

        DisplayCurrentLine();
    }

    /// <summary>Displays the current line's speaker and text with a typewriter effect.</summary>
    private void DisplayCurrentLine()
    {
        DialogueData.Line line = _currentData.lines[_currentLineIndex];

        if (speakerText != null)
            speakerText.text = line.speaker;

        _fullText = line.text;
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }
        _typingCoroutine = StartCoroutine(TypeTextRoutine(line.speaker, _fullText));
    }

    private IEnumerator TypeTextRoutine(string speaker, string fullText)
    {
        _isTyping = true;
        _typewriterBuffer.Clear();
        bodyText.text = "";

        DialogueVoiceController voice = DialogueVoiceController.GetByName(speaker);
        int phonemeFrequency = voice != null ? voice.GetPlayFrequency() : 2;

        // Use real-time wait so typewriter keeps running when timeScale == 0.
        if (Mathf.Abs(charactersPerSecond - _lastCps) > 0.01f)
        {
            _lastCps = charactersPerSecond;
            _typewriterWait = new WaitForSecondsRealtime(1f / Mathf.Max(1f, charactersPerSecond));
        }

        for (int i = 0; i < fullText.Length; i++)
        {
            _typewriterBuffer.Append(fullText[i]);
            bodyText.text = _typewriterBuffer.ToString();

            if (voice != null && i % phonemeFrequency == 0 && !char.IsWhiteSpace(fullText[i]))
            {
                voice.PlayPhoneme();
            }

            yield return _typewriterWait;
        }

        _isTyping = false;
    }

    /// <summary>Closes the dialogue panel and fires the ended event.</summary>
    private void EndDialogue()
    {
        // Stop any in-progress typing coroutine before clearing state.
        if (_typingCoroutine != null)
        {
            StopCoroutine(_typingCoroutine);
            _typingCoroutine = null;
        }
        _isTyping = false;

        IsActive = false;
        _currentData = null;
        _currentLineIndex = 0;

        dialoguePanel.SetActive(false);

        // Restore time only when no more dialogue layers are open.
        _freezeDepth = Mathf.Max(0, _freezeDepth - 1);
        if (_freezeDepth == 0)
            Time.timeScale = 1f;

        OnDialogueEnded?.Invoke();
        Debug.Log("[DialogueUI] Dialogue ended.");
    }

    /// <summary>
    /// Builds the dialogue panel as a SEPARATE child GameObject.
    /// This way, deactivating the panel does not deactivate this component's Update.
    /// </summary>
    private void BuildUI()
    {
        // Create a child panel object — this is what gets activated/deactivated
        GameObject panelGO = new GameObject("DialoguePanel");
        panelGO.transform.SetParent(transform.parent, false); // Sibling under Canvas

        RectTransform panelRect = panelGO.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 0f);
        panelRect.anchorMax = new Vector2(1f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(0f, panelHeight);

        Image bg = panelGO.AddComponent<Image>();
        bg.color = panelColor;

        dialoguePanel = panelGO;

        // Speaker name
        GameObject speakerGO = CreateTextChild("SpeakerName", panelRect,
            new Vector2(0f, 0.7f), new Vector2(1f, 1f),
            new Vector2(30f, 0f), new Vector2(-30f, -10f));
        speakerText = speakerGO.GetComponent<Text>();
        speakerText.fontSize = speakerFontSize;
        speakerText.fontStyle = FontStyle.Bold;
        speakerText.color = new Color(1f, 0.85f, 0.4f);
        speakerText.alignment = TextAnchor.LowerLeft;

        // Body text
        GameObject bodyGO = CreateTextChild("BodyText", panelRect,
            new Vector2(0f, 0.15f), new Vector2(1f, 0.7f),
            new Vector2(30f, 0f), new Vector2(-30f, 0f));
        bodyText = bodyGO.GetComponent<Text>();
        bodyText.fontSize = bodyFontSize;
        bodyText.color = Color.white;
        bodyText.alignment = TextAnchor.UpperLeft;

        // Hint text
        GameObject hintGO = CreateTextChild("HintText", panelRect,
            new Vector2(0f, 0f), new Vector2(1f, 0.15f),
            new Vector2(30f, 5f), new Vector2(-30f, 0f));
        hintText = hintGO.GetComponent<Text>();
        hintText.fontSize = hintFontSize;
        hintText.color = new Color(0.7f, 0.7f, 0.7f);
        hintText.alignment = TextAnchor.MiddleRight;
        hintText.text = "[ Click / Space ] Continue";
    }

    /// <summary>Helper to create a Text child with specified anchors and offsets.</summary>
    private GameObject CreateTextChild(string name, RectTransform parent,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);

        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;

        Text text = go.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        return go;
    }
}
