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
                _instance._fadeImage = img;
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private Image _fadeImage;
    private Coroutine _activeRoutine;

    /// <summary>Fades the screen to black over the given duration, then invokes onComplete.</summary>
    public void FadeToBlack(float duration, Action onComplete = null)
    {
        StopActiveRoutine();
        _activeRoutine = StartCoroutine(FadeRoutine(1f, duration, onComplete));
    }

    /// <summary>Fades the screen from black to clear over the given duration, then invokes onComplete.</summary>
    public void FadeFromBlack(float duration, Action onComplete = null)
    {
        StopActiveRoutine();
        _activeRoutine = StartCoroutine(FadeRoutine(0f, duration, onComplete));
    }

    /// <summary>Instantly sets the screen to fully black without animation.</summary>
    public void SetBlack()
    {
        StopActiveRoutine();
        if (_fadeImage != null)
            _fadeImage.color = new Color(0, 0, 0, 1f);
    }

    /// <summary>Instantly clears the fade overlay without animation.</summary>
    public void SetClear()
    {
        StopActiveRoutine();
        if (_fadeImage != null)
            _fadeImage.color = new Color(0, 0, 0, 0f);
    }

    private void StopActiveRoutine()
    {
        if (_activeRoutine != null)
        {
            StopCoroutine(_activeRoutine);
            _activeRoutine = null;
        }
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration, Action onComplete)
    {
        float startAlpha = _fadeImage.color.a;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            _fadeImage.color = new Color(0, 0, 0, alpha);
            yield return null;
        }

        _fadeImage.color = new Color(0, 0, 0, targetAlpha);
        _activeRoutine = null;
        onComplete?.Invoke();
    }
}
