using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LevelTransitionOnDrink : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "Level 2";
    [SerializeField] private float fadeDuration = 1.5f;

    /// <summary>Called by PlayerPickupController when the player finishes drinking this item.</summary>
    public void OnDrunk()
    {
        Debug.Log($"[LevelTransitionOnDrink] Drinking complete. Starting fade to black then loading '{targetSceneName}'.");
        ScreenFadeUI.Instance.FadeToBlack(fadeDuration, () => LoadTarget());
    }

    private void LoadTarget()
    {
        if (SceneUtility.GetBuildIndexByScenePath($"Assets/Scenes/{targetSceneName}.unity") < 0)
        {
            Debug.LogError($"[LevelTransitionOnDrink] Scene '{targetSceneName}' is not in Build Settings. " +
                           "Add it via File > Build Settings before running.");
            return;
        }

        Debug.Log($"[LevelTransitionOnDrink] Loading scene '{targetSceneName}'.");
        SceneManager.LoadScene(targetSceneName);
    }
}
