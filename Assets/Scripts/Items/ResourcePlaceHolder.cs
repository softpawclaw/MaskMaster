using Player;
using UnityEngine;

namespace Items
{
    public class ResourcePlaceHolder : ItemPlaceHolder
    {
        [Header("Init On Start")]
        [SerializeField] private ResourceItem startResourcePrefab;

        private void Start()
        {
            InitOnStart();
        }

        protected override bool CanAcceptItem(ItemBase item)
        {
            return item is ResourceItem;
        }

        protected override void PlaceItem(PlayerHandsController hands)
        {
            var inItem = hands.ChooseItem(
                placementType: Enums.PlacementType.Placeable,
                size: Enums.ItemSize.Small);

            if (inItem == null)
            {
                Debug.Log("No suitable resource item in hands");
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

        protected override void ReplaceItem(PlayerHandsController hands)
        {
            var tray = hands.GetTrayInHands();
            if (tray == null)
            {
                Debug.Log("No tray in hands");
                return;
            }

            if (currentItem is not ResourceItem shelfResource)
            {
                Debug.LogWarning("Current placeholder item is not ResourceItem");
                return;
            }

            if (!tray.TryReplaceSelected(shelfResource, out var replacedResource))
            {
                Debug.Log("Failed to place resource into tray");
                return;
            }

            currentItem = null;

            if (replacedResource != null)
            {
                AttachItem(replacedResource);
            }
        }

        private void InitOnStart()
        {
            if (currentItem != null)
                return;

            if (startResourcePrefab == null)
                return;

            var spawnedResource = Instantiate(startResourcePrefab);
            AttachItem(spawnedResource);
        }
    }
}