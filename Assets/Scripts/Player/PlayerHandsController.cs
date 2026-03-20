using System;
using Enums;
using Items;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Player
{
    public class PlayerHandsController : MonoBehaviour
    {
        public event Action OnItemTaken;
        
        [Header("Sockets")]
        [SerializeField] private Transform rightHand;
        [SerializeField] private Transform leftHand;

        [Header("Input")]
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private string flipPageActionName = "FlipPage";

        private ItemBase rightItem;
        private ItemBase leftItem;

        public ItemBase RightItem => rightItem;
        public ItemBase LeftItem => leftItem;

        private InputAction flipPageAction;

        private void OnEnable()
        {
            if (playerInput != null && playerInput.actions != null)
            {
                flipPageAction = playerInput.actions[flipPageActionName];

                if (flipPageAction != null)
                {
                    flipPageAction.performed += OnFlipPagePerformed;
                    flipPageAction.Enable();
                }
            }
        }

        private void OnDisable()
        {
            if (flipPageAction != null)
            {
                flipPageAction.performed -= OnFlipPagePerformed;
            }
        }

        private void OnFlipPagePerformed(InputAction.CallbackContext context)
        {
            if (TryFlipContainerInHand(HandType.Right)) return;
            TryFlipContainerInHand(HandType.Left);
        }

        private bool TryFlipContainerInHand(HandType handType)
        {
            var item = GetItem(handType);
            if (item is not ContainerItemBase container) return false;

            container.SelectNext();
            return true;
        }

        public ItemBase GetItem(HandType handType)
        {
            return handType == HandType.Right ? rightItem : leftItem;
        }

        public bool IsHandFree(HandType handType)
        {
            return GetItem(handType) == null;
        }

        public bool GiveItem(ItemBase item)
        {
            if (item == null) return false;

            var preferredHand = item.PreferredHand;
            var fallbackHand = preferredHand == HandType.Right ? HandType.Left : HandType.Right;

            // 1. Сначала пытаемся доложить в контейнер в предпочитаемой руке
            if (TryGiveItemToContainerInHand(item, preferredHand))
            {
                OnItemTaken?.Invoke();
                return true;
            }

            // 2. Потом в контейнер во второй руке
            if (TryGiveItemToContainerInHand(item, fallbackHand))
            {
                OnItemTaken?.Invoke();
                return true;
            }

            // 3. Если не получилось — пробуем как обычный предмет в предпочитаемую руку
            if (TryGiveItemToHand(item, preferredHand))
            {
                OnItemTaken?.Invoke();
                return true;
            }

            // 4. И только потом во вторую руку
            if (TryGiveItemToHand(item, fallbackHand))
            {
                OnItemTaken?.Invoke();
                return true;
            }

            return false;
        }

        private bool TryGiveItemToContainerInHand(ItemBase item, HandType handType)
        {
            if (item == null) return false;

            var handItem = GetItem(handType);
            if (handItem is not ContainerItemBase container) return false;

            if (!container.CanAccept(item)) return false;

            return container.TryAdd(item);
        }

        public bool TryGiveItemToHand(ItemBase item, HandType handType)
        {
            if (item == null) return false;
            if (!IsHandFree(handType)) return false;

            SetItem(handType, item);

            var socket = handType == HandType.Right ? rightHand : leftHand;
            item.OnTakenToHand(socket);

            return true;
        }

        public void FreeItem(ItemBase item)
        {
            if (item == null) return;

            if (rightItem == item)
            {
                rightItem.OnRemovedFromHand();
                rightItem = null;
                return;
            }

            if (leftItem == item)
            {
                leftItem.OnRemovedFromHand();
                leftItem = null;
            }
        }

        public ItemBase ChooseItem(PlacementType placementType, ItemSize size)
        {
            var candidate = ChooseItemFromHand(HandType.Right, placementType, size);
            if (candidate != null) return candidate;

            return ChooseItemFromHand(HandType.Left, placementType, size);
        }

        public ItemBase ChooseItemFromHand(HandType handType, PlacementType placementType, ItemSize size)
        {
            var item = GetItem(handType);
            if (item == null) return null;

            if (item.PlacementType != placementType) return null;
            if (item.Size > size) return null;

            return item;
        }

        private void SetItem(HandType handType, ItemBase item)
        {
            if (handType == HandType.Right)
            {
                rightItem = item;
            }
            else
            {
                leftItem = item;
            }
        }
    }
}