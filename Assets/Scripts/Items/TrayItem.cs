using System;
using System.Collections.Generic;
using Enums;
using Helpers;
using UnityEngine;

namespace Items
{
    public class TrayItem : ContainerItemBase
    {
        private const int SlotCount = 5;

        [Header("Tray")]
        [SerializeField] private int selectedIndex = 0;

        [Header("Warehouse Auto Mode")]
        [SerializeField] private bool useWarehouseAutoMode = false;
        [SerializeField] private int blankSlotIndex = 0;

        [Header("Hand View")]
        [SerializeField] private List<Transform> itemSockets = new();
        [SerializeField] private List<GameObject> selectionVisuals = new();

        private readonly ResourceItem[] slotItems = new ResourceItem[SlotCount];

        private bool takenToHand = false;
        
        public int SelectedIndex => selectedIndex;
        public bool UseWarehouseAutoMode => useWarehouseAutoMode;

        public override bool CanAccept(ItemBase item)
        {
            if (item == null) return false;
            if (item == this) return false;
            if (item is not ResourceItem) return false;

            return HasFreeSlot();
        }

        public override void OnTakenToHand(Transform handSocket)
        {
            takenToHand = true;
            base.OnTakenToHand(handSocket);
            selectedIndex = 0;
            RefreshHandView();
        }

        public override void OnRemovedFromHand()
        {
            takenToHand = false;
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


        /// <summary>
        /// Альтернативная складская логика без ручного выбора слота:
        /// - повторный клик по ящику того же ResourceType возвращает ресурс обратно и удаляет его с подноса;
        /// - blank занимает только blankSlotIndex и не заменяется автоматически другим blank;
        /// - inlay занимает любой свободный слот, кроме blankSlotIndex;
        /// - старый ручной TryExchangeSelectedResource не трогаем.
        /// </summary>
        public bool TryToggleWarehouseResource(ResourceType type, Func<ResourceType, ResourceItem> createResource, out string message)
        {
            message = string.Empty;

            if (!useWarehouseAutoMode)
            {
                message = "TrayItem: warehouse auto mode is disabled.";
                return false;
            }

            if (type == ResourceType.None)
            {
                message = "TrayItem: refused to toggle ResourceType.None.";
                return false;
            }

            int existingIndex = FindSlotByResourceType(type);
            if (existingIndex >= 0)
            {
                var removed = slotItems[existingIndex];
                slotItems[existingIndex] = null;
                SyncItemsListFromSlots();
                RefreshHandView();

                if (removed != null)
                    Destroy(removed.gameObject);

                return true;
            }

            int targetSlot = FindAutoTargetSlot(type);
            if (targetSlot < 0)
            {
                message = ResourceTypeHelper.IsBlank(type)
                    ? $"TrayItem: blank slot is occupied. Return current blank before taking '{type}'."
                    : $"TrayItem: no free inlay slot for '{type}'. Return one inlay before taking a new one.";
                return false;
            }

            if (createResource == null)
            {
                message = $"TrayItem: createResource callback is null for '{type}'.";
                return false;
            }

            var resource = createResource.Invoke(type);
            if (resource == null)
            {
                message = $"TrayItem: failed to create resource '{type}'.";
                return false;
            }

            if (resource.Type != type)
                Debug.LogWarning($"TrayItem: created resource type mismatch. Expected '{type}', got '{resource.Type}'.");

            slotItems[targetSlot] = resource;
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

            if (slotItems[selectedIndex] != null)
            {
                slotItems[selectedIndex].OnTakenToHand(slotItems[selectedIndex].transform);
            }

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


        public bool IsValidStorageExitTray(MainRecipeItem recipe, out string message)
        {
            message = string.Empty;

            if (recipe == null)
            {
                message = "Tray validation failed: no main recipe.";
                return false;
            }

            int blankCount = 0;
            int inlayCount = 0;

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] is not ResourceItem resource)
                    continue;

                if (ResourceTypeHelper.IsBlank(resource.Type))
                    blankCount++;
                else if (ResourceTypeHelper.IsInlay(resource.Type))
                    inlayCount++;
            }

            int requiredInlayCount = GetRequiredInlayCount(recipe);

            if (blankCount != 1)
            {
                message = $"Tray validation failed: expected exactly one blank, actual={blankCount}.";
                return false;
            }

            if (inlayCount < requiredInlayCount)
            {
                message = $"Tray validation failed: not enough inlays. required={requiredInlayCount}, actual={inlayCount}.";
                return false;
            }

            message = $"Tray validation passed: blank={blankCount}, inlays={inlayCount}/{requiredInlayCount}.";
            return true;
        }

        private static int GetRequiredInlayCount(MainRecipeItem recipe)
        {
            if (recipe == null)
                return 0;

            var inlays = recipe.GetExpectedInlays();
            if (inlays == null)
                return 0;

            int count = 0;
            for (int i = 0; i < inlays.Length; i++)
            {
                if (inlays[i].ResourceType != ResourceType.None)
                    count++;
            }

            return count;
        }

        private void Start()
        {
            // Auto-created trays can be given to the player before Unity calls Start().
            // In that case calling OnRemovedFromHand() here would undo OnTakenToHand():
            // the tray and all resources inside it would be moved back to the world layer
            // and start clipping through walls.
            if (!takenToHand)
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

                // Важный момент для ресурсов, созданных складом напрямую:
                // они не проходят через обычный hand/container flow, поэтому должны
                // получить тот же render layer, что и сам поднос. Иначе предмет
                // физически лежит в слоте, но не виден hand camera.
                item.SetRenderLayerRecursive(gameObject.layer);
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


        private int FindSlotByResourceType(ResourceType type)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (slotItems[i] != null && slotItems[i].Type == type)
                    return i;
            }

            return -1;
        }

        private int FindAutoTargetSlot(ResourceType type)
        {
            if (ResourceTypeHelper.IsBlank(type))
            {
                if (!IsValidIndex(blankSlotIndex))
                {
                    Debug.LogError($"TrayItem: blankSlotIndex '{blankSlotIndex}' is invalid. Expected 0..{SlotCount - 1}.");
                    return -1;
                }

                return slotItems[blankSlotIndex] == null ? blankSlotIndex : -1;
            }

            if (!ResourceTypeHelper.IsInlay(type))
            {
                Debug.LogWarning($"TrayItem: resource '{type}' is neither blank nor inlay by ResourceTypeHelper rules. Treating it as inlay.");
            }

            for (int i = 0; i < SlotCount; i++)
            {
                if (i == blankSlotIndex)
                    continue;

                if (slotItems[i] == null)
                    return i;
            }

            return -1;
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