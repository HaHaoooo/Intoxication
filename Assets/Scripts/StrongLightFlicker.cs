using UnityEngine;

public class StrongLightFlicker : MonoBehaviour
{
    private Light _light;
    private float _baseIntensity;
    
    [SerializeField] private float minIntensityMult = 0.1f;
    [SerializeField] private float maxIntensityMult = 1.5f;
    [SerializeField] private float flickerSpeed = 0.05f;
    [SerializeField] private float heavyFlickerChance = 0.05f;

    private void Awake()
    {
        _light = GetComponent<Light>();
        if (_light != null) _baseIntensity = _light.intensity;
    }

    private void Update()
    {
        if (_light == null) return;

        if (Random.value < heavyFlickerChance)
        {
            // Occasional heavy dip or spike
            _light.intensity = _baseIntensity * Random.Range(0f, 0.2f);
        }
        else
        {
            float noise = Mathf.PerlinNoise(Time.time * (1f / flickerSpeed), 0);
            _light.intensity = _baseIntensity * Mathf.Lerp(minIntensityMult, maxIntensityMult, noise);
        }
    }
}
