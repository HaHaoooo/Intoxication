using UnityEngine;
using UnityEngine.Events;

public class SceneEventTrigger : MonoBehaviour
{
    [SerializeField] private bool triggerOnce = true;
    [SerializeField] private string playerTag = "Player";
    public UnityEvent onTriggerEnter = new UnityEvent();

    private bool _hasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (_hasTriggered && triggerOnce) return;

        if (other.CompareTag(playerTag))
        {
            _hasTriggered = true;
            onTriggerEnter?.Invoke();
        }
    }
}
