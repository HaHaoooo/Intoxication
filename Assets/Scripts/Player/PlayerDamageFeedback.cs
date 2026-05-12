using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Cinemachine;
using System.Collections;

public class PlayerDamageFeedback : MonoBehaviour
{
    public static PlayerDamageFeedback Instance { get; private set; }

    [Header("Screen Shake")]
    [SerializeField] private float impulseForce = 0.5f;

    [Header("Damage Tint")]
    [SerializeField] private float flashDuration = 0.5f;
    [SerializeField] private Color tintColor = new Color(1f, 0f, 0f, 0.4f);

    private Volume _volume;
    private Vignette _vignette;
    private Coroutine _activeFlash;
    private CinemachineImpulseSource _impulseSource;

    private void Awake()
    {
        Instance = this;
        _impulseSource = GetComponent<CinemachineImpulseSource>();
        if (_impulseSource == null) _impulseSource = gameObject.AddComponent<CinemachineImpulseSource>();

        // Try to find a Volume with Vignette
        _volume = FindAnyObjectByType<Volume>();
        if (_volume != null && _volume.profile.TryGet<Vignette>(out var v))
        {
            _vignette = v;
        }
    }

    public void PlayFeedback()
    {
        if (_impulseSource != null)
            _impulseSource.GenerateImpulse(Vector3.up * impulseForce);

        if (_activeFlash != null) StopCoroutine(_activeFlash);
        _activeFlash = StartCoroutine(FlashTint());
    }

    private IEnumerator FlashTint()
    {
        if (_vignette == null) yield break;

        _vignette.active = true;
        _vignette.color.Override(tintColor);
        
        float elapsed = 0f;
        while (elapsed < flashDuration)
        {
            float t = elapsed / flashDuration;
            // Pulse then fade
            float intensity = Mathf.Sin(t * Mathf.PI) * 0.45f;
            _vignette.intensity.Override(intensity);
            
            elapsed += Time.deltaTime;
            yield return null;
        }

        _vignette.intensity.Override(0f);
        _vignette.active = false;
    }
}
