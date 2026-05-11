using UnityEngine;

/// <summary>
/// Tracks which Level 1 story beat has been completed.
/// All interactables check this before allowing interaction.
/// Order: Bartender → Little Girl → Axe → Trash Bin → Homeless Man
/// </summary>
public class StorylineManager : MonoBehaviour
{
    public static StorylineManager Instance { get; private set; }

    public enum Beat
    {
        Start = 0,          // Nothing done yet — only bartender is unlocked
        BartenderDone = 1,  // Little girl is now unlocked
        LittleGirlDone = 2, // Axe is now unlocked
        AxeDone = 3,        // Trash bin is now unlocked
        TrashBinDone = 4,   // Homeless man is now unlocked
        HomelessDone = 5    // Storyline complete
    }

    public Beat CurrentBeat { get; private set; } = Beat.Start;

    /// <summary>Human-readable hint shown when a locked interactable is approached.</summary>
    public static readonly string[] LockedHints =
    {
        "Talk to the bartender first.",
        "Follow the little girl first.",
        "Find something to protect yourself first.",
        "Check the trash cans first.",
        "Talk to the man by the trash cans first.",
    };

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Advances the storyline to the next beat. Call this from each interaction script after completion.</summary>
    public void Advance(Beat completed)
    {
        if ((int)completed > (int)CurrentBeat)
            CurrentBeat = completed;

        Debug.Log($"[StorylineManager] Beat advanced to: {CurrentBeat}");
    }

    /// <summary>Returns true if the given beat is currently unlocked (all prior beats done).</summary>
    public bool IsUnlocked(Beat required) => (int)CurrentBeat >= (int)required;

    /// <summary>
    /// Returns the locked hint to show when the player approaches a locked interactable.
    /// Returns null if no relevant hint exists.
    /// </summary>
    public string GetLockedHint()
    {
        int index = (int)CurrentBeat;
        if (index >= 0 && index < LockedHints.Length)
            return LockedHints[index];
        return null;
    }
}
