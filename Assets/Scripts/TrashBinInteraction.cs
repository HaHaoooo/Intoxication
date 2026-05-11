using UnityEngine;

/// <summary>
/// Makes the trash bin interactable via the F key.
/// On interact: plays the trash bin sound, animates the door falling to the ground,
/// and optionally reveals a hidden homeless man NPC nearby.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class TrashBinInteraction : MonoBehaviour, IInteractable, ILockable
{
    [Header("References")]
    [Tooltip("The Door_2 Transform that will fall to the ground on interaction.")]
    [SerializeField] private Transform door;

    [Tooltip("The homeless man GameObject to reveal after interaction (must be inactive in the scene).")]
    [SerializeField] private GameObject homelessMan;

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

    public string GetActionName() => "Interact";
    public InteractionKey GetPreferredKey() => InteractionKey.F;

    /// <summary>Requires the axe to have been picked up first.</summary>
    public StorylineManager.Beat RequiredBeat => StorylineManager.Beat.AxeDone;

    /// <summary>Whether the trash bin can still be interacted with.</summary>
public bool CanInteract() => !_hasInteracted
        && (StorylineManager.Instance == null || StorylineManager.Instance.IsUnlocked(RequiredBeat));

    /// <summary>Plays the sound, starts the door falling animation, and reveals the homeless man.</summary>
    public void OnInteract()
    {
        if (!CanInteract()) return;

        _hasInteracted = true;

        if (_audioSource.clip != null)
            _audioSource.Play();

        _isFalling = true;

        if (homelessMan != null)
            homelessMan.SetActive(true);

        StorylineManager.Instance?.Advance(StorylineManager.Beat.TrashBinDone);
        Debug.Log("[TrashBinInteraction] Interaction triggered — door falling, homeless man revealed.");
    }
}
