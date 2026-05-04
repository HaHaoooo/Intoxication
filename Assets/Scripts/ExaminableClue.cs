using UnityEngine;

/// <summary>
/// A one-time-readable world object (drawing, poster, note, etc.).
/// When the player interacts, it shows a short flavour line in DialogueUI
/// and optionally plays a monologue line from the main character afterwards.
/// </summary>
public class ExaminableClue : MonoBehaviour, IInteractable
{
    [Header("Prompt")]
    [Tooltip("Text shown on the interaction prompt (e.g. '[ F ] Examine').")]
    [SerializeField] private string prompt = "[ F ] Examine";

    [Header("Clue Text")]
    [Tooltip("The environmental description shown when examined (e.g. 'A torn child's drawing lies on the ground.').")]
    [SerializeField] private string clueDescription = "";

    [Header("Monologue")]
    [Tooltip("Optional line spoken by the main character after the clue description. Leave empty to skip.")]
    [SerializeField] private string monologueLine = "";

    [Tooltip("Speaker name used for the monologue line (shown in the dialogue panel).")]
    [SerializeField] private string monologueSpeaker = "You";

    [Header("State")]
    [Tooltip("If true, this clue can only be examined once.")]
    [SerializeField] private bool oneTimeOnly = true;

    private bool _hasBeenRead;
    private DialogueUI _dialogueUI;

    private void Awake()
    {
        _dialogueUI = DialogueUI.Instance;
    }

    /// <summary>Returns the interaction prompt shown when the player looks at this clue.</summary>
    public string GetInteractionPrompt() => prompt;

    /// <summary>Whether this clue can still be examined.</summary>
    public bool CanInteract() => !(oneTimeOnly && _hasBeenRead);

    /// <summary>Shows the clue text (and optional monologue) via DialogueUI.</summary>
    public void OnInteract()
    {
        if (!CanInteract()) return;

        if (oneTimeOnly)
            _hasBeenRead = true;

        if (_dialogueUI == null)
            _dialogueUI = DialogueUI.Instance;

        if (_dialogueUI == null)
        {
            Debug.LogWarning("[ExaminableClue] DialogueUI not found.");
            return;
        }

        DialogueData data = BuildDialogueData();
        _dialogueUI.StartDialogue(data);

        Debug.Log($"[ExaminableClue] Examined: {gameObject.name}");
    }

    /// <summary>Builds a temporary DialogueData from the clue fields — no asset required.</summary>
    private DialogueData BuildDialogueData()
    {
        DialogueData data = ScriptableObject.CreateInstance<DialogueData>();

        bool hasDescription = !string.IsNullOrWhiteSpace(clueDescription);
        bool hasMonologue = !string.IsNullOrWhiteSpace(monologueLine);

        int lineCount = (hasDescription ? 1 : 0) + (hasMonologue ? 1 : 0);
        data.lines = new DialogueData.Line[lineCount];

        int i = 0;
        if (hasDescription)
        {
            data.lines[i++] = new DialogueData.Line
            {
                speaker = "",
                text = clueDescription
            };
        }

        if (hasMonologue)
        {
            data.lines[i] = new DialogueData.Line
            {
                speaker = monologueSpeaker,
                text = monologueLine
            };
        }

        return data;
    }
}
