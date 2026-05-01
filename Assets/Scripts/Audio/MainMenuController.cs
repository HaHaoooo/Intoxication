using UnityEngine;

/// <summary>
/// Audio controller for the main menu scene. Manages the menu music player
/// reference and plays UI click sounds. Attach to the same GameObject as the
/// Menu UI controller.
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Tooltip("Reference to the MenuMusicPlayer component on the Music GameObject.")]
    [SerializeField] private MenuMusicPlayer menuMusicPlayer;

    [Tooltip("Sound effect played when a menu button is clicked.")]
    [SerializeField] private AudioClip clickSound;

    [Tooltip("Volume for UI click sounds.")]
    [Range(0f, 1f)]
    [SerializeField] private float clickVolume = 0.5f;

    private AudioSource _audioSource;

    /// <summary>Singleton-like accessor so Menu.cs can play click sounds without tight coupling.</summary>
    public static MainMenuController Instance { get; private set; }

    private void Awake()
    {
        Instance = this;

        // Use or create an AudioSource for click sounds (separate from music).
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null)
            _audioSource = gameObject.AddComponent<AudioSource>();

        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 0f;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Plays the configured UI click sound effect.</summary>
    public void PlayClickSound()
    {
        if (clickSound != null && _audioSource != null)
            _audioSource.PlayOneShot(clickSound, clickVolume);
    }
}
