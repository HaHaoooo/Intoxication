using UnityEngine;
using UnityEngine.SceneManagement;

namespace Weather
{
    /// <summary>
    /// Spawns a basic rain system in scenes that contain a player-tagged object.
    /// This keeps the feature low-risk and avoids manual scene rewiring.
    /// </summary>
    public static class RainBootstrap
    {
        private static bool _registered;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void RegisterSceneHook()
        {
            // Guard against duplicate registration across domain reloads in the Editor.
            if (_registered) return;
            _registered = true;

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!scene.isLoaded || Object.FindAnyObjectByType<RainSystem>() != null)
            {
                return;
            }

            GameObject player = GameObject.FindWithTag("Player");
            if (player == null)
            {
                return;
            }

            GameObject rainObject = new GameObject("Rain");
            rainObject.transform.position = player.transform.position + Vector3.up * 18f;
            ParticleSystem ps = rainObject.AddComponent<ParticleSystem>();
            // ParticleSystem automatically adds a ParticleSystemRenderer, so we don't add it manually.

            RainSystem rainSystem = rainObject.AddComponent<RainSystem>();
            rainSystem.followTarget = player.transform;
            rainSystem.intensity = 0.5f;
            rainSystem.maxEmissionRate = 600f;
            rainSystem.transitionSpeed = 2.0f;
            rainSystem.spawnWidth = 40f;
            rainSystem.spawnLength = 40f;
            rainSystem.spawnHeight = 15f;
        }
    }
}
