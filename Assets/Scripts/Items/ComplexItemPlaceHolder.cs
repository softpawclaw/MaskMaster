using System;
using System.Collections.Generic;
using Player;
using UnityEngine;

namespace Items
{
    public class ComplexItemPlaceHolder : ItemPlaceHolder
    {
        [Header("Content Sockets")]
        [SerializeField] private List<Transform> contentSockets = new();

        [Header("Init On Start")]
        [SerializeField] private ContainerItemBase startContainerPrefab;
        [SerializeField] private List<ItemBase> startContentPrefabs = new();

        private readonly List<ItemBase> placedContent = new();
        private ContainerItemBase currentContainer;

        public event Action OnContentChanged;

        private void Start()
        {
            InitOnStart();
        }

        protected override bool CanAcceptItem(ItemBase item)
        {
            if (item is not ContainerItemBase)
                return false;

            // Для бумажной стопки сохраняем старую проверку вместимости сокетов.
            if (item is PaperStackItem stack && contentSockets.Count > 0 && stack.Count > contentSockets.Count)
                return false;

            return true;
        }

        protected override void PlaceItem(PlayerHandsController hands)
        {
            var inItem = hands.ChooseItem(placementType, size);
            if (inItem == null)
            {
                Debug.Log("No suitable complex item in hands");
                return;
            }

            if (!CanAcceptItem(inItem))
            {
                Debug.Log($"Refused to place item {inItem.ItemId} into complex holder");
                return;
            }

            hands.FreeItem(inItem);
            AttachItem(inItem);
        }

        protected override void ReplaceItem(PlayerHandsController hands)
        {
            ReturnCurrentContainerToHands(hands);
        }

        protected override void AttachItem(ItemBase item)
        {
            currentItem = item;
            currentContainer = item as ContainerItemBase;

            if (currentContainer == null)
            {
                Debug.LogWarning("ComplexItemPlaceHolder received non-container item");
                return;
            }

            var holderSocket = containerSocket != null ? containerSocket : transform;

            currentContainer.transform.SetParent(holderSocket);
            currentContainer.transform.localPosition = Vector3.zero;
            currentContainer.transform.localRotation = Quaternion.identity;
            currentContainer.gameObject.SetActive(true);

            ClearPlacedContentVisuals();

            if (contentSockets == null || contentSockets.Count == 0)
            {
                NotifyContentChanged();
                return;
            }

            var extractedItems = currentContainer.ExtractAllItems();
            if (extractedItems == null || extractedItems.Count == 0)
            {
                NotifyContentChanged();
                return;
            }

            var overflowItems = new List<ItemBase>();

            for (int i = 0; i < extractedItems.Count; i++)
            {
                var childItem = extractedItems[i];

                if (childItem == null)
                    continue;

                if (i >= contentSockets.Count || contentSockets[i] == null)
                {
                    overflowItems.Add(childItem);
                    continue;
                }

                var socket = contentSockets[i];

                childItem.gameObject.SetActive(true);
                childItem.transform.SetParent(socket);
                childItem.transform.localPosition = Vector3.zero;
                childItem.transform.localRotation = Quaternion.identity;

                placedContent.Add(childItem);
            }

            // Всё, что не влезло в настроенные сокеты, возвращаем обратно в контейнер.
            if (overflowItems.Count > 0)
            {
                currentContainer.LoadItems(overflowItems);
            }

            NotifyContentChanged();
        }

        private void ReturnCurrentContainerToHands(PlayerHandsController hands)
        {
            if (currentContainer == null)
                return;

            var containerToReturn = currentContainer;

            if (!hands.GiveItem(containerToReturn))
            {
                Debug.LogWarning("Failed to return complex item to hands");
                return;
            }

            foreach (var childItem in placedContent)
            {
                if (childItem == null)
                    continue;

                var success = hands.GiveItem(childItem);
                if (!success)
                {
                    Debug.LogWarning($"Failed to return child item {childItem.ItemId} into hands/container");
                }
            }

            placedContent.Clear();
            currentItem = null;
            currentContainer = null;
            NotifyContentChanged();
        }

        private void InitOnStart()
        {
            if (currentItem != null)
                return;

            if (startContainerPrefab == null)
                return;

            var spawnedContainer = Instantiate(startContainerPrefab);

            if (startContentPrefabs != null && startContentPrefabs.Count > 0)
            {
                var spawnedContent = new List<ItemBase>();

                foreach (var prefab in startContentPrefabs)
                {
                    if (prefab == null)
                        continue;

                    var spawnedItem = Instantiate(prefab);
                    spawnedContent.Add(spawnedItem);
                }

                spawnedContainer.LoadItems(spawnedContent);
            }

            AttachItem(spawnedContainer);
        }

        private void ClearPlacedContentVisuals()
        {
            for (int i = 0; i < placedContent.Count; i++)
            {
                if (placedContent[i] == null)
                    continue;

                placedContent[i].transform.SetParent(null);
            }

            placedContent.Clear();
        }

        private void NotifyContentChanged()
        {
            OnContentChanged?.Invoke();
        }
    }
}