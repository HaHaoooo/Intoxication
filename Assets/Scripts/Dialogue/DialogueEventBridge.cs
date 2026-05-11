using UnityEngine;
using UnityEngine.Events;

public class DialogueEventBridge : MonoBehaviour
{
    public UnityEvent onDialogueEnded = new UnityEvent();

    private DialogueUI _ui;

    private void Start()
    {
        _ui = DialogueUI.Instance;
    }

    // This should be called by the interaction script when dialogue starts
    public void RegisterOnce()
    {
        if (_ui == null) _ui = DialogueUI.Instance;
        if (_ui != null)
        {
            _ui.OnDialogueEnded += HandleDialogueEnded;
        }
    }

    private void HandleDialogueEnded()
    {
        if (_ui != null) _ui.OnDialogueEnded -= HandleDialogueEnded;
        onDialogueEnded?.Invoke();
    }
}
