using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using Player;

/// <summary>
/// Handles the jump scare interaction with the kid.
/// After the scare image fades:
///   1. A brief warning message is shown ("Don't let her touch you").
///   2. The little girl visual is hidden.
///   3. The kid ghost is spawned and starts chasing the player.
/// Implements IInteractable to integrate with the project's F-key interaction system.
/// </summary>
public class KidScareInteraction : MonoBehaviour, IInteractable
{
    private const float WarningDuration = 3.0f;
    private const int WarningFontSize = 32;

    [Header("Assets")]
    public Texture2D scareTexture;
    public AudioClip screamClip;
    public GameObject kidGhostPrefab;

    [Header("Scene References")]
    [Tooltip("The little girl visual root to hide after the scare. Assign the 'model' child here.")]
    public GameObject kidVisualRoot;

    [Tooltip("The player GameObject — used to locate the ghost's target and read max speed.")]
    public GameObject playerObject;

    [Header("UI Reference")]
    public RawImage scareRawImage;

    [Header("Settings")]
    public float scareDuration = 1.0f;
    public string promptText = "[ F ] Interact";

    [Tooltip("Ghost movement speed in units per second. Will be clamped to the player's walk speed.")]
    public float ghostMoveSpeed = 2.5f;

    private AudioSource _audioSource;
    private bool _isScaring = false;
    private bool _hasScared = false;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();

        // Ensure audio is 2D for maximum-impact jump scare
        _audioSource.spatialBlend = 0f;

        if (scareRawImage != null)
        {
            scareRawImage.gameObject.SetActive(false);
            if (scareTexture != null) scareRawImage.texture = scareTexture;
        }
    }

    // --- IInteractable Implementation ---

    /// <summary>Returns the interaction prompt shown to the player.</summary>
    public string GetInteractionPrompt()
    {
        return promptText;
    }

    /// <summary>Triggers the scare sequence when the player interacts.</summary>
    public void OnInteract()
    {
        if (!_isScaring && !_hasScared)
        {
            StartCoroutine(TriggerScare());
        }
    }

    /// <summary>Only interactable once — before the scare has been triggered.</summary>
    public bool CanInteract()
    {
        return !_isScaring && !_hasScared;
    }

    // --- Scare Logic ---

    private IEnumerator TriggerScare()
    {
        _isScaring = true;

        // Show the jumpscare image
        if (scareRawImage != null)
        {
            scareRawImage.gameObject.SetActive(true);
        }

        if (_audioSource != null && screamClip != null)
        {
            _audioSource.PlayOneShot(screamClip);
        }

        yield return new WaitForSeconds(scareDuration);

        // Hide the jumpscare image
        if (scareRawImage != null)
        {
            scareRawImage.gameObject.SetActive(false);
        }

        // Show warning text before the ghost appears
        GameObject warningOverlay = BuildWarningOverlay();

        yield return new WaitForSeconds(WarningDuration);

        if (warningOverlay != null) Destroy(warningOverlay);

        // Hide the little girl visual
        if (kidVisualRoot != null)
        {
            kidVisualRoot.SetActive(false);
        }

        // Spawn the kid ghost and set it in motion
        SpawnKidGhost();

        _isScaring = false;
        _hasScared = true;
    }

    /// <summary>Builds a centered warning text on the existing Canvas.</summary>
    private GameObject BuildWarningOverlay()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[KidScareInteraction] No Canvas found — cannot show warning text.");
            return null;
        }

        GameObject overlay = new GameObject("GhostWarningOverlay");
        overlay.transform.SetParent(canvas.transform, false);

        RectTransform overlayRect = overlay.AddComponent<RectTransform>();
        overlayRect.anchorMin = new Vector2(0.1f, 0.35f);
        overlayRect.anchorMax = new Vector2(0.9f, 0.65f);
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Text text = overlay.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = "Don't let her touch you.";
        text.fontSize = WarningFontSize;
        text.fontStyle = FontStyle.BoldAndItalic;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = new Color(0.9f, 0.1f, 0.1f);
        text.raycastTarget = false;

        return overlay;
    }

    private void SpawnKidGhost()
    {
        if (kidGhostPrefab == null)
        {
            Debug.LogWarning("[KidScareInteraction] kidGhostPrefab is not assigned.");
            return;
        }

        // Spawn at the kid's current world position
        GameObject ghost = Instantiate(kidGhostPrefab, transform.position, transform.rotation);

        // Resolve the player's walk speed cap from PlayerMovement
        float playerMaxSpeed = float.MaxValue;
        Transform playerTransform = null;

        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
            PlayerMovement movement = playerObject.GetComponent<PlayerMovement>();
            if (movement != null)
            {
                playerMaxSpeed = movement.walkSpeed;
            }
        }
        else
        {
            // Fallback: search the scene for PlayerMovement
            PlayerMovement movement = FindAnyObjectByType<PlayerMovement>();
            if (movement != null)
            {
                playerTransform = movement.transform;
                playerMaxSpeed = movement.walkSpeed;
            }
        }

        KidGhostMover mover = ghost.AddComponent<KidGhostMover>();
        mover.Initialize(playerTransform, ghostMoveSpeed, playerMaxSpeed);

        // Health system
        ghost.AddComponent<HealthComponent>();  // Awake sets CurrentHealth = maxHealth (100)
        ghost.AddComponent<GhostHealthController>();

        // Ghost health bar — top-right corner, pale green fill
        HealthBarUI ghostBar = ghost.AddComponent<HealthBarUI>();
        ghostBar.barLabel = "Ghost";
        ghostBar.fillColor = new Color(0.3f, 0.85f, 0.3f);
        ghostBar.screenAnchorMin = new Vector2(1f, 1f);
        ghostBar.screenAnchorMax = new Vector2(1f, 1f);
        ghostBar.screenPivot = new Vector2(1f, 1f);
        ghostBar.screenOffset = new Vector2(-20f, -20f);
        // HealthBarUI.Start() runs next frame, finds HealthComponent on same object, auto-binds
    }
}
