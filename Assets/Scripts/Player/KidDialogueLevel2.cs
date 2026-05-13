using System.Collections;
using UnityEngine;
using Player;

/// <summary>
/// Handles the Level 2 little girl interaction.
/// The girl stands still — no jumpscare, no ghost. She simply speaks,
/// then calls back to Level2SequenceController when the conversation ends.
/// Replaces KidScareInteraction on the Level 2 kid GameObject.
/// </summary>
public class KidDialogueLevel2 : MonoBehaviour, IInteractable
{
    private static readonly DialogueData.Line[] ConversationLines = new[]
    {
        new DialogueData.Line { speaker = "You",         text = "Stay away from me." },
        new DialogueData.Line { speaker = "Little Girl", text = "You said that when I followed you." },
        new DialogueData.Line { speaker = "You",         text = "Followed me where?" },
        new DialogueData.Line { speaker = "Little Girl", text = "Into the alley." },
        new DialogueData.Line { speaker = "You",         text = "No. You were the thing chasing me." },
        new DialogueData.Line { speaker = "Little Girl", text = "I was trying to bring you home." },
        new DialogueData.Line { speaker = "You",         text = "You are lying." },
        new DialogueData.Line { speaker = "Little Girl", text = "That is the first lie. The second is that you forgot." },
        new DialogueData.Line { speaker = "You",         text = "What do you want from me?" },
        new DialogueData.Line { speaker = "Little Girl", text = "Wake up." },
        new DialogueData.Line { speaker = "You",         text = "From what?" },
        new DialogueData.Line { speaker = "Little Girl", text = "From the story where you are innocent." },
    };

    [Header("Settings")]
    [SerializeField] private string promptText = "[ F ] Talk";

    private bool _hasInteracted;
    private bool _isInDialogue;

    public string GetActionName() => "Talk";
    public InteractionKey GetPreferredKey() => InteractionKey.F;

    /// <summary>Available once and only while not already in dialogue.</summary>
    public bool CanInteract() => !_isInDialogue && !_hasInteracted;

    /// <summary>Starts the conversation with the little girl.</summary>
    public void OnInteract()
    {
        if (!CanInteract()) return;
        StartCoroutine(RunSequence());
    }

    private IEnumerator RunSequence()
    {
        _isInDialogue = true;

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

        Level2SequenceController.Instance?.OnGirlTalked();
    }
}
