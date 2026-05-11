using UnityEngine;
using Player;

public class FinalBottleInteraction : MonoBehaviour, IInteractable
{
    public string GetActionName() => "Put it down";
    public InteractionKey GetPreferredKey() => InteractionKey.F;
    public bool CanInteract() => true;

    public void OnInteract()
    {
        // Start Ending
        StartCoroutine(EndingRoutine());
    }

    private System.Collections.IEnumerator EndingRoutine()
    {
        // Fade out
        ScreenFadeUI.Instance.FadeToBlack(3f);
        yield return new WaitForSeconds(4f);
        
        // Final Text could be a UI element or just a log for now, 
        // but I'll add a simple overlay.
        Debug.Log("ENDING: Sometimes the scariest thing is not what you see. It is what your mind creates when you lose control.");
        
        // Quit or return to menu
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #else
        Application.Quit();
        #endif
    }
}
