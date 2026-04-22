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

            Transform holderSocket = containerSocket != null ? containerSocket : transform;

            currentContainer.transform.SetParent(holderSocket);
            currentContainer.transform.localPosition = Vector3.zero;
            currentContainer.transform.localRotation = Quaternion.identity;
            currentContainer.gameObject.SetActive(true);

            RebuildVisualsFromContainer();
            NotifyContentChanged();
        }

        public void AttachExternalContainer(ContainerItemBase container)
        {
            if (container == null)
                return;

            if (currentContainer != null || currentItem != null || placedContent.Count > 0)
            {
                EmergencyClearAndDestroy();
            }

            AttachItem(container);
        }

        public ContainerItemBase DetachCurrentContainer()
        {
            if (currentContainer == null)
                return null;

            ContainerItemBase containerToReturn = currentContainer;
            ClearPlacedContentVisuals();
            currentItem = null;
            currentContainer = null;
            NotifyContentChanged();
            return containerToReturn;
        }

        public void RefreshCurrentContainerView()
        {
            if (currentContainer == null)
                return;

            RebuildVisualsFromContainer();
            NotifyContentChanged();
        }

        public void EmergencyClearAndDestroy()
        {
            if (currentContainer == null && placedContent.Count == 0 && currentItem == null)
            {
                NotifyContentChanged();
                return;
            }

            ClearPlacedContentVisuals();

            if (currentContainer != null)
            {
                List<ItemBase> remainingItems = currentContainer.ExtractAllItems();
                if (remainingItems != null)
                {
                    for (int i = 0; i < remainingItems.Count; i++)
                    {
                        if (remainingItems[i] == null)
                            continue;

                        Destroy(remainingItems[i].gameObject);
                    }
                }

                Destroy(currentContainer.gameObject);
            }
            else if (currentItem != null)
            {
                Destroy(currentItem.gameObject);
            }

            currentItem = null;
            currentContainer = null;
            NotifyContentChanged();
        }

        private void ReturnCurrentContainerToHands(PlayerHandsController hands)
        {
            if (currentContainer == null)
                return;

            ContainerItemBase containerToReturn = currentContainer;
            ClearPlacedContentVisuals();
            currentItem = null;
            currentContainer = null;

            if (!hands.GiveItem(containerToReturn))
            {
                Debug.LogWarning("Failed to return complex item to hands");
                AttachItem(containerToReturn);
                return;
            }

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

                    var spawned = Instantiate(prefab);
                    spawnedContent.Add(spawned);
                }

                spawnedContainer.LoadItems(spawnedContent);
            }

            AttachItem(spawnedContainer);
        }

        private void RebuildVisualsFromContainer()
        {
            ClearPlacedContentVisuals();

            if (currentContainer == null)
                return;

            if (contentSockets == null || contentSockets.Count == 0)
                return;

            int visibleCount = currentContainer.GetDisplayCount(contentSockets.Count);
            if (visibleCount <= 0)
                return;

            for (int i = 0; i < visibleCount; i++)
            {
                ItemBase childItem = currentContainer.GetDisplayItemAt(i);
                if (childItem == null)
                    continue;

                Transform socket = contentSockets[i];
                if (socket == null)
                {
                    childItem.gameObject.SetActive(false);
                    childItem.transform.SetParent(currentContainer.transform);
                    continue;
                }

                childItem.gameObject.SetActive(true);
                childItem.transform.SetParent(socket);
                childItem.transform.localPosition = Vector3.zero;
                childItem.transform.localRotation = Quaternion.identity;
                placedContent.Add(childItem);
            }

            IReadOnlyList<ItemBase> rawItems = currentContainer.Items;
            for (int i = 0; i < rawItems.Count; i++)
            {
                ItemBase item = rawItems[i];
                if (item == null)
                    continue;

                if (placedContent.Contains(item))
                    continue;

                item.gameObject.SetActive(false);
                item.transform.SetParent(currentContainer.transform);
            }
        }

        private void ClearPlacedContentVisuals()
        {
            for (int i = 0; i < placedContent.Count; i++)
            {
                ItemBase child = placedContent[i];
                if (child == null)
                    continue;

                if (currentContainer != null)
                    child.transform.SetParent(currentContainer.transform);
                child.gameObject.SetActive(false);
            }

            placedContent.Clear();
        }

        private void NotifyContentChanged()
        {
            OnContentChanged?.Invoke();
        }
    }
}
