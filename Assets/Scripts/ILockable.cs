/// <summary>
/// Implement on any interactable that participates in the Level 1 storyline gate system.
/// PlayerInteractionController uses this to show a locked hint when CanInteract() is false
/// due to storyline progression rather than the object being fully unavailable.
/// </summary>
public interface ILockable
{
    /// <summary>
    /// The minimum story beat required for this interactable to be active.
    /// </summary>
    StorylineManager.Beat RequiredBeat { get; }
}
