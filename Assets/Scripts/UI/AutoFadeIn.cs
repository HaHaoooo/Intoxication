using System.Collections;
using UnityEngine;

public class AutoFadeIn : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 1.0f;
    [SerializeField] private float delay = 0.5f;

    private void Start()
    {
        StartCoroutine(DelayedFadeIn());
    }

    private IEnumerator DelayedFadeIn()
    {
        // Use unscaled time so the fade-in works regardless of Time.timeScale.
        yield return new WaitForSecondsRealtime(delay);
        ScreenFadeUI.Instance.FadeFromBlack(fadeDuration);
    }
}
