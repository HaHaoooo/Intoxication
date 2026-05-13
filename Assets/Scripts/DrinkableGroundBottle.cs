using System.Collections;
using UnityEngine;

/// <summary>
/// Attached to each alley bottle that surrounds the Level 2 homeless man.
/// The bottle becomes drinkable only after the homeless man has vanished
/// (Level2SequenceController.HomelessHasLeft == true).
///
/// When the player presses F:
///   1. A short inner monologue plays.
///   2. The screen fades to black.
///   3. Level 3 loads.
/// </summary>
public class DrinkableGroundBottle : MonoBehaviour, IInteractable
{
    // ── Inlined dialogue ──────────────────────────────────────────────────────

    private static readonly DialogueData.Line[] DrinkLines = new[]
    {
        new DialogueData.Line { speaker = "You",           text = "Just one sip. To steady myself." },
        new DialogueData.Line { speaker = "Unknown Voice", text = "That is what you always say." },
        new DialogueData.Line { speaker = "You",           text = "I remember now. The house... the family." },
        new DialogueData.Line { speaker = "Unknown Voice", text = "Go back. Face it." },
    };

    // ── Inspector ─────────────────────────────────────────────────────────────

    [Header("Transition")]
    [Tooltip("Name of the scene to load after drinking.")]
    [SerializeField] private string targetScene = "Level 3";

    [Tooltip("Duration of the fade-to-black before the scene loads.")]
    [SerializeField] private float fadeDuration = 2f;

    // ── State ─────────────────────────────────────────────────────────────────

    private bool _isRunning;

    // ── IInteractable ─────────────────────────────────────────────────────────

    /// <summary>Prompt shown when the player looks at the bottle.</summary>
    public string GetActionName() => "Drink";

    public InteractionKey GetPreferredKey() => InteractionKey.F;

    /// <summary>Only available once the homeless man has left the scene.</summary>
    public bool CanInteract()
    {
        if (_isRunning) return false;
        return Level2SequenceController.Instance != null &&
               Level2SequenceController.Instance.HomelessHasLeft;
    }

    /// <summary>Starts the drink sequence.</summary>
    public void OnInteract()
    {
        if (!CanInteract()) return;
        StartCoroutine(DrinkSequence());
    }

    // ── Sequence ──────────────────────────────────────────────────────────────

    private IEnumerator DrinkSequence()
    {
        _isRunning = true;

        // Hide all other bottle prompts while the sequence runs
        ObjectiveUI.Instance?.Set(ObjectiveState.None);

        // 1. Play the drink monologue.
        yield return RunDialogue(DrinkLines);

        // 2. Fade to black and load Level 3.
        ScreenFadeUI.Instance?.FadeToBlack(fadeDuration, () =>
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene);
        });
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    /// <summary>Plays a dialogue sequence and waits for the player to advance through all lines.</summary>
    private static IEnumerator RunDialogue(DialogueData.Line[] lines)
    {
        DialogueUI ui = DialogueUI.Instance;
        if (ui == null) yield break;

        bool done = false;
        DialogueData data = ScriptableObject.CreateInstance<DialogueData>();
        data.lines = lines;

        ui.OnDialogueEnded += OnDone;
        ui.StartDialogue(data);

        void OnDone() { done = true; ui.OnDialogueEnded -= OnDone; }
        yield return new WaitUntil(() => done);
    }
}
