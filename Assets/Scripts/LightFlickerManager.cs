using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls all LightFlicker instances in the scene.
/// Periodically picks 1-3 random lights near the player to dim.
/// Drives all active flicker ticks centrally to avoid per-light Update() overhead.
/// </summary>
public class LightFlickerManager : MonoBehaviour
{
    [Tooltip("Minimum seconds between flicker batches.")]
    [SerializeField] private float minInterval = 3f;

    [Tooltip("Maximum seconds between flicker batches.")]
    [SerializeField] private float maxInterval = 8f;

    [Tooltip("Minimum number of lights that flicker at once.")]
    [SerializeField] private int minCount = 1;

    [Tooltip("Maximum number of lights that flicker at once.")]
    [SerializeField] private int maxCount = 3;

    [Tooltip("Only lights within this distance from the player can flicker.")]
    [SerializeField] private float maxDistance = 30f;

    private LightFlicker[] _allLights;
    private Transform _player;
    private float _timer;
    private float _nextTriggerAt;
    private float _maxDistanceSqr;
    private readonly List<LightFlicker> _nearbyBuffer = new ();
    private readonly List<LightFlicker> _activeFlickers = new ();

    private void Start()
    {
        _maxDistanceSqr = maxDistance * maxDistance;
        _allLights = FindObjectsByType<LightFlicker>(FindObjectsSortMode.None);
        _nextTriggerAt = Random.Range(minInterval, maxInterval);

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            _player = playerObj.transform;

        Debug.Log($"[LightFlickerManager] Found {_allLights.Length} flickerable lights.");
    }

    private void Update()
    {
        if (_allLights == null || _allLights.Length == 0 || _player == null) return;

        float dt = Time.deltaTime;

        // Tick all currently active flickers first, then remove finished ones (swap-remove for O(1)).
        for (int i = _activeFlickers.Count - 1; i >= 0; i--)
        {
            LightFlicker lf = _activeFlickers[i];
            if (lf == null)
            {
                int last = _activeFlickers.Count - 1;
                _activeFlickers[i] = _activeFlickers[last];
                _activeFlickers.RemoveAt(last);
                continue;
            }

            lf.TickFlicker(dt);

            if (!lf.IsFlickering)
            {
                int last = _activeFlickers.Count - 1;
                _activeFlickers[i] = _activeFlickers[last];
                _activeFlickers.RemoveAt(last);
            }
        }

        // Trigger new batches on interval
        _timer += dt;
        if (_timer >= _nextTriggerAt)
        {
            _timer = 0f;
            _nextTriggerAt = Random.Range(minInterval, maxInterval);
            TriggerNearbyBatch();
        }
    }

    /// <summary>Picks 1-3 random lights near the player and triggers a flicker.</summary>
    private void TriggerNearbyBatch()
    {
        _nearbyBuffer.Clear();
        Vector3 playerPos = _player.position;

        for (int i = 0; i < _allLights.Length; i++)
        {
            LightFlicker lf = _allLights[i];
            if (lf == null || lf.IsFlickering) continue;

            float sqrDist = (lf.transform.position - playerPos).sqrMagnitude;
            if (sqrDist <= _maxDistanceSqr)
                _nearbyBuffer.Add(lf);
        }

        if (_nearbyBuffer.Count == 0) return;

        int count = Random.Range(minCount, maxCount + 1);
        count = Mathf.Min(count, _nearbyBuffer.Count);

        // Fisher-Yates partial shuffle to pick random elements without repeats
        for (int i = 0; i < count; i++)
        {
            int j = Random.Range(i, _nearbyBuffer.Count);
            (_nearbyBuffer[i], _nearbyBuffer[j]) = (_nearbyBuffer[j], _nearbyBuffer[i]);
            _nearbyBuffer[i].TriggerFlicker();
            _activeFlickers.Add(_nearbyBuffer[i]);
        }
    }
}
