using System.Collections;
using UnityEngine;
using Player;

/// <summary>
/// Placed on the final bottle in Level 2.
///
/// Visit 1 (Scene 4):
///   Player presses F → pre-teleport dialogue → screen shake → fade to black
///   → player teleports back to Level 2 start → fade in → post-teleport dialogue.
///   The bottle relocks and BottleApproachDialogueTrigger resets so Scene 3 replays
///   on the second approach.
///
/// Visit 2 (Scene 5):
///   Player presses F again → break/reject dialogue → ending fade → quit.
/// </summary>
public class FinalBottleInteraction : MonoBehaviour, IInteractable, IBottleInteractionLock
{
    [Header("Dialogue")]
    [Tooltip("Scene 4 — dialogue right before the teleport.")]
    [SerializeField] private DialogueData preTeleportDialogue;

    [Tooltip("Scene 4 — dialogue after the player reappears at the start.")]
    [SerializeField] private DialogueData postTeleportDialogue;

    [Tooltip("Scene 5 — dialogue when the player breaks/rejects the bottle.")]
    [SerializeField] private DialogueData breakDialogue;

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

    // ── State ──────────────────────────────────────────────────────────────────
    private bool _isLocked = true;
    private bool _firstVisitDone = false;
    private bool _isRunning = false;

    // ── IInteractable ──────────────────────────────────────────────────────────

    public string GetActionName() => _firstVisitDone ? "Break it" : "Pick up";
    public InteractionKey GetPreferredKey() => InteractionKey.F;
    public bool CanInteract() => !_isLocked && !_isRunning;

    // ── IBottleInteractionLock ─────────────────────────────────────────────────

    /// <summary>Called by BottleApproachDialogueTrigger to lock/unlock interaction.</summary>
    public void SetLocked(bool locked)
    {
        _isLocked = locked;
    }

    // ── Interaction entry ──────────────────────────────────────────────────────

    public void OnInteract()
    {
        if (_isRunning) return;
        _isRunning = true;

        if (!_firstVisitDone)
            StartCoroutine(Scene4Routine());
        else
            StartCoroutine(Scene5Routine());
    }

    // ── Scene 4: shake → pre-dialogue → fade → teleport → fade in → post-dialogue ──

    private IEnumerator Scene4Routine()
    {
        // Lock the bottle immediately so the player can't spam F.
        _isLocked = true;

        // 1. Screen shake — let it run in background while dialogue plays.
        CameraShake shake = FindAnyObjectByType<CameraShake>();
        shake?.Shake(shakeMagnitude, shakeDuration);

        // 2. Pre-teleport dialogue.
        if (preTeleportDialogue != null && DialogueUI.Instance != null)
        {
            bool dialogueDone = false;
            void OnEnded() { DialogueUI.Instance.OnDialogueEnded -= OnEnded; dialogueDone = true; }
            DialogueUI.Instance.OnDialogueEnded += OnEnded;
            DialogueUI.Instance.StartDialogue(preTeleportDialogue);
            yield return new WaitUntil(() => dialogueDone);
        }

        // 3. Second shake on teleport.
        shake?.Shake(shakeMagnitude * 1.5f, shakeDuration * 1.2f);

        // 4. Fade to black.
        ScreenFadeUI.Instance.FadeToBlack(fadeOutDuration);
        yield return new WaitForSecondsRealtime(fadeOutDuration + 0.15f);

        // 5. Teleport player.
        TeleportPlayer();

        // 6. Activate "more broken" buildings state.
        Level2SequenceController.Instance?.ActivateMoreBrokenState();

        // 7. Brief pause fully black.
        yield return new WaitForSecondsRealtime(0.5f);

        // 8. Fade back in.
        ScreenFadeUI.Instance.FadeFromBlack(fadeInDuration);
        yield return new WaitForSecondsRealtime(fadeInDuration + 0.3f);

        // 9. Post-teleport dialogue.
        if (postTeleportDialogue != null && DialogueUI.Instance != null)
        {
            bool dialogueDone = false;
            void OnEnded() { DialogueUI.Instance.OnDialogueEnded -= OnEnded; dialogueDone = true; }
            DialogueUI.Instance.OnDialogueEnded += OnEnded;
            DialogueUI.Instance.StartDialogue(postTeleportDialogue);
            yield return new WaitUntil(() => dialogueDone);
        }

        // 10. Mark first visit done; the BottleApproachDialogueTrigger will reset itself
        //     and re-lock the bottle — we relock here as safety.
        _firstVisitDone = true;
        _isRunning = false;

        // Tell the sequence controller to reset the approach trigger for the second visit.
        Level2SequenceController.Instance?.ResetForSecondVisit();

        // Set objective: Break the bottle.
        ObjectiveUI.Instance?.Set(ObjectiveState.BreakBottle);
    }

    // ── Scene 5: break dialogue → ending ──────────────────────────────────────

    private IEnumerator Scene5Routine()
    {
        _isLocked = true;

        // Break dialogue.
        if (breakDialogue != null && DialogueUI.Instance != null)
        {
            bool dialogueDone = false;
            void OnEnded() { DialogueUI.Instance.OnDialogueEnded -= OnEnded; dialogueDone = true; }
            DialogueUI.Instance.OnDialogueEnded += OnEnded;
            DialogueUI.Instance.StartDialogue(breakDialogue);
            yield return new WaitUntil(() => dialogueDone);
        }

        // Ending fade.
        ScreenFadeUI.Instance.FadeToBlack(3f);
        yield return new WaitForSecondsRealtime(4f);

        Debug.Log("ENDING: Sometimes the scariest thing is not what you see. It is what your mind creates when you lose control.");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

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
