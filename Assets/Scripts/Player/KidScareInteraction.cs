using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Player;

/// <summary>
/// Handles the full little-girl interaction sequence:
///   1. Pre-scare dialogue (8 lines between You and Little Girl).
///   2. Horror photo jumpscare.
///   3. Post-scare dialogue (3 more lines + girl transformation narration).
///   4. Little girl fades out over 1–2 s, ghost fades in over 2 s and starts chasing.
/// Implements IInteractable for F-key interaction.
/// </summary>
public class KidScareInteraction : MonoBehaviour, IInteractable, ILockable
{
    // -------------------------------------------------------------------------
    // Dialogue lines
    // -------------------------------------------------------------------------

    private static readonly DialogueData.Line[] PreScareLines = new[]
    {
        new DialogueData.Line { speaker = "You",         text = "Hey! Are you okay?" },
        new DialogueData.Line { speaker = "Little Girl", text = "You were supposed to come home." },
        new DialogueData.Line { speaker = "You",         text = "What?" },
        new DialogueData.Line { speaker = "Little Girl", text = "You promised." },
        new DialogueData.Line { speaker = "You",         text = "I... I don't know you." },
        new DialogueData.Line { speaker = "Little Girl", text = "Yes, you do." },
        new DialogueData.Line { speaker = "You",         text = "Where are your parents?" },
        new DialogueData.Line { speaker = "Little Girl", text = "You left them too." },
    };

    private static readonly DialogueData.Line[] PostScareLines = new[]
    {
        new DialogueData.Line { speaker = "Little Girl", text = "You forgot again." },
        new DialogueData.Line { speaker = "You",         text = "Stay back." },
        new DialogueData.Line { speaker = "Little Girl", text = "You always forget." },
        new DialogueData.Line { speaker = "Little Girl", text = "Come home." },
    };

    // -------------------------------------------------------------------------
    // Inspector fields
    // -------------------------------------------------------------------------

    [Header("Assets")]
    public Texture2D scareTexture;
    public AudioClip screamClip;
    public GameObject kidGhostPrefab;

    [Header("Scene References")]
    [Tooltip("The little girl visual root to fade out after the scare.")]
    public GameObject kidVisualRoot;

    [Tooltip("The player GameObject — used to locate the ghost's target.")]
    public GameObject playerObject;

    [Header("UI Reference")]
    public RawImage scareRawImage;

    [Header("Settings")]
    public float scareDuration = 1.0f;
    public string promptText = "[ F ] Interact";

    [Tooltip("Seconds the girl takes to fade out after post-scare dialogue.")]
    public float girlFadeOutDuration = 1.5f;

    [Tooltip("Seconds the ghost takes to fade in and become fully visible.")]
    public float ghostFadeInDuration = 2.0f;

    [Tooltip("Ghost movement speed in units per second.")]
    public float ghostMoveSpeed = 10.5f;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private AudioSource _audioSource;
    private bool _isScaring;
    private bool _hasScared;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.spatialBlend = 0f;

        if (scareRawImage != null)
        {
            scareRawImage.gameObject.SetActive(false);
            if (scareTexture != null) scareRawImage.texture = scareTexture;
        }
    }

    // -------------------------------------------------------------------------
    // IInteractable
    // -------------------------------------------------------------------------

    public string GetActionName() => "Talk";
    public InteractionKey GetPreferredKey() => InteractionKey.F;

    /// <summary>Requires bartender to have been talked to first.</summary>
    public StorylineManager.Beat RequiredBeat => StorylineManager.Beat.BartenderDone;

    /// <summary>Only interactable before the sequence has started, and after bartender is done.</summary>
    public bool CanInteract() => !_isScaring && !_hasScared
        && (StorylineManager.Instance == null || StorylineManager.Instance.IsUnlocked(RequiredBeat));

    /// <summary>Kicks off the full kid scare sequence.</summary>
    public void OnInteract()
    {
        if (!CanInteract()) return;
        StartCoroutine(FullSequence());
    }

    // -------------------------------------------------------------------------
    // Sequence coroutine
    // -------------------------------------------------------------------------

    private IEnumerator FullSequence()
    {
        _isScaring = true;

        // --- Stage 1: Pre-scare dialogue ---
        yield return RunDialogue(PreScareLines);

        // --- Stage 2: Jumpscare image + sound ---
        if (scareRawImage != null)
            scareRawImage.gameObject.SetActive(true);

        if (_audioSource != null && screamClip != null)
            _audioSource.PlayOneShot(screamClip);

        yield return new WaitForSecondsRealtime(scareDuration);

        if (scareRawImage != null)
            scareRawImage.gameObject.SetActive(false);

        // --- Stage 3: Post-scare dialogue ---
        yield return RunDialogue(PostScareLines);

        // --- Stage 4: Fade girl out, spawn ghost, fade ghost in ---
        yield return TransitionGirlToGhost();

        _isScaring = false;
        _hasScared = true;

        StorylineManager.Instance?.Advance(StorylineManager.Beat.LittleGirlDone);
        ObjectiveUI.Instance?.Set(ObjectiveState.FindProtection);
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>Shows a set of dialogue lines and waits until they finish.</summary>
    private IEnumerator RunDialogue(DialogueData.Line[] lines)
    {
        DialogueUI ui = DialogueUI.Instance;
        if (ui == null) yield break;

        bool done = false;
        DialogueData data = ScriptableObject.CreateInstance<DialogueData>();
        data.lines = lines;

        ui.OnDialogueEnded += OnDone;
        ui.StartDialogue(data);

        void OnDone() { done = true; ui.OnDialogueEnded -= OnDone; }
        yield return new WaitUntil(() => done);
    }

    /// <summary>
    /// Fades the girl renderers to alpha 0 over girlFadeOutDuration,
    /// spawns the ghost invisible, then fades it in over ghostFadeInDuration.
    /// </summary>
    private IEnumerator TransitionGirlToGhost()
    {
        // Collect all renderers on the girl visual
        Renderer[] girlRenderers = kidVisualRoot != null
            ? kidVisualRoot.GetComponentsInChildren<Renderer>(includeInactive: false)
            : System.Array.Empty<Renderer>();

        // Cache original materials and make them support transparency
        Material[][] origMats = CacheMaterialsForFade(girlRenderers);

        // Spawn ghost immediately but invisible
        GameObject ghost = SpawnGhost(startInvisible: true);
        Renderer[] ghostRenderers = ghost != null
            ? ghost.GetComponentsInChildren<Renderer>(includeInactive: false)
            : System.Array.Empty<Renderer>();
        Material[][] ghostMats = CacheMaterialsForFade(ghostRenderers);

        // Fade girl OUT — use unscaled time: runs regardless of timeScale
        float elapsed = 0f;
        while (elapsed < girlFadeOutDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(1f - elapsed / girlFadeOutDuration);
            if (float.IsFinite(alpha))
            {
                SetRenderersAlpha(girlRenderers, alpha);
            }
            yield return null;
        }

        if (kidVisualRoot != null)
            kidVisualRoot.SetActive(false);

        // Fade ghost IN — use unscaled time
        elapsed = 0f;
        while (elapsed < ghostFadeInDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float alpha = Mathf.Clamp01(elapsed / ghostFadeInDuration);
            if (float.IsFinite(alpha))
            {
                SetRenderersAlpha(ghostRenderers, alpha);
            }
            yield return null;
        }

        // Restore ghost materials to full opacity (removes fade setup)
        RestoreMaterials(ghostRenderers, ghostMats);
    }

    /// <summary>
    /// Instantiates the ghost prefab and adds required components.
    /// If startInvisible is true, sets all renderer alpha to 0.
    /// </summary>
    private GameObject SpawnGhost(bool startInvisible)
    {
        if (kidGhostPrefab == null)
        {
            Debug.LogWarning("[KidScareInteraction] kidGhostPrefab is not assigned.");
            return null;
        }

        GameObject ghost = Instantiate(kidGhostPrefab, transform.position, transform.rotation);

        Transform playerTransform = null;
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
        }
        else
        {
            PlayerMovement movement = FindAnyObjectByType<PlayerMovement>();
            if (movement != null) playerTransform = movement.transform;
        }

        KidGhostMover mover = ghost.AddComponent<KidGhostMover>();
        mover.Initialize(playerTransform, ghostMoveSpeed);

        HealthComponent ghostHealth = ghost.AddComponent<HealthComponent>();
        ghost.AddComponent<GhostHealthController>();

        // Set fully transparent if fading in
        if (startInvisible)
        {
            Renderer[] renderers = ghost.GetComponentsInChildren<Renderer>(includeInactive: false);
            CacheMaterialsForFade(renderers);
            SetRenderersAlpha(renderers, 0f);
        }

        return ghost;
    }

    // -------------------------------------------------------------------------
    // Material fade helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Switches all materials to Transparent rendering mode and caches originals.
    /// Returns a jagged array of the original material arrays per renderer.
    /// </summary>
    private static Material[][] CacheMaterialsForFade(Renderer[] renderers)
    {
        Material[][] cache = new Material[renderers.Length][];
        for (int i = 0; i < renderers.Length; i++)
        {
            cache[i] = renderers[i].materials;
            // Switch each material instance to fade-capable mode
            foreach (Material mat in renderers[i].materials)
                EnableFadeMode(mat);
        }
        return cache;
    }

    /// <summary>Sets the alpha of all renderer materials to the given value.</summary>
    private static void SetRenderersAlpha(Renderer[] renderers, float alpha)
    {
        foreach (Renderer r in renderers)
        {
            foreach (Material mat in r.materials)
            {
                if (mat.HasProperty("_BaseColor"))
                {
                    Color c = mat.GetColor("_BaseColor");
                    c.a = alpha;
                    mat.SetColor("_BaseColor", c);
                }
                else
                {
                    Color c = mat.color;
                    c.a = alpha;
                    mat.color = c;
                }
            }
        }
    }

    /// <summary>Restores the original shared materials on each renderer.</summary>
    private static void RestoreMaterials(Renderer[] renderers, Material[][] cache)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            if (i < cache.Length)
            {
                renderers[i].materials = cache[i];
                // Force Opaque mode if they were opaque originally (safety)
                foreach (Material m in renderers[i].materials)
                {
                    if (m != null && m.HasProperty("_Surface") && m.GetFloat("_Surface") == 0)
                    {
                        m.SetInt("_ZWrite", 1);
                        m.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                        m.renderQueue = -1; // Default
                    }
                }
            }
        }
    }

    /// <summary>
    /// Configures a URP/Standard material instance to support alpha transparency.
    /// </summary>
    private static void EnableFadeMode(Material mat)
    {
        if (mat == null) return;

        // URP Lit shader surface type
        if (mat.HasProperty("_Surface"))
        {
            mat.SetFloat("_Surface", 1f);   // 1 = Transparent
            mat.SetFloat("_Blend", 0f);     // 0 = Alpha
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        // Standard shader fallback
        mat.SetInt("_SrcBlend",  (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend",  (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite",    0);
        mat.EnableKeyword("_ALPHABLEND_ON");
    }
}
