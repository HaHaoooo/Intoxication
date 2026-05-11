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

    [Header("States")]
    [SerializeField] private GameObject finalEmptyBarState;
    [SerializeField] private GameObject initialBarState;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Initial setup for Level 2
        ObjectiveUI.Instance?.Set(ObjectiveState.None);
        // Start Objective
        StartCoroutine(DelayedStartObjective());
    }

    private IEnumerator DelayedStartObjective()
    {
        yield return new WaitForSeconds(2f);
        ObjectiveUI.Instance?.Set(ObjectiveState.CheckSurroundings); 
        // Using CheckSurroundings as a placeholder for "Look around"
    }

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
        // Need to add this to ObjectiveState enum or use a custom one
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
        // Using LeaveAlley as "Return to bar"
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
