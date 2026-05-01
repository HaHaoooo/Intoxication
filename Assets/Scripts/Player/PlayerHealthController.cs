using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Handles player-specific health behaviour:
/// — Damage intake with a 1-second invincibility window after each hit.
/// — Death sequence: shows a full-screen overlay then restarts the scene.
/// Requires a HealthComponent on the same GameObject.
/// </summary>
[RequireComponent(typeof(HealthComponent))]
public class PlayerHealthController : MonoBehaviour
{
    private const float RestartDelay = 2.5f;
    private const int DeathFontSize = 36;

    [Header("Settings")]
    [Tooltip("Damage dealt to the player per ghost touch.")]
    public float damagePerHit = 20f;

    [Tooltip("Seconds the player is invincible after being hit.")]
    public float invincibilityDuration = 1f;

    private HealthComponent _health;
    private bool _isInvincible = false;

    private void Awake()
    {
        _health = GetComponent<HealthComponent>();
    }

    private void Start()
    {
        _health.OnDeath += HandleDeath;
    }

    /// <summary>
    /// Attempts to deal one hit of damage to the player.
    /// Returns false and does nothing if invincibility is active.
    /// </summary>
    public bool TryTakeDamage()
    {
        if (_isInvincible || !_health.IsAlive) return false;

        _health.TakeDamage(damagePerHit);

        if (_health.IsAlive)
            StartCoroutine(InvincibilityWindow());

        return true;
    }

    private IEnumerator InvincibilityWindow()
    {
        _isInvincible = true;
        yield return new WaitForSeconds(invincibilityDuration);
        _isInvincible = false;
    }

    private void HandleDeath()
    {
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        GameObject overlay = BuildDeathOverlay();
        yield return new WaitForSeconds(RestartDelay);
        if (overlay != null) Destroy(overlay);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>Full-screen dark overlay shown on player death.</summary>
    private GameObject BuildDeathOverlay()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return null;

        GameObject overlay = new GameObject("PlayerDeathOverlay");
        overlay.transform.SetParent(canvas.transform, false);

        RectTransform rt = overlay.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image bg = overlay.AddComponent<Image>();
        bg.color = new Color(0f, 0f, 0f, 0.85f);

        GameObject textGO = new GameObject("DeathText");
        textGO.transform.SetParent(overlay.transform, false);

        RectTransform textRect = textGO.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.1f, 0.3f);
        textRect.anchorMax = new Vector2(0.9f, 0.7f);
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textGO.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = "She got you.\nRestarting...";
        text.fontSize = DeathFontSize;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.85f, 0.1f, 0.1f);
        text.raycastTarget = false;

        return overlay;
    }
}
