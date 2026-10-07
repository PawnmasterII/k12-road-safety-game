using UnityEngine;

namespace SafeStreets
{
    /// <summary>
    /// Borrowed from 2DRPGKit's Dialog.cs (CC0): conversation data as a ScriptableObject.
    /// Simplified: no portraits/inn/shop — just a name + lines + optional closing tip.
    /// </summary>
    [CreateAssetMenu(fileName = "NewConversation", menuName = "SafeStreets/Conversation")]
    public class Conversation : ScriptableObject
    {
        [Tooltip("Name shown in the dialog name plate (e.g. 'Officer Ada')")]
        public string speakerName;

        [Tooltip("Lines spoken, in order. Prefix a line with // to change the name plate mid-dialog.")]
        public string[] lines;

        [Tooltip("Optional closing safety tip shown after the last line")]
        public string closingTip;
    }
}
