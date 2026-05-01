/// <summary>
/// Interface for objects the player can interact with via the F key.
/// Implement on any NPC or interactive object that responds to player proximity + input.
/// </summary>
public interface IInteractable
{
    /// <summary>Returns the prompt text shown to the player (e.g. "[ F ] Talk").</summary>
    string GetInteractionPrompt();

    /// <summary>Called when the player triggers the interaction.</summary>
    void OnInteract();

    /// <summary>Whether this interactable is currently available for interaction.</summary>
    bool CanInteract();
}
