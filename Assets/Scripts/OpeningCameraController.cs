using UnityEngine;
using UnityEngine.Playables;
using Player;

/// <summary>
/// Placeholder for the opening camera controller.
/// The OpeningCamera GameObject uses a PlayableDirector (Timeline) for the intro sequence.
/// This component previously held a player reference; it is retained so the scene
/// serialized data reconnects without missing-script warnings.
/// </summary>
public class OpeningCameraController : MonoBehaviour
{
    [Tooltip("Reference to the player Transform for the opening camera sequence.")]
    [SerializeField] private Transform player;

    private PlayableDirector _director;
    private PlayerMovement _playerMovement;

    private void Awake()
    {
        _director = GetComponent<PlayableDirector>();
        if (player != null)
        {
            _playerMovement = player.GetComponent<PlayerMovement>();
        }
    }

    private void OnEnable()
    {
        if (_director != null)
        {
            _director.played += OnTimelineStarted;
            _director.stopped += OnTimelineStopped;
        }

        // If it starts playing immediately
        if (_director != null && _director.state == PlayState.Playing)
        {
            OnTimelineStarted(_director);
        }
    }

    private void OnDisable()
    {
        if (_director != null)
        {
            _director.played -= OnTimelineStarted;
            _director.stopped -= OnTimelineStopped;
        }
    }

    private void OnTimelineStarted(PlayableDirector pd)
    {
        if (_playerMovement != null)
        {
            _playerMovement.canRotateCamera = false;
        }
    }

    private void OnTimelineStopped(PlayableDirector pd)
    {
        if (_playerMovement != null)
        {
            _playerMovement.canRotateCamera = true;
        }
    }
}
