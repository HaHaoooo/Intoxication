using UnityEngine;
using System.Collections;
using Player;

public class Level2SequenceController : MonoBehaviour
{
    public static Level2SequenceController Instance { get; private set; }

    [Header("NPCs")]
    [SerializeField] private GameObject bartender;
    [SerializeField] private GameObject homelessMan;
    [SerializeField] private GameObject kid;

    [Header("Dialogue Data")]
    [SerializeField] private DialogueData whisperDialogue;
    [SerializeField] private DialogueData girlVoiceDialogue;
    [SerializeField] private DialogueData finalMonologue;

    [Header("Scene 1–3 Dialogues")]
    [Tooltip("Scene 1: player reacts to the broken street at the start of Level 2.")]
    [SerializeField] private DialogueData streetReactionDialogue;
    [Tooltip("Scene 2: distorted echo voices heard mid-level.")]
    [SerializeField] private DialogueData echoVoicesDialogue;
    [Tooltip("Scene 3: player reacts when approaching the final bottle.")]
    [SerializeField] private DialogueData bottleApproachDialogue;

    [Header("Building States")]
    [SerializeField] private GameObject finalEmptyBarState;
    [SerializeField] private GameObject initialBarState;
    [Tooltip("Optional state containing additional broken/distorted building props " +
             "activated after the Scene 4 teleport.")]
    [SerializeField] private GameObject moreBrokenState;

    [Header("Scene 4 & 5 References")]
    [Tooltip("The BottleApproachDialogueTrigger on the bottle — reset after teleport.")]
    [SerializeField] private BottleApproachDialogueTrigger bottleApproachTrigger;

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
        ObjectiveUI.Instance?.Set(ObjectiveState.CheckSurroundings);
    }

    // ─── Scene 1: Street Reaction ────────────────────────────────────────────

    /// <summary>Triggered when the player first enters the broken street.</summary>
    public void TriggerStreetReaction()
    {
        if (DialogueUI.Instance == null || streetReactionDialogue == null) return;
        DialogueUI.Instance.OnDialogueEnded += OnStreetReactionEnded;
        DialogueUI.Instance.StartDialogue(streetReactionDialogue);
    }

    private void OnStreetReactionEnded()
    {
        DialogueUI.Instance.OnDialogueEnded -= OnStreetReactionEnded;
    }

    // ─── Scene 2: Echo Voices ────────────────────────────────────────────────

    /// <summary>Triggered mid-level while the player walks through distorted buildings.</summary>
    public void TriggerEchoVoices()
    {
        if (DialogueUI.Instance == null || echoVoicesDialogue == null) return;
        DialogueUI.Instance.OnDialogueEnded += OnEchoVoicesEnded;
        DialogueUI.Instance.StartDialogue(echoVoicesDialogue);
    }

    private void OnEchoVoicesEnded()
    {
        DialogueUI.Instance.OnDialogueEnded -= OnEchoVoicesEnded;
    }

    // ─── Scene 3: Bottle Approach ────────────────────────────────────────────

    /// <summary>Starts the bottle approach dialogue and invokes onComplete when it finishes.</summary>
    public void TriggerBottleApproach(System.Action onComplete)
    {
        if (DialogueUI.Instance == null || bottleApproachDialogue == null)
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
        DialogueUI.Instance.StartDialogue(bottleApproachDialogue);
    }

    // ─── Scene 4: Post-teleport reset ────────────────────────────────────────

    /// <summary>
    /// Activates the optional more-broken building state after the Scene 4 teleport.
    /// Assign GameObjects with additional broken/distorted props to moreBrokenState.
    /// </summary>
    public void ActivateMoreBrokenState()
    {
        if (moreBrokenState != null)
            moreBrokenState.SetActive(true);
    }

    /// <summary>
    /// Resets the bottle approach trigger after the Scene 4 teleport so Scene 3
    /// plays again on the second visit. Also relocks the bottle.
    /// </summary>
    public void ResetForSecondVisit()
    {
        if (bottleApproachTrigger != null)
            bottleApproachTrigger.ResetTrigger();
    }

    // ─── Existing sequences ──────────────────────────────────────────────────

    public void TriggerWhisper()
    {
        if (DialogueUI.Instance != null && whisperDialogue != null)
        {
            DialogueUI.Instance.OnDialogueEnded += OnWhisperEnded;
            DialogueUI.Instance.StartDialogue(whisperDialogue);
        }
    }

    private void OnWhisperEnded()
    {
        DialogueUI.Instance.OnDialogueEnded -= OnWhisperEnded;
        ObjectiveUI.Instance?.Set(ObjectiveState.TalkToBartender);
    }

    public void OnBartenderTalked()
    {
        ObjectiveUI.Instance?.Set(ObjectiveState.FindHomeless);
    }

    public void OnHomelessTalked()
    {
        StartCoroutine(VanishHomeless());
    }

    private IEnumerator VanishHomeless()
    {
        yield return new WaitForSeconds(1f);
        if (homelessMan != null) homelessMan.SetActive(false);
        ObjectiveUI.Instance?.Set(ObjectiveState.FollowVoice);
    }

    public void TriggerGirlVoice()
    {
        if (DialogueUI.Instance != null && girlVoiceDialogue != null)
        {
            DialogueUI.Instance.OnDialogueEnded += OnGirlVoiceEnded;
            DialogueUI.Instance.StartDialogue(girlVoiceDialogue);
        }
    }

    private void OnGirlVoiceEnded()
    {
        DialogueUI.Instance.OnDialogueEnded -= OnGirlVoiceEnded;
        if (kid != null) kid.SetActive(true);
    }

    public void OnGirlTalked()
    {
        if (kid != null) kid.SetActive(false);
        ObjectiveUI.Instance?.Set(ObjectiveState.LeaveAlley);
        PrepareFinalBar();
    }

    private void PrepareFinalBar()
    {
        if (initialBarState != null) initialBarState.SetActive(false);
        if (finalEmptyBarState != null) finalEmptyBarState.SetActive(true);
    }

    public void TriggerFinalMonologue()
    {
        if (DialogueUI.Instance != null && finalMonologue != null)
        {
            DialogueUI.Instance.StartDialogue(finalMonologue);
        }
    }
}
