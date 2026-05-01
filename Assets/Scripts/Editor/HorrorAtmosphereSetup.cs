using UnityEngine;
using UnityEngine.Rendering;
using UnityEditor;

namespace Editor
{
    /// <summary>
    /// Sets up a dark horror atmosphere via lighting, fog, and ambient only.
    /// Does NOT touch post-processing volumes or profiles.
    /// </summary>
    public static class HorrorAtmosphereSetup
    {
        private const string MENU_PATH = "Tools/Scene/Setup Horror Atmosphere";

        // Moonlight - cold blue-white, very low intensity for oppressive night
        private static readonly Color MOONLIGHT_COLOR = new Color(0.6f, 0.65f, 0.75f, 1f);
        private const float MOONLIGHT_INTENSITY = 0.3f;
        private const float MOONLIGHT_SHADOW_STRENGTH = 0.75f;

        // Street lamps - dim sickly amber, tight range for claustrophobic light pools
        private static readonly Color STREET_LAMP_COLOR = new Color(0.85f, 0.55f, 0.2f, 1f);
        private const float STREET_LAMP_INTENSITY = 3.5f;
        private const float STREET_LAMP_RANGE = 11f;

        // Fog - deep blue-black, denser for obscured vision
        private static readonly Color FOG_COLOR = new Color(0.01f, 0.01f, 0.025f, 1f);
        private const float FOG_DENSITY = 0.032f;

        // Ambient - near-black with cold blue shift
        private static readonly Color AMBIENT_COLOR = new Color(0.03f, 0.03f, 0.045f, 1f);

        // Reflection - heavily suppressed
        private const float REFLECTION_INTENSITY = 0.08f;

        [MenuItem(MENU_PATH)]
        public static void SetupAtmosphere()
        {
            Undo.SetCurrentGroupName("Setup Horror Atmosphere");
            int undoGroup = Undo.GetCurrentGroup();

            SetupMoonlight();
            SetupStreetLamps();
            SetupFogAndAmbient();

            Undo.CollapseUndoOperations(undoGroup);
            Debug.Log("[HorrorAtmosphereSetup] Dark horror atmosphere applied (no post-processing changed).");
        }

        private static void SetupMoonlight()
        {
            GameObject sceneRoot = GameObject.Find("Scene");
            if (sceneRoot == null) return;

            Transform lightsOnly = FindChildRecursive(sceneRoot.transform, "街道_LightsOnly");
            if (lightsOnly == null) return;

            // Try both original and renamed names
            Transform sunTransform = lightsOnly.Find("Moonlight");
            if (sunTransform == null)
                sunTransform = lightsOnly.Find("日光");
            if (sunTransform == null) return;

            Light moonLight = sunTransform.GetComponent<Light>();
            if (moonLight == null) return;

            Undo.RecordObject(moonLight, "Setup Moonlight");
            moonLight.color = MOONLIGHT_COLOR;
            moonLight.intensity = MOONLIGHT_INTENSITY;
            moonLight.shadows = LightShadows.Soft;
            moonLight.shadowStrength = MOONLIGHT_SHADOW_STRENGTH;
            moonLight.enabled = true;

            Undo.RecordObject(sunTransform, "Rotate Moonlight");
            // Low angle creates long, creeping shadows
            sunTransform.localRotation = Quaternion.Euler(25f, -155f, 0f);

            sunTransform.gameObject.name = "Moonlight";
            Debug.Log("[HorrorAtmosphereSetup] Moonlight: cold blue-white, intensity 0.3, strong shadows.");
        }

        private static void SetupStreetLamps()
        {
            GameObject sceneRoot = GameObject.Find("Scene");
            if (sceneRoot == null) return;

            Light[] allLights = sceneRoot.GetComponentsInChildren<Light>(true);
            int adjustedCount = 0;

            foreach (Light light in allLights)
            {
                if (light.type != LightType.Point)
                    continue;

                Undo.RecordObject(light, "Adjust Street Lamp");
                light.color = STREET_LAMP_COLOR;
                light.intensity = STREET_LAMP_INTENSITY;
                light.range = STREET_LAMP_RANGE;
                light.shadows = LightShadows.None;
                adjustedCount++;
            }

            Debug.Log($"[HorrorAtmosphereSetup] Dimmed {adjustedCount} street lamps (intensity 3.5, range 11).");
        }

        private static void SetupFogAndAmbient()
        {
            // Fog - denser, darker, with cold blue tint
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogColor = FOG_COLOR;
            RenderSettings.fogDensity = FOG_DENSITY;

            // Near-black ambient with cold shift - shadows become truly dark
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = AMBIENT_COLOR;

            // Suppress reflections to prevent unnatural brightness in dark areas
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.reflectionIntensity = REFLECTION_INTENSITY;

            Debug.Log("[HorrorAtmosphereSetup] Fog density 0.032, ambient near-black, reflections suppressed.");
        }

        private static Transform FindChildRecursive(Transform parent, string childName)
        {
            foreach (Transform child in parent)
            {
                if (child.name == childName)
                    return child;

                Transform found = FindChildRecursive(child, childName);
                if (found != null)
                    return found;
            }
            return null;
        }
    }
}
