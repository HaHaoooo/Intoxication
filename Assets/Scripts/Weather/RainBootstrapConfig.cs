using UnityEngine;

namespace Weather
{
    /// <summary>
    /// Holds a reference to a rain configuration ScriptableObject.
    /// Attach to a rain GameObject so scene-specific rain settings can be stored
    /// as an asset and reused across scenes.
    /// </summary>
    public class RainBootstrapConfig : MonoBehaviour
    {
        [Tooltip("ScriptableObject with rain configuration data.")]
        [SerializeField] private ScriptableObject config;

        /// <summary>Returns the assigned configuration asset.</summary>
        public ScriptableObject Config => config;
    }
}
