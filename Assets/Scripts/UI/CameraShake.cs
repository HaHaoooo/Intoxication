using System.Collections;
using UnityEngine;

/// <summary>
/// Procedural camera shake using Perlin noise.
/// Attach to the player's Main Camera GameObject.
/// Call Shake() from any system to trigger a shake.
/// </summary>
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("Defaults")]
    [Tooltip("Default shake magnitude (world units).")]
    [SerializeField] private float defaultMagnitude = 0.15f;

    [Tooltip("Default shake duration in seconds.")]
    [SerializeField] private float defaultDuration = 0.6f;

    [Tooltip("How fast the noise seed moves along the Perlin axis.")]
    [SerializeField] private float noiseSpeed = 40f;

    private Vector3 _originalLocalPosition;
    private Coroutine _activeShake;

    private void Awake()
    {
        // Register on whatever camera this is attached to.
        if (Instance == null) Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>Triggers a camera shake with the default magnitude and duration.</summary>
    public void Shake() => Shake(defaultMagnitude, defaultDuration);

    /// <summary>Triggers a camera shake with custom parameters.</summary>
    public void Shake(float magnitude, float duration)
    {
        if (_activeShake != null)
            StopCoroutine(_activeShake);

        _activeShake = StartCoroutine(ShakeRoutine(magnitude, duration));
    }

    private IEnumerator ShakeRoutine(float magnitude, float duration)
    {
        _originalLocalPosition = transform.localPosition;
        float elapsed = 0f;
        float seed = Random.value * 100f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float progress = elapsed / duration;

            // Envelope: ramps up quickly, then eases out.
            float envelope = Mathf.Sin(progress * Mathf.PI);
            float currentMag = magnitude * envelope;

            float offsetX = (Mathf.PerlinNoise(seed, elapsed * noiseSpeed) - 0.5f) * 2f * currentMag;
            float offsetY = (Mathf.PerlinNoise(seed + 1f, elapsed * noiseSpeed) - 0.5f) * 2f * currentMag;

            transform.localPosition = _originalLocalPosition + new Vector3(offsetX, offsetY, 0f);
            yield return null;
        }

        transform.localPosition = _originalLocalPosition;
        _activeShake = null;
    }
}
