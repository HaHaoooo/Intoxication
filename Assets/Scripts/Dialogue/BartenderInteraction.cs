using Player;
using UnityEngine;

/// <summary>
/// Handles bartender NPC interaction: starts a dialogue sequence,
/// switches to third-person camera, and spawns a bottle on the bar
/// counter when the dialogue ends.
/// Attach to the bartender GameObject (requires a Collider for raycast detection).
/// </summary>
public class BartenderInteraction : MonoBehaviour, IInteractable
{
    [Header("Dialogue")]
    [Tooltip("The dialogue data asset containing lines for this conversation.")]
    [SerializeField] private DialogueData dialogueData;

    [Header("Bottle Spawn")]
    [Tooltip("The bottle prefab to instantiate after dialogue.")]
    [SerializeField] private GameObject bottlePrefab;

    [Tooltip("The Transform where the bottle will be placed (e.g. on the bar counter).")]
    [SerializeField] private Transform bottleSpawnPoint;

    [Tooltip("Scale applied to the spawned bottle.")]
    [SerializeField] private Vector3 bottleScale = new Vector3(0.2f, 0.2f, 0.2f);

    [Header("State")]
    [Tooltip("If true, the bartender can only give one bottle per interaction cycle.")]
    [SerializeField] private bool oneTimeOnly = true;

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
        // Unsubscribe to prevent dangling delegate if destroyed mid-dialogue.
        if (_cachedDialogueUI != null)
            _cachedDialogueUI.OnDialogueEnded -= HandleDialogueEnded;
    }

    public string GetActionName() => "Talk";
    public InteractionKey GetPreferredKey() => InteractionKey.F;

    /// <summary>Whether the bartender is available for interaction.</summary>
public bool CanInteract()
    {
        if (_isInDialogue) return false;
        if (oneTimeOnly && _hasInteracted) return false;
        return dialogueData != null;
    }

    /// <summary>Starts the dialogue sequence with the bartender.</summary>
    public void OnInteract()
    {
        if (!CanInteract()) return;

        _isInDialogue = true;

        if (_playerMovement != null)
        {
            _playerMovement.ForceThirdPerson();
        }

        if (_cachedDialogueUI == null)
            _cachedDialogueUI = DialogueUI.Instance;

        DialogueUI ui = _cachedDialogueUI;
        if (ui == null)
        {
            Debug.LogError("[BartenderInteraction] DialogueUI instance not found.");
            _isInDialogue = false;
            return;
        }

        ui.OnDialogueEnded += HandleDialogueEnded;
        
        if (TryGetComponent<DialogueEventBridge>(out var bridge))
        {
            bridge.RegisterOnce();
        }

        ui.StartDialogue(dialogueData);

        Debug.Log("[BartenderInteraction] Started dialogue with bartender.");
    }

    /// <summary>Called when the dialogue sequence finishes; spawns the bottle and advances objective.</summary>
    private void HandleDialogueEnded()
    {
        if (_cachedDialogueUI != null)
        {
            _cachedDialogueUI.OnDialogueEnded -= HandleDialogueEnded;
        }

        _isInDialogue = false;
        _hasInteracted = true;

        SpawnBottle();
        ObjectiveUI.Instance?.Set(ObjectiveState.CheckSurroundings);
        Debug.Log("[BartenderInteraction] Dialogue complete. Bottle spawned.");
    }

    /// <summary>Instantiates the bottle at the spawn point.</summary>
    private void SpawnBottle()
    {
        if (bottlePrefab == null)
        {
            Debug.LogWarning("[BartenderInteraction] No bottle prefab assigned.");
            return;
        }

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
