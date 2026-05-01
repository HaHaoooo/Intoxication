using UnityEngine;

/// <summary>
/// Defines a collection of audio clips and settings used to simulate a character's voice.
/// </summary>
[CreateAssetMenu(fileName = "NewVoiceProfile", menuName = "Dialogue/Voice Profile")]
public class VoiceProfile : ScriptableObject
{
    [Tooltip("The set of short audio clips (phonemes/mumbles) used for this voice.")]
    public AudioClip[] phonemes;

    [Header("Playback Settings")]
    [Range(0.1f, 3.0f)] public float minPitch = 0.9f;
    [Range(0.1f, 3.0f)] public float maxPitch = 1.1f;
    [Range(0f, 1f)] public float volume = 1.0f;

    [Tooltip("Play a sound every X characters in the dialogue text.")]
    [Range(1, 10)] public int playEveryNCharacters = 2;
}
