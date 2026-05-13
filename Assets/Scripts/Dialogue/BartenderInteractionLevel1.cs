using System.Collections;
using UnityEngine;
using Player;

/// <summary>
/// Handles the Level 1 bartender interaction with fully inlined dialogue.
/// Replaces BartenderInteraction on the Level 1 bartender GameObject.
/// Advances the storyline when the conversation ends.
/// </summary>
public class BartenderInteractionLevel1 : MonoBehaviour, IInteractable
{
    private static readonly DialogueData.Line[] ConversationLines = new[]
    {
        new DialogueData.Line { speaker = "Bartender", text = "Welcome back, sir." },
        new DialogueData.Line { speaker = "You",       text = "Hey." },
        new DialogueData.Line { speaker = "Bartender", text = "Sorry for the self service habit. What kind of drink can I get for you?" },
        new DialogueData.Line { speaker = "You",       text = "Give me something strong. It has been a rough day." },
        new DialogueData.Line { speaker = "Bartender", text = "Rough how?" },
        new DialogueData.Line { speaker = "You",       text = "Just... noise in my head. Hard to explain." },
        new DialogueData.Line { speaker = "Bartender", text = "I see that a lot in here." },
        new DialogueData.Line { speaker = "Bartender", text = "You know, there has been some strange talk going around the area lately." },
        new DialogueData.Line { speaker = "You",       text = "What kind of talk?" },
        new DialogueData.Line { speaker = "Bartender", text = "A man. People say he has been wandering around at night." },
        new DialogueData.Line { speaker = "Bartender", text = "Nobody knows who he is. But he has been spotted near the alley more than once." },
        new DialogueData.Line { speaker = "You",       text = "Sounds like just a rumor." },
        new DialogueData.Line { speaker = "Bartender", text = "Maybe. But rumors come from somewhere." },
        new DialogueData.Line { speaker = "Bartender", text = "Here. On the house tonight." },
        new DialogueData.Line { speaker = "You",       text = "Thanks." },
        new DialogueData.Line { speaker = "Bartender", text = "And sir..." },
        new DialogueData.Line { speaker = "You",       text = "Yeah?" },
        new DialogueData.Line { speaker = "Bartender", text = "Do not walk home alone if you can help it." },
        new DialogueData.Line { speaker = "You",       text = "I will keep that in mind." },
    };

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

    /// <summary>Starts the bartender conversation and advances the storyline when done.</summary>
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
            Debug.LogError("[BartenderInteractionLevel1] DialogueUI instance not found.");
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

        StorylineManager.Instance?.Advance(StorylineManager.Beat.BartenderDone);
        Debug.Log("[BartenderInteractionLevel1] Dialogue complete.");
    }
}
