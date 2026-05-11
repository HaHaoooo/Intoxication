using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;

public class ScreenFadeUI : MonoBehaviour
{
    private static ScreenFadeUI _instance;
    public static ScreenFadeUI Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("ScreenFadeUI");
                Canvas canvas = go.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 999;
                go.AddComponent<CanvasScaler>();
                go.AddComponent<GraphicRaycaster>();
                
                GameObject panel = new GameObject("FadePanel");
                panel.transform.SetParent(go.transform, false);
                Image img = panel.AddComponent<Image>();
                img.color = new Color(0, 0, 0, 0);
                RectTransform rect = panel.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
                
                _instance = go.AddComponent<ScreenFadeUI>();
                _instance.fadeImage = img;
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private Image fadeImage;

    public void FadeToBlack(float duration, Action onComplete = null)
    {
        StartCoroutine(FadeRoutine(1f, duration, onComplete));
    }

    public void FadeFromBlack(float duration, Action onComplete = null)
    {
        StartCoroutine(FadeRoutine(0f, duration, onComplete));
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration, Action onComplete)
    {
        float startAlpha = fadeImage.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        fadeImage.color = new Color(0, 0, 0, targetAlpha);
        onComplete?.Invoke();
    }
}
