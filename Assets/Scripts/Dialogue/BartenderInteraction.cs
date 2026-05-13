using Player;
using UnityEngine;

/// <summary>
/// Handles bartender NPC interaction for both Level 1 and Level 2.
/// All dialogue is inlined — no DialogueData assets required.
///
/// Level 1: spawnBottleAfterDialogue = false → plays Level1Lines, advances StorylineManager.
/// Level 2: spawnBottleAfterDialogue = true  → plays Level2Lines, spawns bottle, notifies Level2SequenceController.
/// </summary>
public class BartenderInteraction : MonoBehaviour, IInteractable, ILockable
{
    // ─── Inlined Level 1 dialogue (used when dialogueData is not assigned) ────

    private static readonly DialogueData.Line[] Level1Lines = new[]
    {
        new DialogueData.Line { speaker = "Bartender", text = "Welcome back, sir." },
        new DialogueData.Line { speaker = "You",       text = "Back?" },
        new DialogueData.Line { speaker = "Bartender", text = "Sorry. Force of habit. What can I get you?" },
        new DialogueData.Line { speaker = "You",       text = "Something strong. The best bottle you have." },
        new DialogueData.Line { speaker = "Bartender", text = "Rough night?" },
        new DialogueData.Line { speaker = "You",       text = "I just need to get home." },
        new DialogueData.Line { speaker = "Bartender", text = "Home is a dangerous word tonight." },
        new DialogueData.Line { speaker = "You",       text = "What does that mean?" },
        new DialogueData.Line { speaker = "Bartender", text = "There has been another murder nearby. Police say the suspect may be drunk, confused, and carrying a sharp object." },
        new DialogueData.Line { speaker = "You",       text = "Sounds like the kind of thing they always say on the news." },
        new DialogueData.Line { speaker = "Bartender", text = "Maybe. But men like that do not always plan things. Sometimes they just pick up whatever is near them." },
        new DialogueData.Line { speaker = "You",       text = "Like what?" },
        new DialogueData.Line { speaker = "Bartender", text = "A bottle. A pipe. An axe. Anything becomes a weapon when a man stops seeing people clearly." },
        new DialogueData.Line { speaker = "You",       text = "You talk like you know him." },
        new DialogueData.Line { speaker = "Bartender", text = "No, sir. I talk like I know you." },
        new DialogueData.Line { speaker = "Bartender", text = "Drink slowly. Some people forget because they drink. Some people drink because they remember." },
        new DialogueData.Line { speaker = "You",       text = "I only need enough to calm down." },
        new DialogueData.Line { speaker = "Bartender", text = "That is what everyone says before they leave something behind." },
    };

    // ─── Inlined Level 2 dialogue ─────────────────────────────────────────────

    private static readonly DialogueData.Line[] Level2Lines = new[]
    {
        new DialogueData.Line { speaker = "Bartender", text = "Welcome. What can I get you, sir?" },
        new DialogueData.Line { speaker = "You",       text = "You already asked me that." },
        new DialogueData.Line { speaker = "Bartender", text = "Did I?" },
        new DialogueData.Line { speaker = "You",       text = "You gave me a drink. You told me about the murderer." },
        new DialogueData.Line { speaker = "Bartender", text = "No, sir. You told me." },
        new DialogueData.Line { speaker = "You",       text = "No, I did not." },
        new DialogueData.Line { speaker = "Bartender", text = "You came in shaking. You said someone was following you. You said there was blood on your hands." },
        new DialogueData.Line { speaker = "You",       text = "This is not real." },
        new DialogueData.Line { speaker = "Bartender", text = "That depends. Is guilt real?" },
        new DialogueData.Line { speaker = "You",       text = "Who are you?" },
        new DialogueData.Line { speaker = "Bartender", text = "The part of you that warns you before you do it again." },
        new DialogueData.Line { speaker = "You",       text = "Before I do what?" },
        new DialogueData.Line { speaker = "Bartender", text = "Go outside. Ask the man near the trash cans. If he is still there." },
    };

    // ─── Inspector fields ─────────────────────────────────────────────────────

    [Header("Dialogue")]
    [Tooltip("Unused — all dialogue is inlined. Kept for backward compatibility with serialized scene data.")]
    [SerializeField] private DialogueData dialogueData;

    [Header("Bottle Spawn (Level 2 only)")]
    [Tooltip("Enable bottle spawning after dialogue ends. Keep false in Level 1.")]
    [SerializeField] private bool spawnBottleAfterDialogue = false;

    [Tooltip("The bottle prefab to instantiate after dialogue.")]
    [SerializeField] private GameObject bottlePrefab;

    [Tooltip("The Transform where the bottle will be placed (e.g. on the bar counter).")]
    [SerializeField] private Transform bottleSpawnPoint;

    [Tooltip("Scale applied to the spawned bottle.")]
    [SerializeField] private Vector3 bottleScale = new Vector3(0.2f, 0.2f, 0.2f);

    [Header("State")]
    [Tooltip("If true, the bartender can only be talked to once per cycle.")]
    [SerializeField] private bool oneTimeOnly = true;

    // ─── Private state ────────────────────────────────────────────────────────

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
            _cachedDialogueUI.OnDialogueEnded -= HandleDialogueEnded;
    }

    public string GetActionName() => "Talk";
    public InteractionKey GetPreferredKey() => InteractionKey.F;

    /// <summary>Bartender is always the first story beat — no prior beat required.</summary>
    public StorylineManager.Beat RequiredBeat => StorylineManager.Beat.Start;

    public bool CanInteract()
    {
        if (_isInDialogue) return false;
        if (oneTimeOnly && _hasInteracted) return false;
        return true;
    }

    /// <summary>Starts the dialogue sequence with the bartender.</summary>
    public void OnInteract()
    {
        if (!CanInteract()) return;

        _isInDialogue = true;

        if (_playerMovement != null)
            _playerMovement.ForceThirdPerson();

        if (_cachedDialogueUI == null)
            _cachedDialogueUI = DialogueUI.Instance;

        if (_cachedDialogueUI == null)
        {
            Debug.LogError("[BartenderInteraction] DialogueUI instance not found.");
            _isInDialogue = false;
            return;
        }

        // Level 2 uses the inlined Level2Lines. Level 1 uses the inlined Level1Lines.
        DialogueData data;
        if (spawnBottleAfterDialogue)
        {
            data = ScriptableObject.CreateInstance<DialogueData>();
            data.lines = Level2Lines;
        }
        else
        {
            data = ScriptableObject.CreateInstance<DialogueData>();
            data.lines = Level1Lines;
        }

        if (TryGetComponent<DialogueEventBridge>(out var bridge))
            bridge.RegisterOnce();

        _cachedDialogueUI.OnDialogueEnded += HandleDialogueEnded;
        _cachedDialogueUI.StartDialogue(data);
    }

    /// <summary>Called when dialogue finishes. Advances storyline (Level 1) or notifies sequence controller (Level 2).</summary>
    private void HandleDialogueEnded()
    {
        _cachedDialogueUI.OnDialogueEnded -= HandleDialogueEnded;

        _isInDialogue = false;
        _hasInteracted = true;

        // Level 1: advance the storyline beat so the little girl becomes available.
        StorylineManager.Instance?.Advance(StorylineManager.Beat.BartenderDone);

        // Level 2: spawn bottle and notify sequence controller (no-ops in Level 1 when disabled).
        if (spawnBottleAfterDialogue && bottlePrefab != null)
            SpawnBottle();

        Level2SequenceController.Instance?.OnBartenderTalked();

        Debug.Log("[BartenderInteraction] Dialogue complete.");
    }

    /// <summary>Instantiates the bottle at the spawn point.</summary>
    private void SpawnBottle()
    {
        Vector3 spawnPos = bottleSpawnPoint != null
            ? bottleSpawnPoint.position
            : transform.position + transform.forward * 1.5f + Vector3.up * 1.0f;

        Quaternion spawnRot = bottleSpawnPoint != null
            ? bottleSpawnPoint.rotation
            : Quaternion.identity;

        GameObject bottle = Instantiate(bottlePrefab, spawnPos, spawnRot);
        bottle.transform.localScale = bottleScale;
        bottle.SetActive(true);

        Debug.Log($"[BartenderInteraction] Bottle spawned at {spawnPos}.");
    }

    /// <summary>Resets the interaction so the bartender can be talked to again.</summary>
    public void ResetInteraction()
    {
        _hasInteracted = false;
    }
}
