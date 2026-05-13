using System.Collections;
using UnityEngine;
using Player;

/// <summary>
/// Handles the Level 2 homeless man interaction.
/// Inlines the Scene 6 dialogue and notifies Level2SequenceController when done.
/// Replace HomelessManInteraction on the Level 2 homeless man GameObject with this.
/// </summary>
public class HomelessManDialogueLevel2 : MonoBehaviour, IInteractable
{
    private static readonly DialogueData.Line[] ConversationLines = new[]
    {
        new DialogueData.Line { speaker = "You",          text = "Do you remember me?" },
        new DialogueData.Line { speaker = "Homeless Man", text = "You came back faster this time." },
        new DialogueData.Line { speaker = "You",          text = "What do you mean, this time?" },
        new DialogueData.Line { speaker = "Homeless Man", text = "You always ask the same questions." },
        new DialogueData.Line { speaker = "You",          text = "I just need to know what is happening." },
        new DialogueData.Line { speaker = "Homeless Man", text = "Then stop looking at the street." },
        new DialogueData.Line { speaker = "You",          text = "What?" },
        new DialogueData.Line { speaker = "Homeless Man", text = "Look at yourself." },
        new DialogueData.Line { speaker = "You",          text = "You are not making sense." },
        new DialogueData.Line { speaker = "Homeless Man", text = "That is what they all say before everything disappears." },
        new DialogueData.Line { speaker = "You",          text = "Who disappears?" },
        new DialogueData.Line { speaker = "Homeless Man", text = "Everyone you made up. The bartender. The girl. Me." },
        new DialogueData.Line { speaker = "You",          text = "No. You are real. I am talking to you right now." },
        new DialogueData.Line { speaker = "Homeless Man", text = "Are you?" },
        new DialogueData.Line { speaker = "You",          text = "What are you supposed to be?" },
        new DialogueData.Line { speaker = "Homeless Man", text = "What is left when a man keeps choosing the bottle instead of the truth." },
    };

    [Header("Settings")]
    [SerializeField] private string promptText = "[ F ] Talk";

    private bool _hasInteracted;
    private bool _isInDialogue;
    private PlayerMovement _playerMovement;

    private void Awake()
    {
        _playerMovement = FindAnyObjectByType<PlayerMovement>();
    }

    public string GetActionName() => "Talk";
    public InteractionKey GetPreferredKey() => InteractionKey.F;

    /// <summary>Available once and only while not already in dialogue.</summary>
    public bool CanInteract() => !_isInDialogue && !_hasInteracted;

    /// <summary>Starts the conversation with the homeless man.</summary>
    public void OnInteract()
    {
        if (!CanInteract()) return;
        StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        _isInDialogue = true;

        if (_playerMovement != null)
            _playerMovement.ForceThirdPerson();

        DialogueUI ui = DialogueUI.Instance;
        if (ui == null)
        {
            _isInDialogue = false;
            yield break;
        }

        bool done = false;

        DialogueData data = ScriptableObject.CreateInstance<DialogueData>();
        data.lines = ConversationLines;

        ui.OnDialogueEnded += OnDone;
        ui.StartDialogue(data);

        void OnDone() { done = true; ui.OnDialogueEnded -= OnDone; }
        yield return new WaitUntil(() => done);

        _isInDialogue = false;
        _hasInteracted = true;

        Level2SequenceController.Instance?.OnHomelessTalked();
    }
}
