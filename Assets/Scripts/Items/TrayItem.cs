using System;
using System.Collections.Generic;
using UnityEngine;

namespace Items
{
    public class TrayItem : ContainerItemBase
    {
        private const int SlotCount = 5;

        [Header("Tray")]
        [SerializeField] private int selectedIndex = 0;

        [Header("Hand View")]
        [SerializeField] private List<Transform> itemSockets = new();
        [SerializeField] private List<GameObject> selectionVisuals = new();

        private readonly ResourceItem[] slotItems = new ResourceItem[SlotCount];

        private bool takenToHand = false;
        
        public int SelectedIndex => selectedIndex;

        public override bool CanAccept(ItemBase item)
        {
            if (item == null) return false;
            if (item == this) return false;
            if (item is not ResourceItem) return false;

            return HasFreeSlot();
        }

        public override void OnTakenToHand(Transform handSocket)
        {
            base.OnTakenToHand(handSocket);
            selectedIndex = 0;
            RefreshHandView();
        }

        public override void OnRemovedFromHand()
        {
            base.OnRemovedFromHand();
            selectedIndex = -1;
            RefreshSelectionView();
        }

        public override ItemBase GetSelectedItem()
        {
            if (!IsValidIndex(selectedIndex)) return null;
            return slotItems[selectedIndex];
        }

        public override void SelectNext()
        {
            selectedIndex++;
            if (selectedIndex >= SlotCount)
                selectedIndex = 0;

            RefreshHandView();
        }

        public override void SelectPrevious()
        {
            selectedIndex--;
            if (selectedIndex < 0)
                selectedIndex = SlotCount - 1;

            RefreshHandView();
        }

        public override bool TryAdd(ItemBase item)
        {
            if (item is not ResourceItem resource) return false;
            if (!CanAccept(item)) return false;
            if (Contains(resource)) return false;

            int freeIndex = FindFirstFreeSlot();
            if (freeIndex < 0) return false;

            slotItems[freeIndex] = resource;
            SyncItemsListFromSlots();
            RefreshHandView();
            return true;
        }

        public override bool TryRemove(ItemBase item)
        {
            if (item is not ResourceItem resource) return false;

            for (int i = 0; i < SlotCount; i++)
            {
                if (slotItems[i] != resource) continue;

                slotItems[i] = null;
                SyncItemsListFromSlots();
                RefreshHandView();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Новая универсальная логика:
        /// выбранный слот рассматривается как точка обмена.
        ///
        /// incomingResource:
        /// - ресурс, приходящий извне (например, с полки);
        /// - может быть null.
        ///
        /// outgoingResource:
        /// - то, что раньше лежало в выбранном слоте;
        /// - может быть null.
        ///
        /// Возвращает false только если индекс невалиден
        /// или если и incoming, и текущее содержимое слота == null,
        /// то есть реально менять нечего.
        /// </summary>
        public bool TryExchangeSelectedResource(ResourceItem incomingResource, out ResourceItem outgoingResource)
        {
            outgoingResource = null;

            if (!IsValidIndex(selectedIndex)) return false;

            var currentSelected = slotItems[selectedIndex];

            if (currentSelected == null && incomingResource == null)
                return false;

            outgoingResource = currentSelected;
            slotItems[selectedIndex] = incomingResource;

            SyncItemsListFromSlots();
            RefreshHandView();
            slotItems[selectedIndex].OnTakenToHand(slotItems[selectedIndex].transform);
            return true;
        }

        public override List<ItemBase> ExtractAllItems()
        {
            var extracted = new List<ItemBase>();

            for (int i = 0; i < SlotCount; i++)
            {
                if (slotItems[i] == null) continue;

                extracted.Add(slotItems[i]);
                slotItems[i] = null;
            }

            SyncItemsListFromSlots();
            RefreshHandView();

            return extracted;
        }

        public override void LoadItems(IEnumerable<ItemBase> source)
        {
            ClearSlots();

            foreach (var item in source)
            {
                if (item is not ResourceItem resource) continue;

                int freeIndex = FindFirstFreeSlot();
                if (freeIndex < 0) break;

                slotItems[freeIndex] = resource;
            }

            SyncItemsListFromSlots();
            RefreshHandView();
        }

        private void Start()
        {
            OnRemovedFromHand();
        }

        protected override void OnContainerChanged()
        {
            SyncSlotsFromItemsList();
            RefreshHandView();
        }

        private void RefreshHandView()
        {
            RefreshItemsView();
            RefreshSelectionView();
        }

        private void RefreshItemsView()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                var item = slotItems[i];
                if (item == null) continue;

                var socket = GetSocket(i);
                item.gameObject.SetActive(true);

                if (socket != null)
                {
                    item.transform.SetParent(socket);
                    item.transform.localPosition = Vector3.zero;
                    item.transform.localRotation = Quaternion.identity;
                }
                else
                {
                    item.transform.SetParent(transform);
                    item.transform.localPosition = Vector3.zero;
                    item.transform.localRotation = Quaternion.identity;
                }
            }
        }

        private void RefreshSelectionView()
        {
            if (selectionVisuals == null || selectionVisuals.Count == 0) return;

            for (int i = 0; i < selectionVisuals.Count; i++)
            {
                if (selectionVisuals[i] == null) continue;
                selectionVisuals[i].SetActive(i == selectedIndex);
            }
        }

        private Transform GetSocket(int index)
        {
            if (itemSockets == null) return null;
            if (index < 0 || index >= itemSockets.Count) return null;

            return itemSockets[index];
        }

        private bool HasFreeSlot()
        {
            return FindFirstFreeSlot() >= 0;
        }

        private int FindFirstFreeSlot()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (slotItems[i] == null)
                    return i;
            }

            return -1;
        }

        private bool Contains(ResourceItem item)
        {
            if (item == null) return false;

            for (int i = 0; i < SlotCount; i++)
            {
                if (slotItems[i] == item)
                    return true;
            }

            return false;
        }

        private bool IsValidIndex(int index)
        {
            return index >= 0 && index < SlotCount;
        }

        private void ClearSlots()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                slotItems[i] = null;
            }
        }

        private void SyncItemsListFromSlots()
        {
            items.Clear();

            for (int i = 0; i < SlotCount; i++)
            {
                if (slotItems[i] != null)
                    items.Add(slotItems[i]);
            }
        }

        private void SyncSlotsFromItemsList()
        {
            ClearSlots();

            int slotIndex = 0;
            for (int i = 0; i < items.Count; i++)
            {
                if (slotIndex >= SlotCount) break;
                if (items[i] is not ResourceItem resource) continue;

                slotItems[slotIndex] = resource;
                slotIndex++;
            }
        }
    }
}