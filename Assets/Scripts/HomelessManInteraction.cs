using Player;
using UnityEngine;

/// <summary>
/// Handles the homeless NPC interaction: starts the dialogue sequence,
/// switches to third-person camera during the conversation, and then plays
/// a short echo of the wife's voice before finishing.
/// Attach to the homeless GameObject (requires a Collider for raycast detection).
/// </summary>
public class HomelessManInteraction : MonoBehaviour, IInteractable, ILockable
{
    [Header("Bottle Spawn")]
    [Tooltip("The bottle prefab to instantiate after dialogue.")]
    [SerializeField] private GameObject bottlePrefab;

    [Tooltip("The Transform where the bottle will be placed.")]
    [SerializeField] private Transform bottleSpawnPoint;

    [Tooltip("Scale applied to the spawned bottle.")]
    [SerializeField] private Vector3 bottleScale = new Vector3(0.2f, 0.2f, 0.2f);

    [Header("State")]
    [Tooltip("If true, the interaction can only be triggered once.")]
    [SerializeField] private bool oneTimeOnly = true;

    private static readonly DialogueData.Line[] HomelessLines = new[]
    {
        new DialogueData.Line { speaker = "Homeless Man", text = "Leave." },
        new DialogueData.Line { speaker = "You",          text = "I am not here to hurt you." },
        new DialogueData.Line { speaker = "Homeless Man", text = "That is what you said before." },
        new DialogueData.Line { speaker = "You",          text = "Before what?" },
        new DialogueData.Line { speaker = "Homeless Man", text = "Before the screaming. Before the axe. Before you started calling it self-defense." },
        new DialogueData.Line { speaker = "You",          text = "I do not know what you are talking about." },
        new DialogueData.Line { speaker = "Homeless Man", text = "Of course you do not. That bottle always takes the memory first." },
        new DialogueData.Line { speaker = "You",          text = "I just need my keys. I need to get home." },
        new DialogueData.Line { speaker = "Homeless Man", text = "Home? You do not get to say that word anymore." },
    };

    private static readonly DialogueData.Line[] WifeLines = new[]
    {
        new DialogueData.Line { speaker = "Wife's Voice", text = "You promised this would be the last time." },
        new DialogueData.Line { speaker = "You",          text = "Who said that?" },
        new DialogueData.Line { speaker = "Wife's Voice", text = "She waited by the door." },
        new DialogueData.Line { speaker = "You",          text = "Stop." },
        new DialogueData.Line { speaker = "Wife's Voice", text = "I waited too." },
    };

    private static readonly DialogueData.Line[] PostBottleLines = new[]
    {
        new DialogueData.Line { speaker = "Homeless Man", text = "There it is. The easiest door out." },
        new DialogueData.Line { speaker = "You",          text = "I just need to calm down." },
    };

    private bool _hasInteracted;
    private bool _isInDialogue;
    private PlayerMovement _playerMovement;
    private DialogueUI _cachedDialogueUI;

    private void Awake()
    {
        _playerMovement = FindAnyObjectByType<PlayerMovement>();
    }

    private void OnDestroy()
    {
        if (_cachedDialogueUI != null)
        {
            _cachedDialogueUI.OnDialogueEnded -= HandleHomelessDialogueEnded;
            _cachedDialogueUI.OnDialogueEnded -= HandleWifeDialogueEnded;
            _cachedDialogueUI.OnDialogueEnded -= HandlePostBottleDialogueEnded;
        }
    }

    public string GetActionName() => "Talk";
    public InteractionKey GetPreferredKey() => InteractionKey.F;

    /// <summary>Requires the trash bin to have been interacted with first.</summary>
    public StorylineManager.Beat RequiredBeat => StorylineManager.Beat.TrashBinDone;

    /// <summary>Whether the homeless man is available for interaction.</summary>
    public bool CanInteract()
    {
        if (_isInDialogue) return false;
        if (oneTimeOnly && _hasInteracted) return false;
        if (StorylineManager.Instance != null && !StorylineManager.Instance.IsUnlocked(RequiredBeat)) return false;
        return true;
    }

    /// <summary>Starts the homeless man dialogue sequence.</summary>
    public void OnInteract()
    {
        if (!CanInteract()) return;

        _isInDialogue = true;

        if (_playerMovement != null)
            _playerMovement.ForceThirdPerson();

        if (_cachedDialogueUI == null)
            _cachedDialogueUI = DialogueUI.Instance;

        DialogueUI ui = _cachedDialogueUI;
        if (ui == null)
        {
            Debug.LogError("[HomelessManInteraction] DialogueUI instance not found.");
            _isInDialogue = false;
            return;
        }

        if (TryGetComponent<DialogueEventBridge>(out var bridge))
            bridge.RegisterOnce();

        DialogueData homelessData = ScriptableObject.CreateInstance<DialogueData>();
        homelessData.lines = HomelessLines;

        ui.OnDialogueEnded += HandleHomelessDialogueEnded;
        ui.StartDialogue(homelessData);

        Debug.Log("[HomelessManInteraction] Started dialogue with homeless man.");
    }

    /// <summary>
    /// Called when the homeless man's dialogue finishes.
    /// Chains into the wife's voice echo sequence.
    /// </summary>
    private void HandleHomelessDialogueEnded()
    {
        _cachedDialogueUI.OnDialogueEnded -= HandleHomelessDialogueEnded;

        DialogueData wifeData = ScriptableObject.CreateInstance<DialogueData>();
        wifeData.lines = WifeLines;

        _cachedDialogueUI.OnDialogueEnded += HandleWifeDialogueEnded;
        _cachedDialogueUI.StartDialogue(wifeData);

        Debug.Log("[HomelessManInteraction] Starting wife's voice echo.");
    }

    /// <summary>
    /// Called when the wife's voice echo finishes.
    /// Spawns the bottle, then plays the homeless man's reaction to it.
    /// </summary>
    private void HandleWifeDialogueEnded()
    {
        _cachedDialogueUI.OnDialogueEnded -= HandleWifeDialogueEnded;

        SpawnBottle();

        DialogueData postBottleData = ScriptableObject.CreateInstance<DialogueData>();
        postBottleData.lines = PostBottleLines;

        _cachedDialogueUI.OnDialogueEnded += HandlePostBottleDialogueEnded;
        _cachedDialogueUI.StartDialogue(postBottleData);

        Debug.Log("[HomelessManInteraction] Bottle spawned. Playing post-bottle lines.");
    }

    /// <summary>Called after the homeless man's final reaction — marks the full sequence as complete.</summary>
    private void HandlePostBottleDialogueEnded()
    {
        _cachedDialogueUI.OnDialogueEnded -= HandlePostBottleDialogueEnded;

        _isInDialogue = false;
        _hasInteracted = true;

        StorylineManager.Instance?.Advance(StorylineManager.Beat.HomelessDone);
        ObjectiveUI.Instance?.Set(ObjectiveState.LeaveAlley);
        Debug.Log("[HomelessManInteraction] Full sequence complete.");
    }

    /// <summary>Instantiates the bottle at the spawn point.</summary>
    private void SpawnBottle()
    {
        if (bottlePrefab == null)
        {
            Debug.LogWarning("[HomelessManInteraction] No bottle prefab assigned.");
            return;
        }

        Vector3 spawnPos = bottleSpawnPoint != null
            ? bottleSpawnPoint.position
            : transform.position + transform.forward * 1.2f + Vector3.up * 0.5f;

        Quaternion spawnRot = bottleSpawnPoint != null
            ? bottleSpawnPoint.rotation
            : Quaternion.identity;

        GameObject bottle = Instantiate(bottlePrefab, spawnPos, spawnRot);
        bottle.transform.localScale = bottleScale;
        bottle.SetActive(true);

        // Ensure the spawned bottle has the transition script
        LevelTransitionOnDrink transition = bottle.GetComponent<LevelTransitionOnDrink>();
        if (transition == null)
        {
            transition = bottle.AddComponent<LevelTransitionOnDrink>();
        }
        // transition.targetSceneName = "Level 2"; // It's already the default, but we could set it here if needed.

        Debug.Log($"[HomelessManInteraction] Bottle spawned at {spawnPos}.");
    }
}
