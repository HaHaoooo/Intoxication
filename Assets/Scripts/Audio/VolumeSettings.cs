using UnityEngine;

/// <summary>
/// Static utility for managing game volume via AudioListener.volume and PlayerPrefs.
/// Provides a single point of control for volume across all scenes.
/// </summary>
public static class VolumeSettings
{
    private const string VolumeKey = "MasterVolume";
    private const float DefaultVolume = 0.8f;

    /// <summary>
    /// Gets or sets the master volume (0.0-1.0).
    /// Setting this updates AudioListener.volume and persists the value.
    /// </summary>
    public static float Volume
    {
        get => AudioListener.volume;
        set
        {
            float clamped = Mathf.Clamp01(value);
            AudioListener.volume = clamped;
            PlayerPrefs.SetFloat(VolumeKey, clamped);
            PlayerPrefs.Save();
        }
    }

    /// <summary>Loads the saved volume from PlayerPrefs and applies it to AudioListener.</summary>
    public static void Load()
    {
        float saved = PlayerPrefs.GetFloat(VolumeKey, DefaultVolume);
        AudioListener.volume = Mathf.Clamp01(saved);
    }

    /// <summary>Returns the saved volume value (0.0–1.0) without applying it.</summary>
    public static float GetSavedVolume()
    {
        return PlayerPrefs.GetFloat(VolumeKey, DefaultVolume);
    }
}
