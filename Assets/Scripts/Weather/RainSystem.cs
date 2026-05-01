using UnityEngine;
using UnityEngine.Rendering;

namespace Weather
{
    /// <summary>
    /// Self-configuring rain particle system that follows the player.
    /// All particle modules are configured in code so the scene only needs this component.
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    [RequireComponent(typeof(ParticleSystemRenderer))]
    public class RainSystem : MonoBehaviour
    {
        [Header("Rain Settings")]
        [Range(0f, 1f)] public float intensity = 0.8f;
        public float maxEmissionRate = 1500f;
        public float transitionSpeed = 2f;

        [Header("Spawn Area")]
        public float spawnWidth = 150f;
        public float spawnLength = 150f;
        public float spawnHeight = 25f;

        [Header("Follow Target")]
        public Transform followTarget;

        [Header("Rendering")]
        public Material rainMaterial;

        [Header("Audio")]
        [Tooltip("Audio clip for rain ambience. Played on the attached AudioSource.")]
        public AudioClip rainAudioClip;

        [Tooltip("Volume of the rain audio.")]
        [Range(0f, 1f)]
        public float rainAudioVolume = 1f;

        private ParticleSystem rainParticles;
        private ParticleSystem.EmissionModule emission;
        private ParticleSystem.ShapeModule shape;
        private Material runtimeMaterial;
        private AudioSource rainAudioSource;
        private float currentRate;
        private float occlusionCheckTimer;
        private ParticleSystem.Particle[] particleBuffer;
        private Collider[] occlusionVolumes;
        private Vector3 _lastShapeScale;

        private const float OCCLUSION_CHECK_INTERVAL = 0.1f;

        private static readonly int BaseColorProp = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorProp = Shader.PropertyToID("_Color");
        private static readonly int SurfaceProp = Shader.PropertyToID("_Surface");
        private static readonly int BlendProp = Shader.PropertyToID("_Blend");
        private static readonly int CullProp = Shader.PropertyToID("_Cull");
        private static readonly int SrcBlendProp = Shader.PropertyToID("_SrcBlend");
        private static readonly int DstBlendProp = Shader.PropertyToID("_DstBlend");
        private static readonly int ZWriteProp = Shader.PropertyToID("_ZWrite");

        private void Awake()
        {
            rainParticles = GetComponent<ParticleSystem>();
            emission = rainParticles.emission;
            shape = rainParticles.shape;

            ConfigureParticleSystem();
            ConfigureRenderer();
            ConfigureAudio();

            // Pre-allocate particle buffer based on max particles
            particleBuffer = new ParticleSystem.Particle[rainParticles.main.maxParticles];
        }

        private void Start()
        {
            if (followTarget == null)
            {
                GameObject player = GameObject.FindWithTag("Player");
                if (player != null)
                {
                    followTarget = player.transform;
                }
            }

            currentRate = intensity * maxEmissionRate;
            emission.rateOverTime = currentRate;
            UpdateShapeSize();

            if (!rainParticles.isPlaying)
            {
                rainParticles.Play();
            }
        }

        private void Update()
        {
            if (followTarget != null)
            {
                Vector3 position = followTarget.position;
                if (float.IsFinite(position.x) && float.IsFinite(position.y) && float.IsFinite(position.z))
                {
                    position.y += spawnHeight;
                    transform.position = position;
                }
            }

            // Only update shape when dimensions actually change
            Vector3 newScale = new Vector3(spawnWidth, spawnLength, 0.1f);
            if (newScale != _lastShapeScale)
            {
                shape.scale = newScale;
                _lastShapeScale = newScale;
            }

            float targetRate = intensity * maxEmissionRate;
            currentRate = Mathf.Lerp(currentRate, targetRate, Time.deltaTime * transitionSpeed);
            emission.rateOverTime = currentRate;

            // Keep audio volume independent of particle intensity.
            if (rainAudioSource != null)
                rainAudioSource.volume = rainAudioVolume;
        }

        private void OnDestroy()
        {
            if (runtimeMaterial == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(runtimeMaterial);
            }
            else
            {
                DestroyImmediate(runtimeMaterial);
            }
        }

        /// <summary>Sets rain intensity between 0 (off) and 1 (heavy rain).</summary>
        public void SetIntensity(float value)
        {
            intensity = Mathf.Clamp01(value);
        }

        private void ConfigureParticleSystem()
        {
            transform.rotation = Quaternion.Euler(90f, 0f, 0f);

            var main = rainParticles.main;
            main.loop = true;
            main.playOnAwake = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 3.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(10f, 14f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.05f);
            main.startColor = new ParticleSystem.MinMaxGradient(
                new Color(0.75f, 0.88f, 1f, 0.55f),
                new Color(0.85f, 0.93f, 1f, 0.3f));
            main.gravityModifier = 0.4f;
            main.maxParticles = 15000;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            emission.enabled = true;
            emission.rateOverTime = maxEmissionRate * intensity;

            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.rotation = Vector3.zero;
            UpdateShapeSize();

            var velocityOverLifetime = rainParticles.velocityOverLifetime;
            velocityOverLifetime.enabled = false;

            var collision = rainParticles.collision;
            collision.enabled = false;
        }

        private void ConfigureRenderer()
        {
            ParticleSystemRenderer renderer = GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 0.08f;
            renderer.velocityScale = 0.14f;
            renderer.cameraVelocityScale = 0f;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            Material materialToUse = rainMaterial != null ? rainMaterial : CreateRuntimeMaterial();
            if (materialToUse != null)
            {
                renderer.sharedMaterial = materialToUse;
            }
        }

        private void UpdateShapeSize()
        {
            shape.scale = new Vector3(spawnWidth, spawnLength, 0.1f);
        }

        /// <summary>Configures the AudioSource for rain ambience if a clip is assigned.</summary>
        private void ConfigureAudio()
        {
            if (rainAudioClip == null) return;

            rainAudioSource = GetComponent<AudioSource>();
            if (rainAudioSource == null)
                rainAudioSource = gameObject.AddComponent<AudioSource>();

            rainAudioSource.clip = rainAudioClip;
            rainAudioSource.loop = true;
            rainAudioSource.spatialBlend = 0f;
            rainAudioSource.pitch = 1f;
            rainAudioSource.volume = rainAudioVolume;
            rainAudioSource.playOnAwake = false;
            rainAudioSource.Play();
        }

        private Material CreateRuntimeMaterial()
        {
            if (runtimeMaterial != null)
            {
                return runtimeMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
            {
                shader = Shader.Find("Particles/Standard Unlit");
            }

            if (shader == null)
            {
                return null;
            }

            runtimeMaterial = new Material(shader)
            {
                name = "Runtime Rain Material",
                renderQueue = (int)RenderQueue.Transparent
            };

            Color rainColor = new Color(0.7f, 0.88f, 1f, 0.35f);
            SetColorIfExists(runtimeMaterial, BaseColorProp, rainColor);
            SetColorIfExists(runtimeMaterial, ColorProp, rainColor);
            SetFloatIfExists(runtimeMaterial, SurfaceProp, 1f);
            SetFloatIfExists(runtimeMaterial, BlendProp, 0f);
            SetFloatIfExists(runtimeMaterial, CullProp, (float)CullMode.Off);
            SetFloatIfExists(runtimeMaterial, SrcBlendProp, (float)BlendMode.SrcAlpha);
            SetFloatIfExists(runtimeMaterial, DstBlendProp, (float)BlendMode.OneMinusSrcAlpha);
            SetFloatIfExists(runtimeMaterial, ZWriteProp, 0f);
            runtimeMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

            return runtimeMaterial;
        }

        private static void SetFloatIfExists(Material material, int propertyId, float value)
        {
            if (material.HasProperty(propertyId))
            {
                material.SetFloat(propertyId, value);
            }
        }

        private static void SetColorIfExists(Material material, int propertyId, Color value)
        {
            if (material.HasProperty(propertyId))
            {
                material.SetColor(propertyId, value);
            }
        }
    }
}
