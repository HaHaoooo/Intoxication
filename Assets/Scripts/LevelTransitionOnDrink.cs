using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelTransitionOnDrink : MonoBehaviour
{
    [SerializeField] private string targetSceneName = "Level 2";
    
    public void OnDrunk()
    {
        Debug.Log($"[LevelTransitionOnDrink] Drinking complete. Loading {targetSceneName}...");
        SceneManager.LoadScene(targetSceneName);
    }
}
