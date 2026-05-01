using UnityEngine;

/// <summary>
/// Plays looping footstep audio that switches between street and indoor clips
/// based on which FootstepZone the player occupies. Fades volume smoothly to
/// avoid audio pops when starting/stopping or transitioning between surfaces.
/// Adds slight pitch variation for a more natural feel.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerFootstepController : MonoBehaviour
{
    [Header("Clips")]
    [Tooltip("Footstep sound used outdoors on the street.")]
    [SerializeField] private AudioClip streetClip;

    [Tooltip("Footstep sound used indoors (bar, etc.).")]
    [SerializeField] private AudioClip indoorClip;

    [Header("Volume")]
    [Tooltip("Volume for outdoor street footsteps.")]
    [Range(0f, 1f)]
    [SerializeField] private float streetVolume = 0.35f;

    [Tooltip("Volume for indoor footsteps — should be more prominent than street.")]
    [Range(0f, 1f)]
    [SerializeField] private float indoorVolume = 0.75f;

    [Header("Fade & Pitch")]
    [Tooltip("Seconds for volume to fade in/out when starting or stopping. Prevents audio pops.")]
    [SerializeField] private float fadeDuration = 0.15f;

    [Tooltip("Random pitch variation range applied each time footsteps start playing.")]
    [SerializeField] private float minPitch = 0.92f;

    [Tooltip("Random pitch variation range applied each time footsteps start playing.")]
    [SerializeField] private float maxPitch = 1.08f;

    [Header("Detection")]
    [Tooltip("Minimum horizontal speed (m/s) required to play footstep audio.")]
    [SerializeField] private float minMoveSpeed = 0.3f;

    private AudioSource audioSource;
    private CharacterController characterController;
    private Player.PlayerMovement playerMovement;
    private FootstepSurface currentSurface = FootstepSurface.Street;
    private int indoorZoneCount;
    private float targetVolume;
    private float currentFadeVelocity;
    private bool shouldPlay;
    private float minMoveSpeedSqr;

    private void Awake()
    {
        minMoveSpeedSqr = minMoveSpeed * minMoveSpeed;
        characterController = GetComponent<CharacterController>();
        playerMovement = GetComponent<Player.PlayerMovement>();

        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.loop = true;
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0f;
        audioSource.priority = 128;
    }

    private void Start()
    {
        SetClipForSurface(currentSurface);
    }

    private void Update()
    {
        // Freeze all audio logic if player movement is disabled (e.g. during opening animation)
        if (playerMovement != null && !playerMovement.canRotateCamera)
        {
            targetVolume = 0f;
            audioSource.volume = Mathf.SmoothDamp(audioSource.volume, targetVolume,
                ref currentFadeVelocity, fadeDuration);
            if (audioSource.isPlaying && audioSource.volume < 0.005f)
            {
                audioSource.Stop();
                audioSource.volume = 0f;
            }
            return;
        }

        shouldPlay = IsMovingOnGround();

        if (shouldPlay)
        {
            AudioClip desired = ClipForSurface(currentSurface);

            if (audioSource.clip != desired)
            {
                audioSource.clip = desired;
                audioSource.pitch = Random.Range(minPitch, maxPitch);
                audioSource.Play();
            }
            else if (!audioSource.isPlaying)
            {
                audioSource.pitch = Random.Range(minPitch, maxPitch);
                audioSource.Play();
            }

            targetVolume = VolumeForSurface(currentSurface);
        }
        else
        {
            targetVolume = 0f;
        }

        // Smooth fade to avoid audio pops on start/stop/surface transitions.
        audioSource.volume = Mathf.SmoothDamp(audioSource.volume, targetVolume,
            ref currentFadeVelocity, fadeDuration);

        // Only stop playback once the volume has fully faded out.
        if (!shouldPlay && audioSource.isPlaying && audioSource.volume < 0.005f)
        {
            audioSource.Stop();
            audioSource.volume = 0f;
        }
    }

    /// <summary>Called by FootstepZone when the player enters an indoor trigger.</summary>
    public void EnterZone(FootstepSurface surface)
    {
        if (surface == FootstepSurface.Indoor)
            indoorZoneCount++;

        UpdateSurface();
    }

    /// <summary>Called by FootstepZone when the player exits an indoor trigger.</summary>
    public void ExitZone(FootstepSurface surface)
    {
        if (surface == FootstepSurface.Indoor)
            indoorZoneCount = Mathf.Max(0, indoorZoneCount - 1);

        UpdateSurface();
    }

    private void UpdateSurface()
    {
        FootstepSurface newSurface = indoorZoneCount > 0 ? FootstepSurface.Indoor : FootstepSurface.Street;
        if (newSurface == currentSurface) return;
        currentSurface = newSurface;
        SetClipForSurface(currentSurface);
    }

    private void SetClipForSurface(FootstepSurface surface)
    {
        audioSource.clip = ClipForSurface(surface);
    }

    private AudioClip ClipForSurface(FootstepSurface surface)
    {
        return surface == FootstepSurface.Indoor ? indoorClip : streetClip;
    }

    private float VolumeForSurface(FootstepSurface surface)
    {
        return surface == FootstepSurface.Indoor ? indoorVolume : streetVolume;
    }

    private bool IsMovingOnGround()
    {
        if (!characterController.isGrounded)
            return false;

        Vector3 horizontalVelocity = characterController.velocity;
        horizontalVelocity.y = 0f;
        return horizontalVelocity.sqrMagnitude >= minMoveSpeedSqr;
    }
}
