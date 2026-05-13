using UnityEngine;

/// <summary>
/// Placed on or near the final bottle in Level 2.
/// When the player enters the trigger volume, plays the bottle-approach dialogue
/// via Level2SequenceController and blocks the bottle's IInteractable until the
/// dialogue finishes.
///
/// Can be reset (via ResetTrigger) so it fires again on the second visit after
/// the Scene 4 teleport.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BottleApproachDialogueTrigger : MonoBehaviour
{
    [Tooltip("The bottle GameObject that implements IBottleInteractionLock. " +
             "Its interaction is locked until the approach dialogue ends.")]
    [SerializeField] private GameObject bottleObject;

    [Tooltip("Tag used to identify the player.")]
    [SerializeField] private string playerTag = "Player";

    private bool _hasTriggered;
    private IBottleInteractionLock _bottleLock;

    private void Awake()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;

        if (bottleObject != null)
            _bottleLock = bottleObject.GetComponent<IBottleInteractionLock>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_hasTriggered) return;
        if (!other.CompareTag(playerTag)) return;

        _hasTriggered = true;

        // Lock the bottle during dialogue.
        _bottleLock?.SetLocked(true);

        Level2SequenceController seq = Level2SequenceController.Instance;
        if (seq != null)
        {
            seq.TriggerBottleApproach(OnDialogueComplete);
        }
        else
        {
            OnDialogueComplete();
        }
    }

    private void OnDialogueComplete()
    {
        _bottleLock?.SetLocked(false);
    }

    /// <summary>
    /// Called by Level2SequenceController after the Scene 4 teleport so the
    /// trigger fires again on the second approach. The bottle is re-locked
    /// until the dialogue finishes a second time.
    /// </summary>
    public void ResetTrigger()
    {
        _hasTriggered = false;
    }
}

/// <summary>Interface implemented by the bottle to allow external locking of interaction.</summary>
public interface IBottleInteractionLock
{
    /// <summary>When locked, the bottle's CanInteract returns false.</summary>
    void SetLocked(bool locked);
}
