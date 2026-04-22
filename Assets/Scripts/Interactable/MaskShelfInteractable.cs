using System;
using System.Collections.Generic;
using Enums;
using Global;
using Items;
using Player;
using Systems;
using UnityEngine;

namespace Interactable
{
    public class MaskShelfInteractable : Interactable
    {
        [Header("Shelf Sockets")]
        [SerializeField] private List<Transform> maskSockets = new();

        private readonly List<MaskItem> storedMasks = new();

        private QuestSystem questSystem;
        private DelayedDialogSystem delayedDialogSystem;

        public event Action<MaskItem> OnMaskStored;
        public event Action<MaskItem> OnMaskTaken;

        public int Capacity => maskSockets != null ? maskSockets.Count : 0;
        public int StoredCount => storedMasks.Count;

        public void Link()
        {
            questSystem = Linker.Instance.QuestSystem;
            delayedDialogSystem = Linker.Instance.DelayedDialogSystem;

            ValidateConfig();
        }

        protected override void OnInteract(GameObject interactor)
        {
            var hands = interactor.GetComponent<PlayerHandsController>();
            if (hands == null)
            {
                Debug.LogWarning("MaskShelfInteractable: PlayerHandsController not found.");
                CompleteInteraction(interactor);
                return;
            }

            if (maskSockets == null || maskSockets.Count == 0)
            {
                Debug.LogError("MaskShelfInteractable: no mask sockets configured.");
                CompleteInteraction(interactor);
                return;
            }

            var maskInHands = TryGetMaskFromHands(hands);

            // 1. Если маска в руках есть — пробуем поставить
            if (maskInHands != null)
            {
                TryStoreMask(interactor, hands, maskInHands);
                return;
            }

            // 2. Если маски в руках нет — пробуем снять с полки
            TryTakeMask(interactor, hands);
        }

        private void TryStoreMask(GameObject interactor, PlayerHandsController hands, MaskItem mask)
        {
            if (storedMasks.Count >= maskSockets.Count)
            {
                Debug.Log("MaskShelfInteractable: shelf is full.");
                CompleteInteraction(interactor);
                return;
            }

            var questWasAwait = questSystem != null && questSystem.CurrentState == QuestState.Await;

            hands.FreeItem(mask);
            PlaceMask(mask);

            // Продвигаем квест только один раз: Await -> Request
            if (questWasAwait)
            {
                questSystem.ChangeQuestState();
                delayedDialogSystem?.ScheduleBell();
            }

            OnMaskStored?.Invoke(mask);
            CompleteInteraction(interactor);
        }

        private void TryTakeMask(GameObject interactor, PlayerHandsController hands)
        {
            if (storedMasks.Count == 0)
            {
                Debug.Log("MaskShelfInteractable: shelf is empty.");
                CompleteInteraction(interactor);
                return;
            }

            var lastIndex = storedMasks.Count - 1;
            var mask = storedMasks[lastIndex];

            if (mask == null)
            {
                storedMasks.RemoveAt(lastIndex);
                Debug.LogWarning("MaskShelfInteractable: removed null mask entry from shelf.");
                CompleteInteraction(interactor);
                return;
            }

            if (!hands.GiveItem(mask))
            {
                Debug.Log("MaskShelfInteractable: could not give mask to player hands.");
                CompleteInteraction(interactor);
                return;
            }

            storedMasks.RemoveAt(lastIndex);

            OnMaskTaken?.Invoke(mask);
            Debug.Log($"MaskShelfInteractable: took mask {mask.ItemId}. {storedMasks.Count}/{maskSockets.Count}");

            CompleteInteraction(interactor);
        }

        private void PlaceMask(MaskItem mask)
        {
            var socketIndex = storedMasks.Count;
            var socket = maskSockets[socketIndex] != null ? maskSockets[socketIndex] : transform;

            mask.transform.SetParent(socket);
            mask.transform.SetPositionAndRotation(socket.position, socket.rotation);
            mask.gameObject.SetActive(true);
            mask.SetWorldRenderLayer();

            storedMasks.Add(mask);

            Debug.Log($"MaskShelfInteractable: stored mask {mask.ItemId}. {storedMasks.Count}/{maskSockets.Count}");
        }

        private MaskItem TryGetMaskFromHands(PlayerHandsController hands)
        {
            var right = hands.GetItem(HandType.Right);
            if (right is MaskItem rightMask)
                return rightMask;

            var left = hands.GetItem(HandType.Left);
            if (left is MaskItem leftMask)
                return leftMask;

            return null;
        }

        private void ValidateConfig()
        {
            if (maskSockets == null || maskSockets.Count == 0)
            {
                Debug.LogError("MaskShelfInteractable: shelf must contain at least one socket.");
                return;
            }

            for (int i = maskSockets.Count - 1; i >= 0; i--)
            {
                if (maskSockets[i] == null)
                {
                    Debug.LogWarning($"MaskShelfInteractable: socket at index {i} is null and will be ignored.");
                    maskSockets.RemoveAt(i);
                }
            }

            if (maskSockets.Count == 0)
            {
                Debug.LogError("MaskShelfInteractable: all configured sockets are null.");
            }
        }
    }
}