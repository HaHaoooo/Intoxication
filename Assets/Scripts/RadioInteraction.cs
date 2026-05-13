using System.Collections;
using UnityEngine;

/// <summary>
/// Attach to the radio GameObject.
/// When the player walks within triggerRadius, the radio "turns on by itself"
/// and plays a sequence of news broadcast lines followed by the player's reactions.
/// One-shot — fires exactly once per play session.
/// Requires ObjectiveState.FollowSound to already be active before triggering,
/// preventing the radio from firing too early.
/// </summary>
public class RadioInteraction : MonoBehaviour
{
    private const float CheckInterval = 0.3f;   // seconds between proximity checks

    private static readonly DialogueData.Line[] RadioLines = new[]
    {
        new DialogueData.Line { speaker = "Radio", text = "Breaking news tonight. Police are still searching for the suspect connected to the recent serial murder case." },
        new DialogueData.Line { speaker = "You",   text = "Turn it off." },
        new DialogueData.Line { speaker = "Radio", text = "Witnesses report that the suspect may appear intoxicated, unstable, and unable to remember recent events." },
        new DialogueData.Line { speaker = "You",   text = "Shut up." },
        new DialogueData.Line { speaker = "Radio", text = "Authorities warn citizens to avoid dark alleyways and report any suspicious behavior." },
        new DialogueData.Line { speaker = "You",   text = "I said shut up." },
        new DialogueData.Line { speaker = "Radio", text = "The suspect may be carrying a sharp object. Sources say he may have had a wife and young daughter." },
        new DialogueData.Line { speaker = "You",   text = "That is enough!" },
    };

    private static readonly DialogueData.Line[] AfterRadioLines = new[]
    {
        new DialogueData.Line { speaker = "You", text = "That is not me." },
        new DialogueData.Line { speaker = "You", text = "That cannot be me." },
    };

    [Header("Trigger Settings")]
    [Tooltip("Distance at which the radio activates (player enters this radius).")]
    [SerializeField] private float triggerRadius = 6f;

    [Tooltip("Only fire when the current objective is FollowSound or later. Prevents premature activation.")]
    [SerializeField] private bool requireFollowSoundObjective = true;

    [Header("Audio (optional)")]
    [Tooltip("Optional static/on sound played when the radio switches on.")]
    [SerializeField] private AudioClip radioOnClip;

    private bool _hasTriggered;
    private Transform _playerTransform;
    private AudioSource _audioSource;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null && radioOnClip != null)
        {
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 1f;
            _audioSource.playOnAwake = false;
        }

        // Poll for the player on a timer to avoid per-frame FindAnyObjectByType
        StartCoroutine(ProximityPoller());
    }

    private IEnumerator ProximityPoller()
    {
        WaitForSeconds wait = new WaitForSeconds(CheckInterval);

        while (!_hasTriggered)
        {
            if (_playerTransform == null)
            {
                var pm = FindAnyObjectByType<Player.PlayerMovement>();
                if (pm != null) _playerTransform = pm.transform;
            }

            if (_playerTransform != null)
            {
                float dist = Vector3.Distance(transform.position, _playerTransform.position);
                if (dist <= triggerRadius && IsObjectiveReady())
                {
                    _hasTriggered = true;
                    StartCoroutine(PlayRadioSequence());
                    yield break;
                }
            }

            yield return wait;
        }
    }

    /// <summary>Returns true if objective prerequisites are met.</summary>
    private bool IsObjectiveReady()
    {
        // If we don't require a specific state, always fire
        if (!requireFollowSoundObjective) return true;

        // ObjectiveUI tracks the current state via Set(); we can't read it back easily,
        // so we rely on a simple static flag set by GhostHealthController.
        return RadioTriggerGate.IsReady;
    }

    private IEnumerator PlayRadioSequence()
    {
        if (_audioSource != null && radioOnClip != null)
            _audioSource.PlayOneShot(radioOnClip);

        DialogueUI ui = DialogueUI.Instance;
        if (ui == null) yield break;

        // Radio broadcast
        yield return RunDialogue(ui, RadioLines);

        // Poster reaction (the "face flickers" are handled visually by MissingPosterFlicker
        // on the MissingChildPoster — we just play the player's reaction here)
        yield return RunDialogue(ui, AfterRadioLines);

        ObjectiveUI.Instance?.Set(ObjectiveState.CheckNoise);
        Debug.Log("[RadioInteraction] Sequence complete — CheckNoise objective set.");
    }

    /// <summary>Plays a set of lines via DialogueUI and waits until they finish.</summary>
    private static IEnumerator RunDialogue(DialogueUI ui, DialogueData.Line[] lines)
    {
        bool done = false;
        DialogueData data = ScriptableObject.CreateInstance<DialogueData>();
        data.lines = lines;

        ui.OnDialogueEnded += OnDone;
        ui.StartDialogue(data);

        void OnDone() { done = true; ui.OnDialogueEnded -= OnDone; }
        yield return new WaitUntil(() => done);
    }
}

/// <summary>
/// Static gate set by GhostHealthController after the ghost dies.
/// Lets RadioInteraction know it is safe to activate.
/// </summary>
public static class RadioTriggerGate
{
    public static bool IsReady { get; private set; }

    /// <summary>Call this when the FollowSound objective becomes active.</summary>
    public static void Open() => IsReady = true;
}
