using UnityEngine;

public class AutoFadeIn : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 1.0f;
    [SerializeField] private float delay = 0.5f;

    private void Start()
    {
        Invoke(nameof(DoFadeIn), delay);
    }

    private void DoFadeIn()
    {
        ScreenFadeUI.Instance.FadeFromBlack(fadeDuration);
    }
}
