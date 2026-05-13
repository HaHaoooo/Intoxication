using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen ending overlay that fades in sentences one at a time, then invokes a callback.
/// Builds its own canvas at sort order 1000 so it always renders on top of everything.
/// Call Show() to start the sequence.
/// </summary>
public class EndingTextUI : MonoBehaviour
{
    private static EndingTextUI _instance;

    /// <summary>Singleton accessor — auto-creates the object if absent.</summary>
    public static EndingTextUI Instance
    {
        get
        {
            if (_instance == null)
            {
                GameObject go = new GameObject("EndingTextUI");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<EndingTextUI>();
                _instance.BuildCanvas();
            }
            return _instance;
        }
    }

    // ── Tweakable timings ─────────────────────────────────────────────────────

    private const float SentenceFadeIn    = 1.2f;
    private const float SentenceHoldTime  = 2.2f;
    private const float SentenceFadeOut   = 0.8f;
    private const float GapBetween        = 0.4f;

    // ── Internal UI ───────────────────────────────────────────────────────────

    private Canvas    _canvas;
    private Image     _background;
    private Text      _label;
    private Coroutine _activeRoutine;

    // ── Public API ────────────────────────────────────────────────────────────

    /// <summary>
    /// Shows the given sentences one at a time on a black screen, then calls onComplete.
    /// </summary>
    public void Show(IList<string> sentences, System.Action onComplete = null)
    {
        // Must be active before StartCoroutine can run on this GameObject.
        gameObject.SetActive(true);

        if (_activeRoutine != null) StopCoroutine(_activeRoutine);
        _activeRoutine = StartCoroutine(Sequence(sentences, onComplete));
    }

    // ── Canvas construction ───────────────────────────────────────────────────

    private void BuildCanvas()
    {
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode  = RenderMode.ScreenSpaceOverlay;
        _canvas.sortingOrder = 1000;
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        // Black background panel
        GameObject bgGO = new GameObject("Background");
        bgGO.transform.SetParent(transform, false);
        _background = bgGO.AddComponent<Image>();
        _background.color = new Color(0f, 0f, 0f, 0f);
        RectTransform bgRect = bgGO.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.sizeDelta  = Vector2.zero;

        // Centred text label
        GameObject labelGO = new GameObject("SentenceLabel");
        labelGO.transform.SetParent(transform, false);
        RectTransform labelRect = labelGO.AddComponent<RectTransform>();
        labelRect.anchorMin      = new Vector2(0.1f, 0.35f);
        labelRect.anchorMax      = new Vector2(0.9f, 0.65f);
        labelRect.offsetMin      = Vector2.zero;
        labelRect.offsetMax      = Vector2.zero;

        _label = labelGO.AddComponent<Text>();
        _label.font              = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        _label.fontSize          = 32;
        _label.fontStyle         = FontStyle.Italic;
        _label.alignment         = TextAnchor.MiddleCenter;
        _label.horizontalOverflow = HorizontalWrapMode.Wrap;
        _label.verticalOverflow   = VerticalWrapMode.Overflow;
        _label.color             = new Color(1f, 1f, 1f, 0f);
        _label.raycastTarget     = false;

        gameObject.SetActive(false);
    }

    // ── Sequence coroutine ────────────────────────────────────────────────────

    private IEnumerator Sequence(IList<string> sentences, System.Action onComplete)
    {
        // Fade background to black first
        yield return FadeBackground(0f, 1f, 0.8f);

        foreach (string sentence in sentences)
        {
            _label.text = sentence;

            // Fade text in
            yield return FadeText(0f, 1f, SentenceFadeIn);
            yield return new WaitForSecondsRealtime(SentenceHoldTime);
            // Fade text out
            yield return FadeText(1f, 0f, SentenceFadeOut);
            yield return new WaitForSecondsRealtime(GapBetween);
        }

        // Brief pause before handing off
        yield return new WaitForSecondsRealtime(0.6f);

        _activeRoutine = null;
        onComplete?.Invoke();
    }

    // ── Fade helpers ──────────────────────────────────────────────────────────

    private IEnumerator FadeBackground(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(from, to, elapsed / duration);
            _background.color = new Color(0f, 0f, 0f, a);
            yield return null;
        }
        _background.color = new Color(0f, 0f, 0f, to);
    }

    private IEnumerator FadeText(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float a = Mathf.Lerp(from, to, elapsed / duration);
            _label.color = new Color(1f, 1f, 1f, a);
            yield return null;
        }
        _label.color = new Color(1f, 1f, 1f, to);
    }
}
