using System.Collections;
using UnityEngine;

/// <summary>
/// Attach to the MissingChildPoster GameObject alongside its Renderer.
/// After the radio event fires (RadioTriggerGate.IsReady), polls until the player is close,
/// then briefly swaps the poster texture to the "little girl" texture for ~1 second,
/// then swaps back — creating the face-flicker horror moment.
/// </summary>
public class MissingPosterFlicker : MonoBehaviour
{
    [Header("Textures")]
    [Tooltip("The little girl's face texture to flash on the poster.")]
    [SerializeField] private Texture2D girlFaceTexture;

    [Tooltip("Trigger distance — flicker fires when player is closer than this.")]
    [SerializeField] private float triggerDistance = 4f;

    [Tooltip("Duration (seconds) the girl's face is shown before reverting.")]
    [SerializeField] private float flickerDuration = 1.0f;

    private Renderer _renderer;
    private Texture _originalTexture;
    private bool _hasFlickered;

    private void Start()
    {
        _renderer = GetComponent<Renderer>();
        if (_renderer != null && _renderer.material != null)
            _originalTexture = _renderer.material.mainTexture;

        StartCoroutine(WatchForFlicker());
    }

    private IEnumerator WatchForFlicker()
    {
        WaitForSeconds wait = new WaitForSeconds(0.25f);
        Transform playerTransform = null;

        while (!_hasFlickered)
        {
            // Wait until the radio event has fired
            if (!RadioTriggerGate.IsReady)
            {
                yield return wait;
                continue;
            }

            if (playerTransform == null)
            {
                var pm = FindAnyObjectByType<Player.PlayerMovement>();
                if (pm != null) playerTransform = pm.transform;
            }

            if (playerTransform != null)
            {
                float dist = Vector3.Distance(transform.position, playerTransform.position);
                if (dist <= triggerDistance)
                {
                    _hasFlickered = true;
                    StartCoroutine(DoFlicker());
                    yield break;
                }
            }

            yield return wait;
        }
    }

    private IEnumerator DoFlicker()
    {
        if (_renderer == null || girlFaceTexture == null) yield break;

        _renderer.material.mainTexture = girlFaceTexture;
        yield return new WaitForSeconds(flickerDuration);
        _renderer.material.mainTexture = _originalTexture;

        Debug.Log("[MissingPosterFlicker] Face flicker complete — reverted to original texture.");
    }
}
