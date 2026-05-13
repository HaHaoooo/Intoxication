using UnityEngine;
using System.Collections;
using Player;

public class Level2SequenceController : MonoBehaviour
{
    public static Level2SequenceController Instance { get; private set; }

    /// <summary>True once the homeless man has vanished. Enables alley bottles for drinking.</summary>
    public bool HomelessHasLeft { get; private set; }

    // ─── Inlined dialogue ─────────────────────────────────────────────────────

    // Scene 1: Unknown Voice whisper on level start.
    private static readonly DialogueData.Line[] WhisperLines = new[]
    {
        new DialogueData.Line { speaker = "You",           text = "No... I was just here." },
        new DialogueData.Line { speaker = "Unknown Voice", text = "You never left." },
        new DialogueData.Line { speaker = "You",           text = "Who is there?" },
        new DialogueData.Line { speaker = "Unknown Voice", text = "You came back because you chose to." },
        new DialogueData.Line { speaker = "You",           text = "I did not choose anything." },
        new DialogueData.Line { speaker = "Unknown Voice", text = "That is what makes the loop easy." },
    };

    // Scene 2: Distorted street — little girl's voice through the rain.
    private static readonly DialogueData.Line[] StreetReactionLines = new[]
    {
        new DialogueData.Line { speaker = "You",              text = "What the hell happened to this place?" },
        new DialogueData.Line { speaker = "Little Girl Voice", text = "You said that last time." },
        new DialogueData.Line { speaker = "You",              text = "Last time?" },
        new DialogueData.Line { speaker = "Little Girl Voice", text = "You always keep walking away." },
        new DialogueData.Line { speaker = "You",              text = "Away from what?" },
        new DialogueData.Line { speaker = "Little Girl Voice", text = "From the door you were supposed to open." },
        new DialogueData.Line { speaker = "You",              text = "I am not listening to this." },
        new DialogueData.Line { speaker = "Little Girl Voice", text = "You never do." },
    };

    // Mid-level echo voices.
    private static readonly DialogueData.Line[] EchoVoicesLines = new[]
    {
        new DialogueData.Line { speaker = "Bartender Echo",    text = "Drink slowly, sir." },
        new DialogueData.Line { speaker = "Radio Echo",        text = "The suspect may appear confused." },
        new DialogueData.Line { speaker = "Homeless Man Echo", text = "That bottle always takes the memory first." },
        new DialogueData.Line { speaker = "Little Girl Echo",  text = "You promised." },
        new DialogueData.Line { speaker = "Wife's Voice",      text = "Please come home." },
        new DialogueData.Line { speaker = "You",               text = "Stop. Stop talking." },
        new DialogueData.Line { speaker = "Little Girl Voice", text = "Then stop running." },
        new DialogueData.Line { speaker = "You",               text = "I did not do anything." },
        new DialogueData.Line { speaker = "Little Girl Voice", text = "Then why are you afraid to remember?" },
    };

    // Player reaction when spotting the final bottle.
    private static readonly DialogueData.Line[] BottleApproachLines = new[]
    {
        new DialogueData.Line { speaker = "You",               text = "It is the same bottle." },
        new DialogueData.Line { speaker = "Little Girl Voice", text = "Pick it up." },
        new DialogueData.Line { speaker = "You",               text = "No. This started after I drank it." },
        new DialogueData.Line { speaker = "Little Girl Voice", text = "It started before that." },
    };

    // Final monologue near the end of the street.
    private static readonly DialogueData.Line[] FinalMonologueLines = new[]
    {
        new DialogueData.Line { speaker = "You", text = "It is the same bottle." },
        new DialogueData.Line { speaker = "You", text = "No... This started after I drank it." },
        new DialogueData.Line { speaker = "You", text = "Maybe it started before that." },
    };

    // ─── Inspector fields ─────────────────────────────────────────────────────

    [Header("NPCs")]
    [SerializeField] private GameObject bartender;
    [SerializeField] private GameObject homelessMan;
    [SerializeField] private GameObject kid;

    [Header("Girl Voice Dialogue (asset — before the kid becomes visible)")]
    [SerializeField] private DialogueData girlVoiceDialogue;

    [Header("Building States")]
    [SerializeField] private GameObject finalEmptyBarState;
    [SerializeField] private GameObject initialBarState;

    [Header("Scene 4 & 5 References")]
    [Tooltip("The BottleApproachDialogueTrigger on the bottle — reset after teleport.")]
    [SerializeField] private BottleApproachDialogueTrigger bottleApproachTrigger;

    // ─── Lifecycle ────────────────────────────────────────────────────────────

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        ObjectiveUI.Instance?.Set(ObjectiveState.None);
        StartCoroutine(DelayedStartObjective());
    }

    private IEnumerator DelayedStartObjective()
    {
        yield return new WaitForSeconds(2f);
        ObjectiveUI.Instance?.Set(ObjectiveState.ReturnToBar);
    }

    // ─── Scene 1: Whisper ────────────────────────────────────────────────────

    /// <summary>Triggered when the player first spawns into Level 2.</summary>
    public void TriggerWhisper()
    {
        if (DialogueUI.Instance == null) return;
        DialogueUI.Instance.OnDialogueEnded += OnWhisperEnded;
        DialogueUI.Instance.StartDialogue(MakeData(WhisperLines));
    }

    private void OnWhisperEnded()
    {
        DialogueUI.Instance.OnDialogueEnded -= OnWhisperEnded;
        ObjectiveUI.Instance?.Set(ObjectiveState.TalkToBartender);
    }

    // ─── Scene 2: Distorted Street ───────────────────────────────────────────

    /// <summary>Triggered when the player enters the distorted street zone.</summary>
    public void TriggerStreetReaction()
    {
        if (DialogueUI.Instance == null) return;
        DialogueUI.Instance.OnDialogueEnded += OnStreetReactionEnded;
        DialogueUI.Instance.StartDialogue(MakeData(StreetReactionLines));
    }

    private void OnStreetReactionEnded()
    {
        DialogueUI.Instance.OnDialogueEnded -= OnStreetReactionEnded;
    }

    // ─── Mid-level: Echo Voices ──────────────────────────────────────────────

    /// <summary>Triggered mid-level while the player walks through distorted buildings.</summary>
    public void TriggerEchoVoices()
    {
        if (DialogueUI.Instance == null) return;
        DialogueUI.Instance.OnDialogueEnded += OnEchoVoicesEnded;
        DialogueUI.Instance.StartDialogue(MakeData(EchoVoicesLines));
    }

    private void OnEchoVoicesEnded()
    {
        DialogueUI.Instance.OnDialogueEnded -= OnEchoVoicesEnded;
    }

    // ─── Bottle Approach ─────────────────────────────────────────────────────

    /// <summary>Starts the bottle approach dialogue and invokes onComplete when it finishes.</summary>
    public void TriggerBottleApproach(System.Action onComplete)
    {
        if (DialogueUI.Instance == null)
        {
            onComplete?.Invoke();
            return;
        }

        void Handler()
        {
            DialogueUI.Instance.OnDialogueEnded -= Handler;
            onComplete?.Invoke();
        }

        DialogueUI.Instance.OnDialogueEnded += Handler;
        DialogueUI.Instance.StartDialogue(MakeData(BottleApproachLines));
    }

    // ─── Final Monologue ─────────────────────────────────────────────────────

    /// <summary>Plays the final monologue when the player nears the end of the street.</summary>
    public void TriggerFinalMonologue()
    {
        if (DialogueUI.Instance == null) return;
        DialogueUI.Instance.StartDialogue(MakeData(FinalMonologueLines));
    }

    // ─── NPC Callbacks ───────────────────────────────────────────────────────

    /// <summary>Called by the Level 2 bartender after its dialogue ends.</summary>
    public void OnBartenderTalked()
    {
        ObjectiveUI.Instance?.Set(ObjectiveState.FindHomeless);
    }

    /// <summary>Called by the Level 2 homeless man after its dialogue ends.</summary>
    public void OnHomelessTalked()
    {
        StartCoroutine(VanishHomeless());
    }

    private IEnumerator VanishHomeless()
    {
        yield return new WaitForSeconds(1f);
        if (homelessMan != null) homelessMan.SetActive(false);
        HomelessHasLeft = true;
        ObjectiveUI.Instance?.Set(ObjectiveState.DrinkBottle);
    }

    /// <summary>Plays the girl voice asset, then reveals the kid NPC.</summary>
    public void TriggerGirlVoice()
    {
        if (DialogueUI.Instance == null || girlVoiceDialogue == null) return;
        DialogueUI.Instance.OnDialogueEnded += OnGirlVoiceEnded;
        DialogueUI.Instance.StartDialogue(girlVoiceDialogue);
    }

    private void OnGirlVoiceEnded()
    {
        DialogueUI.Instance.OnDialogueEnded -= OnGirlVoiceEnded;
        if (kid != null) kid.SetActive(true);
    }

    /// <summary>Called by the little girl NPC after its dialogue ends.</summary>
    public void OnGirlTalked()
    {
        if (kid != null) kid.SetActive(false);
        ObjectiveUI.Instance?.Set(ObjectiveState.LeaveAlley);
        PrepareFinalBar();
    }

    // ─── Scene 4 & 5 ─────────────────────────────────────────────────────────

    /// <summary>Resets the bottle approach trigger for the second visit.</summary>
    public void ResetForSecondVisit()
    {
        if (bottleApproachTrigger != null)
            bottleApproachTrigger.ResetTrigger();
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private void PrepareFinalBar()
    {
        if (initialBarState != null) initialBarState.SetActive(false);
        if (finalEmptyBarState != null) finalEmptyBarState.SetActive(true);
    }

    /// <summary>Wraps a line array into a transient DialogueData instance.</summary>
    private static DialogueData MakeData(DialogueData.Line[] lines)
    {
        DialogueData data = ScriptableObject.CreateInstance<DialogueData>();
        data.lines = lines;
        return data;
    }
}
