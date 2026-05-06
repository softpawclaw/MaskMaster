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

        [Tooltip("Mouse wheel action. Expected value type: Vector2 or Axis/float.")]
        [SerializeField] private string scrollActiveHandActionName = "Scroll";

        [Tooltip("Off by default so existing prefab values for Q/E do not keep stealing gameplay input.")]
        [SerializeField] private bool enableLegacyHandScrollActions = false;

        [Tooltip("Legacy fallback. Used only when Enable Legacy Hand Scroll Actions is true.")]
        [SerializeField] private string flipPageActionName = string.Empty;

        [Tooltip("Legacy fallback. Used only when Enable Legacy Hand Scroll Actions is true.")]
        [SerializeField] private string selectPlaceActionName = string.Empty;

        [SerializeField] private string inspectLeftHandActionName = "InspectLeftHand";
        [SerializeField] private string inspectRightHandActionName = "InspectRightHand";

        [Header("View")]
        [SerializeField] private float handMoveSpeed = 10f;
        [SerializeField] private float handRotateSpeed = 10f;

        private ItemBase rightItem;
        private ItemBase leftItem;

        public ItemBase RightItem => rightItem;
        public ItemBase LeftItem => leftItem;

        private InputAction scrollActiveHandAction;
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

            scrollActiveHandAction = FindAction(scrollActiveHandActionName);
            if (scrollActiveHandAction != null)
            {
                scrollActiveHandAction.performed += OnScrollActiveHandPerformed;
                scrollActiveHandAction.Enable();
            }

            // Старые бинды оставлены как опциональный fallback, но выключены по умолчанию.
            // Это важно: на уже существующих префабах Unity сохранит старые строки FlipPage/SelectPlace.
            if (enableLegacyHandScrollActions)
            {
                flipPageAction = FindAction(flipPageActionName);
                if (flipPageAction != null)
                {
                    flipPageAction.performed += OnFlipPagePerformed;
                    flipPageAction.Enable();
                }

                selectPlaceAction = FindAction(selectPlaceActionName);
                if (selectPlaceAction != null)
                {
                    selectPlaceAction.performed += OnSelectPlacePerformed;
                    selectPlaceAction.Enable();
                }
            }

            inspectLeftHandAction = FindAction(inspectLeftHandActionName);
            if (inspectLeftHandAction != null)
            {
                inspectLeftHandAction.performed += OnInspectLeftHandPerformed;
                inspectLeftHandAction.Enable();
            }

            inspectRightHandAction = FindAction(inspectRightHandActionName);
            if (inspectRightHandAction != null)
            {
                inspectRightHandAction.performed += OnInspectRightHandPerformed;
                inspectRightHandAction.Enable();
            }
        }

        private void OnDisable()
        {
            if (scrollActiveHandAction != null)
                scrollActiveHandAction.performed -= OnScrollActiveHandPerformed;

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

        private void OnScrollActiveHandPerformed(InputAction.CallbackContext context)
        {
            float scrollValue = ReadScrollValue(context);

            if (Mathf.Approximately(scrollValue, 0f))
                return;

            TryScrollActiveHand(scrollValue > 0f);
        }

        private void OnFlipPagePerformed(InputAction.CallbackContext context)
        {
            TryScrollActiveHand(true);
        }

        private void OnSelectPlacePerformed(InputAction.CallbackContext context)
        {
            TryScrollActiveHand(true);
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

        private bool TryScrollActiveHand(bool forward)
        {
            if (!TryGetActiveHand(out HandType activeHand))
                return false;

            return TryScrollContainerInHand(activeHand, forward);
        }

        private bool TryScrollContainerInHand(HandType handType, bool forward)
        {
            var item = GetItem(handType);
            if (item is not ContainerItemBase container) return false;

            if (forward)
                container.SelectNext();
            else
                container.SelectPrevious();

            return true;
        }

        private bool TryGetActiveHand(out HandType handType)
        {
            if (rightHandState == HandViewState.Inspect)
            {
                handType = HandType.Right;
                return true;
            }

            if (leftHandState == HandViewState.Inspect)
            {
                handType = HandType.Left;
                return true;
            }

            handType = default;
            return false;
        }

        private float ReadScrollValue(InputAction.CallbackContext context)
        {
            try
            {
                return context.ReadValue<Vector2>().y;
            }
            catch (InvalidOperationException)
            {
                return context.ReadValue<float>();
            }
        }

        private InputAction FindAction(string actionName)
        {
            if (string.IsNullOrWhiteSpace(actionName))
                return null;

            return playerInput.actions.FindAction(actionName, false);
        }

        public TrayItem GetTrayInHands()
        {
            if (rightItem is TrayItem rightTray) return rightTray;
            if (leftItem is TrayItem leftTray) return leftTray;

            return null;
        }


        public T GetFirstItemInHands<T>() where T : ItemBase
        {
            if (rightItem is T rightTyped) return rightTyped;
            if (leftItem is T leftTyped) return leftTyped;

            return null;
        }

        public bool TryTakeFirstItemFromHands<T>(out T item) where T : ItemBase
        {
            item = GetFirstItemInHands<T>();
            if (item == null) return false;

            FreeItem(item);
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