using UnityEngine;

/// <summary>
/// Marks a light as flickerable. All per-frame updates are driven by LightFlickerManager
/// to avoid 1000+ individual Update() calls.
/// </summary>
public class LightFlicker : MonoBehaviour
{
    public Light targetLight;
    public float minIntensity = 0f;
    public float maxIntensity = 10f;

    [Tooltip("How fast the intensity lerps toward the random target (units/sec). Higher = snappier flicker.")]
    public float lerpSpeed = 15f;

    [Tooltip("Average number of random intensity changes per second. Frame-rate independent.")]
    public float changesPerSecond = 10f;

    [Tooltip("Duration of a triggered flicker event in seconds.")]
    public float flickerDuration = 1.5f;

    /// <summary>True while this light is in a triggered flicker event.</summary>
    public bool IsFlickering { get; private set; }

    private float _baseIntensity;
    private float _flickerTimer;
    private float _flickerTarget;

    private void Start()
    {
        if (targetLight == null)
            targetLight = GetComponent<Light>();

        if (targetLight != null)
            _baseIntensity = targetLight.intensity;
    }

    /// <summary>Starts a timed flicker event. Called by LightFlickerManager.</summary>
    public void TriggerFlicker()
    {
        if (IsFlickering || targetLight == null) return;
        IsFlickering = true;
        _flickerTimer = flickerDuration;
        _flickerTarget = Random.Range(minIntensity, maxIntensity);
    }

    /// <summary>Ticks the flicker state. Called by LightFlickerManager each frame for active lights only.</summary>
    public void TickFlicker(float deltaTime)
    {
        if (!IsFlickering || targetLight == null) return;

        _flickerTimer -= deltaTime;

        if (_flickerTimer <= 0f)
        {
            // Restore to base intensity and stop
            targetLight.intensity = _baseIntensity;
            IsFlickering = false;
            return;
        }

        // Frame-rate independent probability: convert changesPerSecond to a per-frame probability.
        // P(change this frame) = 1 - (1 - 1)^dt  approximated as changesPerSecond * dt for small dt.
        if (Random.value < changesPerSecond * deltaTime)
            _flickerTarget = Random.Range(minIntensity, maxIntensity);

        // Frame-rate independent lerp using exponential decay.
        float t = 1f - Mathf.Exp(-lerpSpeed * deltaTime);
        targetLight.intensity = Mathf.Lerp(targetLight.intensity, _flickerTarget, t);
    }
}
