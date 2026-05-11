using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTransitionOnDrink : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "Level 2";
    [SerializeField] private float fadeDuration = 1.5f;
    
    public void OnDrunk()
    {
        Debug.Log($"[LevelTransitionOnDrink] Drinking complete. Starting fade to black...");
        ScreenFadeUI.Instance.FadeToBlack(fadeDuration, () => {
            Debug.Log($"[LevelTransitionOnDrink] Fade complete. Loading {targetSceneName}...");
            SceneManager.LoadScene(targetSceneName);
        });
    }
}
