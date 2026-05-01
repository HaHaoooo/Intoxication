using UnityEngine;

/// <summary>
/// Plays a music clip on the attached AudioSource when the scene starts.
/// Fades the volume in smoothly to avoid an abrupt start.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class MenuMusicPlayer : MonoBehaviour
{
    [Tooltip("The music clip to play. If null, the AudioSource's existing clip is used.")]
    [SerializeField] private AudioClip musicClip;

    [Tooltip("Target playback volume.")]
    [Range(0f, 1f)]
    [SerializeField] private float volume = 0.5f;

    [Tooltip("Duration of the fade-in when music starts (seconds).")]
    [SerializeField] private float fadeInDuration = 1.5f;

    private AudioSource _audioSource;
    private float _fadeTimer;
    private bool _isFadingIn;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
    }

    private void Start()
    {
        if (musicClip != null)
            _audioSource.clip = musicClip;

        if (_audioSource.clip == null)
        {
            Debug.LogWarning("[MenuMusicPlayer] No music clip assigned.", this);
            return;
        }

        _audioSource.loop = true;
        _audioSource.volume = 0f;
        _audioSource.Play();

        _fadeTimer = 0f;
        _isFadingIn = true;
    }

    private void Update()
    {
        if (!_isFadingIn) return;

        _fadeTimer += Time.unscaledDeltaTime;
        float t = fadeInDuration > 0f ? Mathf.Clamp01(_fadeTimer / fadeInDuration) : 1f;
        _audioSource.volume = Mathf.Lerp(0f, volume, t);

        if (t >= 1f)
            _isFadingIn = false;
    }

    /// <summary>Stops the music with an optional fade-out.</summary>
    public void StopMusic()
    {
        _isFadingIn = false;
        _audioSource.Stop();
    }
}
