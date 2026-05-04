using System.Collections;
using UnityEngine;

/// <summary>
/// Handles ghost-specific health behaviour: destroys the ghost GameObject on death,
/// then plays a post-fight monologue followed by a whispered echo from the little girl.
/// Requires a HealthComponent on the same GameObject.
/// </summary>
[RequireComponent(typeof(HealthComponent))]
public class GhostHealthController : MonoBehaviour
{
    private static readonly DialogueData.Line[] PostFightLines = new[]
    {
        new DialogueData.Line { speaker = "You",         text = "What did I just do?" },
        new DialogueData.Line { speaker = "You",         text = "No... no, that wasn't real." },
        new DialogueData.Line { speaker = "Little Girl", text = "You always say that." },
    };

    private HealthComponent _health;

    private void Awake()
    {
        _health = GetComponent<HealthComponent>();
    }

    private void Start()
    {
        _health.OnDeath += HandleDeath;
    }

    private void HandleDeath()
    {
        // Hide health bars immediately
        CombatHealthBarUI.Instance?.Hide();

        // Destroy this ghost then run the dialogue sequence on a persistent host
        Destroy(gameObject);

        // Find a persistent MonoBehaviour to host the coroutine
        DialogueUI ui = DialogueUI.Instance;
        if (ui != null)
            ui.StartCoroutine(PlayPostFightSequence(ui));
        else
            ObjectiveUI.Instance?.Set(ObjectiveState.FollowSound);
        RadioTriggerGate.Open();
    }

    private static IEnumerator PlayPostFightSequence(DialogueUI ui)
    {
        bool done = false;

        DialogueData data = ScriptableObject.CreateInstance<DialogueData>();
        data.lines = PostFightLines;

        ui.OnDialogueEnded += OnDone;
        ui.StartDialogue(data);

        void OnDone() { done = true; ui.OnDialogueEnded -= OnDone; }
        yield return new WaitUntil(() => done);

        ObjectiveUI.Instance?.Set(ObjectiveState.FollowSound);
        RadioTriggerGate.Open();
    }
}
