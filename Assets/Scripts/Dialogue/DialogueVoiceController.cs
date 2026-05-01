using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles character-specific audio playback during dialogue.
/// Attach to an NPC GameObject with an AudioSource.
/// Uses a dedicated AudioSource with PlayOneShot to avoid pitch/volume bleeding
/// between overlapping phoneme clips.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class DialogueVoiceController : MonoBehaviour
{
    private static readonly Dictionary<string, DialogueVoiceController> _registry = new();

    [Tooltip("The character's name as it appears in the DialogueData 'speaker' field.")]
    [SerializeField] private string characterName;

    [Tooltip("The character's voice profile containing phonemes and settings.")]
    [SerializeField] private VoiceProfile voiceProfile;

    private AudioSource _audioSource;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ClearRegistry()
    {
        // Ensures the static registry is clean after domain reload or entering Play mode.
        _registry.Clear();
    }

    private void Awake()
    {
        _audioSource = GetComponent<AudioSource>();
        _audioSource.playOnAwake = false;

        if (!string.IsNullOrEmpty(characterName))
        {
            _registry[characterName] = this;
        }
    }

    private void OnDestroy()
    {
        if (!string.IsNullOrEmpty(characterName) && _registry.TryGetValue(characterName, out var controller) && controller == this)
        {
            _registry.Remove(characterName);
        }
    }

    /// <summary>Finds a registered DialogueVoiceController by the character's name.</summary>
    public static DialogueVoiceController GetByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        return _registry.TryGetValue(name, out var controller) ? controller : null;
    }

    /// <summary>Plays a random mumble/phoneme sound from the voice profile with randomized pitch.
    /// Pitch is set before PlayOneShot; because phonemes are very short clips, overlap is minimal.</summary>
    public void PlayPhoneme()
    {
        if (voiceProfile == null || voiceProfile.phonemes == null || voiceProfile.phonemes.Length == 0) return;

        AudioClip clip = voiceProfile.phonemes[Random.Range(0, voiceProfile.phonemes.Length)];
        if (clip == null) return;

        _audioSource.pitch = Random.Range(voiceProfile.minPitch, voiceProfile.maxPitch);
        _audioSource.PlayOneShot(clip, voiceProfile.volume);
    }

    /// <summary>Returns the play frequency for phonemes (every N characters).</summary>
    public int GetPlayFrequency()
    {
        return voiceProfile != null ? voiceProfile.playEveryNCharacters : 2;
    }
}
