using Enums;
using Player;
using UnityEngine;

namespace Items
{
    public class ItemPlaceHolder : Interactable.Interactable
    {
        [Header("Container Socket")]
        [SerializeField] protected Transform containerSocket;
        
        [SerializeField] protected PlacementType placementType;
        [SerializeField] protected ItemSize size;

        protected ItemBase currentItem;

        public ItemBase CurrentItem => currentItem;

        protected override void OnInteract(GameObject interactor)
        {
            var hands = interactor.GetComponent<PlayerHandsController>();
            if (!hands) return;

            if (currentItem == null)
            {
                PlaceItem(hands);
            }
            else
            {
                ReplaceItem(hands);
            }

            CompleteInteraction(interactor);
        }

        protected virtual void PlaceItem(PlayerHandsController hands)
        {
            var inItem = hands.ChooseItem(placementType, size);

            if (inItem == null)
            {
                Debug.Log("Refused to place empty item");
                return;
            }

            if (!CanAcceptItem(inItem))
            {
                Debug.Log($"Refused to place item {inItem.ItemId}");
                return;
            }

            hands.FreeItem(inItem);
            AttachItem(inItem);
        }

        protected virtual void ReplaceItem(PlayerHandsController hands)
        {
            var inItem = hands.ChooseItem(placementType, size);

            if (inItem != null && !CanAcceptItem(inItem))
            {
                Debug.Log($"Refused to replace with item {inItem.ItemId}");
                return;
            }

            if (inItem != null)
            {
                hands.FreeItem(inItem);
            }

            var oldItem = currentItem;
            currentItem = null;

            if (oldItem != null)
            {
                var success = hands.GiveItem(oldItem);
                if (!success)
                {
                    Debug.LogWarning("Failed to return current item to player hands");
                    AttachItem(oldItem);
                    return;
                }
            }

            if (inItem != null)
            {
                AttachItem(inItem);
            }
        }

        protected virtual bool CanAcceptItem(ItemBase item)
        {
            if (item == null) return false;
            if (item.PlacementType != placementType) return false;
            if (item.Size > size) return false;

            return true;
        }

        protected virtual void AttachItem(ItemBase item)
        {
            currentItem = item;

            var socket = containerSocket != null ? containerSocket : transform;
            item.transform.SetParent(socket);
            item.transform.SetPositionAndRotation(socket.position, socket.rotation);
        }
    }
}