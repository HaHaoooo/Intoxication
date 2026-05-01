using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// Handles the jump scare interaction with the kid.
/// Implements IInteractable to integrate with the project's F-key interaction system.
/// </summary>
public class KidScareInteraction : MonoBehaviour, IInteractable
{
    [Header("Assets")]
    public Texture2D scareTexture;
    public AudioClip screamClip;
    
    [Header("UI Reference")]
    public RawImage scareRawImage;
    
    [Header("Settings")]
    public float scareDuration = 1.0f;
    public string promptText = "[ F ] Interact";

    private AudioSource _audioSource;
    private bool _isScaring = false;

    private void Start()
    {
        _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) _audioSource = gameObject.AddComponent<AudioSource>();
        
        // Ensure audio is 2D for max volume jump scare
        _audioSource.spatialBlend = 0f;

        if (scareRawImage != null)
        {
            scareRawImage.gameObject.SetActive(false);
            if (scareTexture != null) scareRawImage.texture = scareTexture;
        }
    }

    // --- IInteractable Implementation ---

    public string GetInteractionPrompt()
    {
        return promptText;
    }

    public void OnInteract()
    {
        if (!_isScaring)
        {
            StartCoroutine(TriggerScare());
        }
    }

    public bool CanInteract()
    {
        return !_isScaring;
    }

    // --- Scare Logic ---

    private IEnumerator TriggerScare()
    {
        _isScaring = true;
        
        if (scareRawImage != null)
        {
            scareRawImage.gameObject.SetActive(true);
        }

        if (_audioSource != null && screamClip != null)
        {
            _audioSource.PlayOneShot(screamClip);
        }

        yield return new WaitForSeconds(scareDuration);

        if (scareRawImage != null)
        {
            scareRawImage.gameObject.SetActive(false);
        }

        _isScaring = false;
    }
}