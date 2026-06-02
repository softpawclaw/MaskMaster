using System.Collections.Generic;
using UnityEngine;

namespace Systems
{
    public class DialogueHistorySystem : MonoBehaviour
    {
        private readonly HashSet<string> shownDialogueIds = new HashSet<string>();

        public void MarkShown(string dialogueId)
        {
            if (string.IsNullOrWhiteSpace(dialogueId)) return;
            shownDialogueIds.Add(dialogueId);
        }

        public bool WasShown(string dialogueId)
        {
            return !string.IsNullOrWhiteSpace(dialogueId) && shownDialogueIds.Contains(dialogueId);
        }

        public bool WereAllShown(string[] dialogueIds)
        {
            if (dialogueIds == null || dialogueIds.Length == 0) return true;

            for (int i = 0; i < dialogueIds.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(dialogueIds[i])) continue;
                if (!WasShown(dialogueIds[i])) return false;
            }

            return true;
        }

        public bool WasAnyShown(string[] dialogueIds)
        {
            if (dialogueIds == null || dialogueIds.Length == 0) return false;

            for (int i = 0; i < dialogueIds.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(dialogueIds[i])) continue;
                if (WasShown(dialogueIds[i])) return true;
            }

            return false;
        }

        public void Clear()
        {
            shownDialogueIds.Clear();
        }
    }
}
