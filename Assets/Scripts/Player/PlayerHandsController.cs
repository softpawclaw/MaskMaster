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

        private enum HandViewState
        {
            Rest = 0,
            Inspect = 1
        }

        [Serializable]
        private class HandPoseData
        {
            [Header("Socket")]
            [SerializeField] private Transform handSocket;

            [Header("Rest Pose")]
            [SerializeField] private Vector3 restLocalPosition;
            [SerializeField] private Vector3 restLocalRotation;

            [Header("Inspect Pose")]
            [SerializeField] private Vector3 inspectLocalPosition;
            [SerializeField] private Vector3 inspectLocalRotation;

            public Transform HandSocket => handSocket;
            public Vector3 RestLocalPosition => restLocalPosition;
            public Vector3 RestLocalRotation => restLocalRotation;
            public Vector3 InspectLocalPosition => inspectLocalPosition;
            public Vector3 InspectLocalRotation => inspectLocalRotation;
        }

        [Header("Hands")]
        [SerializeField] private HandPoseData rightHandData;
        [SerializeField] private HandPoseData leftHandData;

        [Header("Input")]
        [SerializeField] private PlayerInput playerInput;
        [SerializeField] private string flipPageActionName = "FlipPage";           // E
        [SerializeField] private string selectPlaceActionName = "SelectPlace";     // Q
        [SerializeField] private string inspectLeftHandActionName = "InspectLeftHand";
        [SerializeField] private string inspectRightHandActionName = "InspectRightHand";

        [Header("View")]
        [SerializeField] private float handMoveSpeed = 10f;
        [SerializeField] private float handRotateSpeed = 10f;

        private ItemBase rightItem;
        private ItemBase leftItem;

        public ItemBase RightItem => rightItem;
        public ItemBase LeftItem => leftItem;

        private InputAction flipPageAction;
        private InputAction selectPlaceAction;
        private InputAction inspectLeftHandAction;
        private InputAction inspectRightHandAction;

        private HandViewState rightHandState = HandViewState.Rest;
        private HandViewState leftHandState = HandViewState.Rest;

        private void Awake()
        {
            SnapHandsToCurrentState();
        }

        private void OnEnable()
        {
            if (playerInput == null || playerInput.actions == null)
                return;

            flipPageAction = playerInput.actions[flipPageActionName];
            if (flipPageAction != null)
            {
                flipPageAction.performed += OnFlipPagePerformed;
                flipPageAction.Enable();
            }

            selectPlaceAction = playerInput.actions[selectPlaceActionName];
            if (selectPlaceAction != null)
            {
                selectPlaceAction.performed += OnSelectPlacePerformed;
                selectPlaceAction.Enable();
            }

            inspectLeftHandAction = playerInput.actions[inspectLeftHandActionName];
            if (inspectLeftHandAction != null)
            {
                inspectLeftHandAction.performed += OnInspectLeftHandPerformed;
                inspectLeftHandAction.Enable();
            }

            inspectRightHandAction = playerInput.actions[inspectRightHandActionName];
            if (inspectRightHandAction != null)
            {
                inspectRightHandAction.performed += OnInspectRightHandPerformed;
                inspectRightHandAction.Enable();
            }
        }

        private void OnDisable()
        {
            if (flipPageAction != null)
                flipPageAction.performed -= OnFlipPagePerformed;

            if (selectPlaceAction != null)
                selectPlaceAction.performed -= OnSelectPlacePerformed;

            if (inspectLeftHandAction != null)
                inspectLeftHandAction.performed -= OnInspectLeftHandPerformed;

            if (inspectRightHandAction != null)
                inspectRightHandAction.performed -= OnInspectRightHandPerformed;
        }

        private void Update()
        {
            UpdateHandPose(rightHandData, rightHandState);
            UpdateHandPose(leftHandData, leftHandState);
        }

        private void OnFlipPagePerformed(InputAction.CallbackContext context)
        {
            if (TryFlipPaperStackInHand(HandType.Right)) return;
            TryFlipPaperStackInHand(HandType.Left);
        }

        private void OnSelectPlacePerformed(InputAction.CallbackContext context)
        {
            if (TrySelectTraySlotInHand(HandType.Right)) return;
            TrySelectTraySlotInHand(HandType.Left);
        }

        private void OnInspectLeftHandPerformed(InputAction.CallbackContext context)
        {
            ToggleInspectForHand(HandType.Left);
        }

        private void OnInspectRightHandPerformed(InputAction.CallbackContext context)
        {
            ToggleInspectForHand(HandType.Right);
        }

        private void ToggleInspectForHand(HandType handType)
        {
            if (handType == HandType.Left)
            {
                if (leftHandState == HandViewState.Inspect)
                {
                    leftHandState = HandViewState.Rest;
                    rightHandState = HandViewState.Rest;
                }
                else
                {
                    leftHandState = HandViewState.Inspect;
                    rightHandState = HandViewState.Rest;
                }

                return;
            }

            if (rightHandState == HandViewState.Inspect)
            {
                rightHandState = HandViewState.Rest;
                leftHandState = HandViewState.Rest;
            }
            else
            {
                rightHandState = HandViewState.Inspect;
                leftHandState = HandViewState.Rest;
            }
        }

        private void UpdateHandPose(HandPoseData handData, HandViewState state)
        {
            if (handData == null || handData.HandSocket == null)
                return;

            Vector3 targetPosition;
            Quaternion targetRotation;

            if (state == HandViewState.Inspect)
            {
                targetPosition = handData.InspectLocalPosition;
                targetRotation = Quaternion.Euler(handData.InspectLocalRotation);
            }
            else
            {
                targetPosition = handData.RestLocalPosition;
                targetRotation = Quaternion.Euler(handData.RestLocalRotation);
            }

            handData.HandSocket.localPosition = Vector3.Lerp(
                handData.HandSocket.localPosition,
                targetPosition,
                Time.deltaTime * handMoveSpeed);

            handData.HandSocket.localRotation = Quaternion.Slerp(
                handData.HandSocket.localRotation,
                targetRotation,
                Time.deltaTime * handRotateSpeed);
        }

        private void SnapHandsToCurrentState()
        {
            SnapHandPose(rightHandData, rightHandState);
            SnapHandPose(leftHandData, leftHandState);
        }

        private void SnapHandPose(HandPoseData handData, HandViewState state)
        {
            if (handData == null || handData.HandSocket == null)
                return;

            if (state == HandViewState.Inspect)
            {
                handData.HandSocket.localPosition = handData.InspectLocalPosition;
                handData.HandSocket.localRotation = Quaternion.Euler(handData.InspectLocalRotation);
            }
            else
            {
                handData.HandSocket.localPosition = handData.RestLocalPosition;
                handData.HandSocket.localRotation = Quaternion.Euler(handData.RestLocalRotation);
            }
        }

        private bool TryFlipPaperStackInHand(HandType handType)
        {
            var item = GetItem(handType);
            if (item is not PaperStackItem stack) return false;

            stack.SelectNext();
            return true;
        }

        private bool TrySelectTraySlotInHand(HandType handType)
        {
            var item = GetItem(handType);
            if (item is not TrayItem tray) return false;

            tray.SelectNext();
            return true;
        }

        public TrayItem GetTrayInHands()
        {
            if (rightItem is TrayItem rightTray) return rightTray;
            if (leftItem is TrayItem leftTray) return leftTray;

            return null;
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

            if (TryGiveItemToContainerInHand(item, preferredHand))
            {
                OnItemTaken?.Invoke();
                return true;
            }

            if (TryGiveItemToContainerInHand(item, fallbackHand))
            {
                OnItemTaken?.Invoke();
                return true;
            }

            if (TryGiveItemToHand(item, preferredHand))
            {
                OnItemTaken?.Invoke();
                return true;
            }

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

            var socket = handType == HandType.Right
                ? rightHandData != null ? rightHandData.HandSocket : null
                : leftHandData != null ? leftHandData.HandSocket : null;

            if (socket == null) return false;

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
                rightItem = item;
            else
                leftItem = item;
        }
    }
}