public enum InteractionKey
{
    E,
    F
}

/// <summary>
/// Interface for objects the player can interact with.
/// </summary>
public interface IInteractable
{
    /// <summary>Returns the action name (e.g. "Talk", "Pick Up").</summary>
    string GetActionName();

    /// <summary>Returns the preferred key for this interaction.</summary>
    InteractionKey GetPreferredKey();

    /// <summary>Called when the player triggers the interaction.</summary>
    void OnInteract();

    /// <summary>Whether this interactable is currently available for interaction.</summary>
    bool CanInteract();
}
