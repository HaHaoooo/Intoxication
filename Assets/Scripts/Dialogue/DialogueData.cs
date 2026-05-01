using System;
using UnityEngine;

/// <summary>
/// ScriptableObject holding a sequence of dialogue lines.
/// Create via Assets > Create > Dialogue > Dialogue Data.
/// </summary>
[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Serializable]
    public struct Line
    {
        /// <summary>Name displayed above the dialogue text (e.g. "酒保").</summary>
        public string speaker;

        /// <summary>The dialogue text content.</summary>
        [TextArea(2, 5)]
        public string text;
    }

    [Tooltip("Ordered list of dialogue lines to display.")]
    public Line[] lines;
}
