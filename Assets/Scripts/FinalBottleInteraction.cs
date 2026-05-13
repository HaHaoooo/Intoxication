using System.Collections;
using UnityEngine;
using Player;

/// <summary>
/// Placed on the final bottle in Level 2.
///
/// Visit 1 (Scene 7 — first half):
///   Player presses F → echo dialogue → shake → fade to black → teleport to level start
///   → fade in → post-crack dialogue.
///   The bottle relocks and BottleApproachDialogueTrigger resets so the approach
///   plays again on the second walk-up.
///
/// Visit 2 (Scene 8):
///   Player presses F again → break dialogue → ending fade → quit.
/// </summary>
public class FinalBottleInteraction : MonoBehaviour, IInteractable, IBottleInteractionLock
{
    // ── Inlined dialogue ──────────────────────────────────────────────────────

    // Scene 7 — overlapping echoes when the player first presses F on the bottle.
    private static readonly DialogueData.Line[] PreTeleportLines = new[]
    {
        new DialogueData.Line { speaker = "Bartender Echo",    text = "Here you are. This is our signature." },
        new DialogueData.Line { speaker = "Radio Echo",        text = "The suspect may be carrying a sharp object." },
        new DialogueData.Line { speaker = "Wife's Voice",      text = "Please come home." },
        new DialogueData.Line { speaker = "Homeless Man Echo", text = "The bottle gives the street a voice." },
    };

    // Scene 7 — player reacts after cracking the bottle and reappearing at the start.
    private static readonly DialogueData.Line[] PostCrackLines = new[]
    {
        new DialogueData.Line { speaker = "You",               text = "I broke it." },
        new DialogueData.Line { speaker = "Little Girl Voice", text = "You cracked it." },
        new DialogueData.Line { speaker = "You",               text = "Why am I still here?" },
        new DialogueData.Line { speaker = "Little Girl Voice", text = "Because now you have to look inside." },
    };

    // Scene 8 — second press: player rejects the bottle, game ends.
    private static readonly DialogueData.Line[] BreakLines = new[]
    {
        new DialogueData.Line { speaker = "You",               text = "I don't need this." },
        new DialogueData.Line { speaker = "Little Girl Voice", text = "Then look at what you did." },
    };

    // ── Inspector fields ──────────────────────────────────────────────────────

    [Header("Teleport")]
    [Tooltip("Where the player is sent on reset. Matches the Level 2 spawn position.")]
    [SerializeField] private Vector3 teleportPosition = new Vector3(-12.1f, 0.17f, 36.9f);

    [Tooltip("Player rotation after teleport (Y euler degrees).")]
    [SerializeField] private float teleportYaw = 90.5f;

    [Header("Shake")]
    [SerializeField] private float shakeMagnitude = 0.25f;
    [SerializeField] private float shakeDuration = 0.8f;

    [Header("Fade")]
    [SerializeField] private float fadeOutDuration = 1.2f;
    [SerializeField] private float fadeInDuration = 1.0f;

    // ── State ─────────────────────────────────────────────────────────────────

    private bool _isLocked = true;
    private bool _firstVisitDone = false;
    private bool _isRunning = false;

    // ── IInteractable ─────────────────────────────────────────────────────────

    public string GetActionName() => _firstVisitDone ? "Break it" : "Pick up";
    public InteractionKey GetPreferredKey() => InteractionKey.F;
    public bool CanInteract() => !_isLocked && !_isRunning;

    // ── IBottleInteractionLock ────────────────────────────────────────────────

    /// <summary>Called by BottleApproachDialogueTrigger to lock/unlock interaction.</summary>
    public void SetLocked(bool locked) => _isLocked = locked;

    // ── Interaction entry ─────────────────────────────────────────────────────

    public void OnInteract()
    {
        if (_isRunning) return;
        _isRunning = true;

        if (!_firstVisitDone)
            StartCoroutine(Scene7FirstHalfRoutine());
        else
            StartCoroutine(Scene8Routine());
    }

    // ── Scene 7 — first half ──────────────────────────────────────────────────

    private IEnumerator Scene7FirstHalfRoutine()
    {
        _isLocked = true;

        // 1. Shake starts in background while echo dialogue plays.
        CameraShake shake = FindAnyObjectByType<CameraShake>();
        shake?.Shake(shakeMagnitude, shakeDuration);

        // 2. Echo dialogue.
        yield return RunDialogue(PreTeleportLines);

        // 3. Heavier shake just before blackout.
        shake?.Shake(shakeMagnitude * 1.5f, shakeDuration * 1.2f);

        // 4. Fade to black.
        ScreenFadeUI.Instance.FadeToBlack(fadeOutDuration);
        yield return new WaitForSecondsRealtime(fadeOutDuration + 0.15f);

        // 5. Teleport player to level start.
        TeleportPlayer();

        // 6. Brief fully-black pause.
        yield return new WaitForSecondsRealtime(0.5f);

        // 7. Fade back in.
        ScreenFadeUI.Instance.FadeFromBlack(fadeInDuration);
        yield return new WaitForSecondsRealtime(fadeInDuration + 0.3f);

        // 8. Post-crack dialogue.
        yield return RunDialogue(PostCrackLines);

        // 9. Mark first visit done and reset for second approach.
        _firstVisitDone = true;
        _isRunning = false;

        Level2SequenceController.Instance?.ResetForSecondVisit();
        ObjectiveUI.Instance?.Set(ObjectiveState.BreakBottle);
    }

    // ── Scene 8 ───────────────────────────────────────────────────────────────

    private IEnumerator Scene8Routine()
    {
        _isLocked = true;

        yield return RunDialogue(BreakLines);

        ScreenFadeUI.Instance.FadeToBlack(3f);
        yield return new WaitForSecondsRealtime(4f);

        Debug.Log("ENDING: Sometimes the scariest thing is not what you see. It is what your mind creates when you lose control.");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>Runs a set of lines via DialogueUI and waits until the player advances through all of them.</summary>
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

    private void TeleportPlayer()
    {
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogWarning("[FinalBottleInteraction] Could not find Player for teleport.");
            return;
        }

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        player.transform.SetPositionAndRotation(
            teleportPosition,
            Quaternion.Euler(0f, teleportYaw, 0f)
        );

        if (cc != null) cc.enabled = true;

        Debug.Log("[FinalBottleInteraction] Player teleported to level start.");
    }
}
