using UnityEngine;

/// <summary>
/// Loads the saved master volume on scene start.
/// Attach to any always-active GameObject in each gameplay scene,
/// or use [RuntimeInitializeOnLoadMethod] for automatic global loading.
/// </summary>
public class VolumeBootstrap : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InitVolume()
    {
        VolumeSettings.Load();
    }
}
