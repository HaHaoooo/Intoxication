using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Player;

/// <summary>
/// Master sequence controller for Level 3: "The Last Home."
///
/// Two-beat story flow:
///   Beat 1 — Foyer    : player enters through the front door, directed to the left bedroom.
///   Beat 2 — Bedroom  : player enters the family memory room; axe-drop gate; final bottle choice.
///
/// The kitchen, hallway corridor, final bar, and interrogation room are NOT part of the flow.
/// </summary>
public class Level3SequenceController : MonoBehaviour
{
    public static Level3SequenceController Instance { get; private set; }

    // ── Waypoints ─────────────────────────────────────────────────────────────

    [Header("Waypoints")]
    [Tooltip("Target shown after the player enters the foyer — points at the bedroom door.")]
    [SerializeField] private Vector3 waypointBedroomDoor = new Vector3(2.77f, 3.0f, 54.8f);

    // ── Foyer dialogue ────────────────────────────────────────────────────────

    private static readonly DialogueData.Line[] FoyerLines = new[]
    {
        new DialogueData.Line { speaker = "You",           text = "I know this place." },
        new DialogueData.Line { speaker = "Unknown Voice", text = "You have always known." },
        new DialogueData.Line { speaker = "You",           text = "The room on the left. I remember it." },
        new DialogueData.Line { speaker = "Unknown Voice", text = "Go. Remember." },
    };

    // ── Bedroom dialogue (plays on entry, before the axe-drop gate) ──────────

    private static readonly DialogueData.Line[] BedroomLines = new[]
    {
        new DialogueData.Line { speaker = "Wife's Voice",  text = "You promised this would be the last time." },
        new DialogueData.Line { speaker = "Child's Memory", text = "You said you were coming home." },
        new DialogueData.Line { speaker = "You",            text = "I thought I was running from something." },
        new DialogueData.Line { speaker = "Child's Memory", text = "No. You were running from what you did." },
        new DialogueData.Line { speaker = "You",            text = "I remember now." },
        new DialogueData.Line { speaker = "Wife's Voice",  text = "Then choose." },
    };

    // ── True ending sentences (shown full-screen, no dialogue box) ────────────

    private static readonly string[] TrueEndingSentences = new[]
    {
        "PLAYER: No more running.",
        "PLAYER: I remember now.",
        "PLAYER: I cannot change what happened.",
        "PLAYER: But I can stop hiding from it.",
        "THE NIGHT ENDS WHEN THE TRUTH IS FACED.",
    };

    // ── Bad ending sentence ───────────────────────────────────────────────────

    private static readonly string[] BadEndingSentences = new[]
    {
        "You close your eyes.",
        "You choose to forget again.",
    };

    // ── Scene names ───────────────────────────────────────────────────────────

    [Header("Scene Names")]
    [Tooltip("Scene to load for the true ending (main menu or Level 1).")]
    [SerializeField] private string trueEndingScene = "Menu";
    [Tooltip("Scene to reload for the bad ending — loop restarts from the bar.")]
    [SerializeField] private string badEndingScene = "Level 1";

    // ── State ─────────────────────────────────────────────────────────────────

    private bool _foyerDone;
    private bool _bedroomDone;

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    // ── Public trigger points ─────────────────────────────────────────────────

    /// <summary>Called by the FoyerTrigger SceneEventTrigger when the player enters the house.</summary>
    public void OnEnterFoyer()
    {
        if (_foyerDone) return;
        _foyerDone = true;
        ObjectiveUI.Instance?.Set(ObjectiveState.Level3FindBedroom);
        DirectionArrowUI.Instance?.SetTarget(waypointBedroomDoor, "← Bedroom");
        StartCoroutine(RunDialogue(FoyerLines));
    }

    /// <summary>Called by the BedroomTrigger SceneEventTrigger when the player enters the bedroom.</summary>
    public void OnEnterBedroom()
    {
        if (_bedroomDone) return;
        _bedroomDone = true;
        DirectionArrowUI.Instance?.Hide();
        StartCoroutine(BedroomSequence());
    }

    // No-ops for removed triggers — prevents errors from lingering references:
    public void OnEnterHallway()  { }
    public void OnEnterKitchen()  { }
    public void OnEnterFinalRoom(){ }

    // ── Bedroom flow ──────────────────────────────────────────────────────────

    private IEnumerator BedroomSequence()
    {
        // 1. Set axe-drop objective and play memory dialogue.
        ObjectiveUI.Instance?.Set(ObjectiveState.Level3PutDownAxe);

        yield return RunDialogue(BedroomLines);

        // 2. Wait until the player has dropped the axe (no longer holding it).
        yield return WaitForAxeDrop();

        // 3. Unlock the final bottle.
        ObjectiveUI.Instance?.Set(ObjectiveState.Level3FaceTheTruth);
        Level3FinalBottle.Instance?.Unlock();
    }

    /// <summary>
    /// Polls every frame until the player is no longer holding the axe.
    /// Displays the put-down-axe objective the entire time.
    /// </summary>
    private IEnumerator WaitForAxeDrop()
    {
        PlayerPickupController pickup = FindAnyObjectByType<PlayerPickupController>();
        if (pickup == null) yield break;

        // Wait until the player drops whatever they are holding.
        while (pickup.IsHoldingItem)
            yield return null;
    }

    // ── Ending callbacks (from Level3FinalBottle) ─────────────────────────────

    /// <summary>Called by Level3FinalBottle when the player breaks the bottle.</summary>
    public void OnBottleBroken()
    {
        StartCoroutine(TrueEndingRoutine());
    }

    /// <summary>Called by Level3FinalBottle when the player drinks again.</summary>
    public void OnBottleDrunk()
    {
        StartCoroutine(BadEndingRoutine());
    }

    // ── Ending routines ───────────────────────────────────────────────────────

    private IEnumerator TrueEndingRoutine()
    {
        ObjectiveUI.Instance?.Set(ObjectiveState.None);

        // Show full-screen ending sentences, then load the true-ending scene.
        bool done = false;
        EndingTextUI.Instance.Show(TrueEndingSentences, () => done = true);
        yield return new WaitUntil(() => done);

        yield return new WaitForSecondsRealtime(0.5f);
        SceneManager.LoadScene(trueEndingScene);
    }

    private IEnumerator BadEndingRoutine()
    {
        ObjectiveUI.Instance?.Set(ObjectiveState.None);

        // Show the single bad-ending sentence, then reload from Level 1.
        bool done = false;
        EndingTextUI.Instance.Show(BadEndingSentences, () => done = true);
        yield return new WaitUntil(() => done);

        yield return new WaitForSecondsRealtime(0.5f);

        // Hide the EndingTextUI black overlay before the scene loads,
        // then let ScreenFadeUI handle a clean fade-in once Level 1 is ready.
        EndingTextUI.Instance.gameObject.SetActive(false);
        ScreenFadeUI.Instance.SetBlack();

        // Subscribe to sceneLoaded so we can fade in after Level 1 finishes loading.
        SceneManager.sceneLoaded += OnLevel1Loaded;
        SceneManager.LoadScene(badEndingScene);
    }

    private void OnLevel1Loaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnLevel1Loaded;
        ScreenFadeUI.Instance.FadeFromBlack(1.5f);
    }

    // ── Shared dialogue helper ────────────────────────────────────────────────

    private IEnumerator RunDialogue(DialogueData.Line[] lines, System.Action onComplete = null)
    {
        DialogueUI ui = DialogueUI.Instance;
        if (ui == null) { onComplete?.Invoke(); yield break; }

        bool done = false;
        DialogueData data = ScriptableObject.CreateInstance<DialogueData>();
        data.lines = lines;

        ui.OnDialogueEnded += OnDone;
        ui.StartDialogue(data);

        void OnDone() { done = true; ui.OnDialogueEnded -= OnDone; }
        yield return new WaitUntil(() => done);

        onComplete?.Invoke();
    }
}
