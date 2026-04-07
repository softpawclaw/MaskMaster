using System;
using Items;
using Player;
using UnityEngine;

namespace Interactable.Table
{
    public class MaskOutputInteractable : Interactable
    {
        [SerializeField] private Transform outputSocket;

        private ItemBase currentItem;

        public event Action OnItemChanged;

        public bool HasItem => currentItem != null;
        public ItemBase CurrentItem => currentItem;

        public void SetItem(ItemBase item)
        {
            if (item == null) return;

            currentItem = item;

            var socket = outputSocket != null ? outputSocket : transform;
            currentItem.transform.SetParent(socket);
            currentItem.transform.SetPositionAndRotation(socket.position, socket.rotation);
            currentItem.gameObject.SetActive(true);

            OnItemChanged?.Invoke();
        }

        protected override void OnInteract(GameObject interactor)
        {
            if (currentItem == null)
            {
                CompleteInteraction(interactor);
                return;
            }

            var hands = interactor.GetComponent<PlayerHandsController>();
            if (hands == null)
            {
                CompleteInteraction(interactor);
                return;
            }

            if (!hands.GiveItem(currentItem))
            {
                Debug.LogWarning("MaskOutputInteractable: failed to give mask to hands.");
                CompleteInteraction(interactor);
                return;
            }

            currentItem = null;
            OnItemChanged?.Invoke();
            CompleteInteraction(interactor);
        }
    }
}