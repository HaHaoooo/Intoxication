using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Displays player and ghost health bars during the ghost chase sequence.
/// Shown when the ghost spawns (via KidScareInteraction) and hidden when the ghost dies.
/// Singleton — lives on a persistent UI GameObject inside the Canvas.
/// </summary>
public class CombatHealthBarUI : MonoBehaviour
{
    public static CombatHealthBarUI Instance { get; private set; }

    [Header("Player Bar")]
    [Tooltip("Fill image for the player health bar (Image Type: Filled).")]
    [SerializeField] private Image playerFill;
    [SerializeField] private Text playerLabel;

    [Header("Ghost Bar")]
    [Tooltip("Fill image for the ghost health bar (Image Type: Filled).")]
    [SerializeField] private Image ghostFill;
    [SerializeField] private Text ghostLabel;

    [Header("Container")]
    [Tooltip("Root GameObject that wraps both bars — toggled on/off.")]
    [SerializeField] private GameObject containerRoot;

    private HealthComponent _playerHealth;
    private HealthComponent _ghostHealth;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (containerRoot != null)
            containerRoot.SetActive(false);
    }

    private void OnDestroy()
    {
        Unsubscribe();
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Subscribes to both HealthComponents and reveals the bars.
    /// Call this immediately after the ghost is spawned.
    /// </summary>
    public void Show(HealthComponent playerHealth, HealthComponent ghostHealth)
    {
        Unsubscribe();

        _playerHealth = playerHealth;
        _ghostHealth = ghostHealth;

        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChanged += OnPlayerHealthChanged;
            RefreshBar(playerFill, playerLabel, _playerHealth.CurrentHealth, _playerHealth.maxHealth);
        }

        if (_ghostHealth != null)
        {
            _ghostHealth.OnHealthChanged += OnGhostHealthChanged;
            RefreshBar(ghostFill, ghostLabel, _ghostHealth.CurrentHealth, _ghostHealth.maxHealth);
        }

        if (containerRoot != null)
            containerRoot.SetActive(true);
    }

    /// <summary>Hides the bars and unsubscribes from health events.</summary>
    public void Hide()
    {
        Unsubscribe();
        if (containerRoot != null)
            containerRoot.SetActive(false);
    }

    private void OnPlayerHealthChanged(float current, float max)
        => RefreshBar(playerFill, playerLabel, current, max);

    private void OnGhostHealthChanged(float current, float max)
        => RefreshBar(ghostFill, ghostLabel, current, max);

    private static void RefreshBar(Image fill, Text label, float current, float max)
    {
        if (fill != null)
            fill.fillAmount = max > 0f ? current / max : 0f;

        if (label != null)
            label.text = $"{Mathf.CeilToInt(current)} / {Mathf.CeilToInt(max)}";
    }

    private void Unsubscribe()
    {
        if (_playerHealth != null)
            _playerHealth.OnHealthChanged -= OnPlayerHealthChanged;

        if (_ghostHealth != null)
            _ghostHealth.OnHealthChanged -= OnGhostHealthChanged;

        _playerHealth = null;
        _ghostHealth = null;
    }
}
