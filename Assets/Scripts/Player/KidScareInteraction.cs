using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;
using Player;

/// <summary>
/// Handles the jump scare interaction with the kid.
/// After the scare image fades:
///   1. The little girl visual is hidden.
///   2. The kid ghost is spawned and starts chasing the player.
/// Implements IInteractable to integrate with the project's F-key interaction system.
/// </summary>
public class KidScareInteraction : MonoBehaviour, IInteractable
{
    [Header("Assets")]
    public Texture2D scareTexture;
    public AudioClip screamClip;
    public GameObject kidGhostPrefab;

    [Header("Scene References")]
    [Tooltip("The little girl visual root to hide after the scare. Assign the 'model' child here.")]
    public GameObject kidVisualRoot;

    [Tooltip("The player GameObject — used to locate the ghost's target.")]
    public GameObject playerObject;

    [Header("UI Reference")]
    public RawImage scareRawImage;

    [Header("Settings")]
    public float scareDuration = 1.0f;
    public string promptText = "[ F ] Interact";

    [Tooltip("Ghost movement speed in units per second.")]
    public float ghostMoveSpeed = 10.5f;

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

        if (scareRawImage != null)
            scareRawImage.gameObject.SetActive(true);

        if (_audioSource != null && screamClip != null)
            _audioSource.PlayOneShot(screamClip);

        yield return new WaitForSeconds(scareDuration);

        if (scareRawImage != null)
            scareRawImage.gameObject.SetActive(false);

        // Hide the little girl visual
        if (kidVisualRoot != null)
            kidVisualRoot.SetActive(false);

        // Spawn the kid ghost and set it in motion
        SpawnKidGhost();

        _isScaring = false;
        _hasScared = true;
    }

    private void SpawnKidGhost()
    {
        if (kidGhostPrefab == null)
        {
            Debug.LogWarning("[KidScareInteraction] kidGhostPrefab is not assigned.");
            return;
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
            if (movement != null)
                playerTransform = movement.transform;
        }

        KidGhostMover mover = ghost.AddComponent<KidGhostMover>();
        mover.Initialize(playerTransform, ghostMoveSpeed);

        // Health system (no UI)
        ghost.AddComponent<HealthComponent>();
        ghost.AddComponent<GhostHealthController>();
    }
}
