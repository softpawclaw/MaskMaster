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
            var tray = hands.GetTrayInHands();
            if (tray == null)
            {
                Debug.Log("No tray in hands");
                return;
            }

            if (!tray.TryExchangeSelectedResource(null, out var trayResource))
            {
                Debug.Log("Nothing to place: selected tray slot is empty");
                return;
            }

            if (trayResource == null)
            {
                Debug.Log("Nothing to place: selected tray slot is empty");
                return;
            }

            AttachItem(trayResource);
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

            if (!tray.TryExchangeSelectedResource(shelfResource, out var trayResource))
            {
                Debug.Log("Nothing to exchange");
                return;
            }

            if (trayResource == null)
            {
                ForgetCurrentItem();
                return;
            }

            AttachItem(trayResource);
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

        private void ForgetCurrentItem()
        {
            currentItem = null;
        }
    }
}