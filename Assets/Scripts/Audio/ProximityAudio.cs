using UnityEngine;

/// <summary>
/// Fades an AudioSource in/out based on distance to the AudioListener.
/// Uses spatialBlend = 0 (2D) so this script has full control over volume
/// without double-attenuation from Unity's 3D audio rolloff.
/// Uses sqrMagnitude to avoid per-frame sqrt, and caches the inverse
/// distance range so the hot path is a single multiply.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class ProximityAudio : MonoBehaviour
{
    [SerializeField] private float maxDistance = 5.0f;
    [SerializeField] private float minDistance = 1.0f;

    [Tooltip("Target volume when the listener is at or closer than minDistance.")]
    [Range(0f, 1f)]
    [SerializeField] private float maxVolume = 1.0f;

    [Tooltip("How fast volume changes are smoothed (seconds).")]
    [SerializeField] private float smoothTime = 0.1f;

    private AudioSource _audioSource;
    private Transform _listenerTransform;
    private float _sqrMaxDistance;
    private float _invDistanceRange;
    private float _volumeVelocity;

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        // Use 2D mode — we manually control volume based on distance.
        // spatialBlend = 1 would cause Unity to also apply its own distance rolloff,
        // resulting in double attenuation.
        _audioSource.spatialBlend = 0f;
        _audioSource.loop = true;
        _audioSource.playOnAwake = false;
        _audioSource.Stop();
        _audioSource.volume = 0f;

        _sqrMaxDistance = maxDistance * maxDistance;
        float range = maxDistance - minDistance;
        _invDistanceRange = range > 0.0001f ? 1f / range : 1f;
    }

    private void Start()
    {
        AudioListener listener = FindAnyObjectByType<AudioListener>();
        if (listener != null) _listenerTransform = listener.transform;
    }

    private void Update()
    {
        // Lazily re-locate the listener if it was null or has since been destroyed.
        if (_listenerTransform == null)
        {
            AudioListener listener = FindAnyObjectByType<AudioListener>();
            if (listener != null)
                _listenerTransform = listener.transform;
            else
                return;
        }

        float sqrDist = (transform.position - _listenerTransform.position).sqrMagnitude;

        if (sqrDist <= _sqrMaxDistance)
        {
            if (!_audioSource.isPlaying) _audioSource.Play();

            // Only sqrt when actually in range
            float distance = Mathf.Sqrt(sqrDist);
            float targetVol = Mathf.Clamp01((maxDistance - distance) * _invDistanceRange) * maxVolume;
            _audioSource.volume = Mathf.SmoothDamp(_audioSource.volume, targetVol,
                ref _volumeVelocity, smoothTime);
        }
        else
        {
            if (_audioSource.isPlaying)
            {
                // Fade out smoothly before stopping to prevent pops.
                _audioSource.volume = Mathf.SmoothDamp(_audioSource.volume, 0f,
                    ref _volumeVelocity, smoothTime);

                if (_audioSource.volume < 0.005f)
                {
                    _audioSource.Stop();
                    _audioSource.volume = 0f;
                }
            }
        }
    }
}
