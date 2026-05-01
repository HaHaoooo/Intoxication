using UnityEngine;

/// <summary>
/// Makes the trash bin interactable via the F key.
/// On interact: plays the trash bin sound and animates the door
/// falling straight down to ground level.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class TrashBinInteraction : MonoBehaviour, IInteractable
{
    [Header("References")]
    [Tooltip("The Door_2 Transform that will fall to the ground on interaction.")]
    [SerializeField] private Transform door;

    [Header("Settings")]
    [Tooltip("Interaction prompt shown to the player.")]
    [SerializeField] private string interactionPrompt = "[ F ] Interact";

    [Tooltip("Target local Y position the door falls to (ground level relative to its parent).")]
    [SerializeField] private float doorGroundLocalY = -0.57f;

    [Tooltip("Speed at which the door falls (units per second).")]
    [SerializeField] private float fallSpeed = 3f;

    private AudioSource _audioSource;
    private bool _hasInteracted;
    private bool _isFalling;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    private void Update()
    {
        if (!_isFalling || door == null) return;

        Vector3 localPos = door.localPosition;
        if (localPos.y > doorGroundLocalY)
        {
            localPos.y = Mathf.MoveTowards(localPos.y, doorGroundLocalY, fallSpeed * Time.deltaTime);
            door.localPosition = localPos;
        }
        else
        {
            localPos.y = doorGroundLocalY;
            door.localPosition = localPos;
            _isFalling = false;
        }
    }

    /// <summary>Returns the prompt text shown when the player looks at the trash bin.</summary>
    public string GetInteractionPrompt() => interactionPrompt;

    /// <summary>Whether the trash bin can still be interacted with.</summary>
    public bool CanInteract() => !_hasInteracted;

    /// <summary>Plays the sound and starts the door falling animation.</summary>
    public void OnInteract()
    {
        if (!CanInteract()) return;

        _hasInteracted = true;

        if (_audioSource.clip != null)
            _audioSource.Play();

        _isFalling = true;

        Debug.Log("[TrashBinInteraction] Interaction triggered — door falling.");
    }
}
