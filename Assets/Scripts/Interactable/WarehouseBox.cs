using Enums;
using Global;
using Helpers;
using Items;
using Player;
using UnityEngine;

namespace Interactable
{
    public class WarehouseBox : Interactable
    {
        [Header("Warehouse Box")]
        [SerializeField] private string boxId;
        [SerializeField] private GameObject boxVisualRoot;
        [SerializeField] private GameObject iconVisualRoot;
        [SerializeField] private SpriteRenderer iconRenderer;
        [SerializeField] private Collider[] boxColliders;

        private ResourceType resourceType = ResourceType.None;
        private bool canInteract;

        public string BoxId => boxId;
        public ResourceType ResourceType => resourceType;
        public bool CanInteract => canInteract;

        private void Awake()
        {
            CacheCollidersIfNeeded();
            ResetBox();
        }

        public void ResetBox()
        {
            resourceType = ResourceType.None;
            canInteract = false;
            SetRootActive(boxVisualRoot, false);
            SetBoxCollidersActive(false);
            SetIconActive(false);

            if (iconRenderer != null)
                iconRenderer.sprite = null;
        }

        public void Init(ResourceType type, Sprite icon)
        {
            resourceType = type;
            canInteract = type != ResourceType.None;

            SetRootActive(boxVisualRoot, true);
            SetBoxCollidersActive(true);

            if (type == ResourceType.None)
            {
                SetIconActive(false);
                if (iconRenderer != null)
                    iconRenderer.sprite = null;
                return;
            }

            if (iconRenderer == null)
            {
                Debug.LogWarning($"WarehouseBox '{boxId}': iconRenderer is not assigned for resource '{type}'.");
                SetIconActive(false);
                return;
            }

            if (icon == null)
            {
                Debug.LogWarning($"WarehouseBox '{boxId}': icon for resource '{type}' was not resolved.");
                SetIconActive(false);
                return;
            }

            iconRenderer.sprite = icon;
            SetIconActive(true);
        }

        protected override void OnInteract(GameObject interactor)
        {
            if (!canInteract || resourceType == ResourceType.None)
            {
                Debug.Log($"WarehouseBox '{boxId}': box is visible but has no resource interaction.");
                CompleteInteraction(interactor);
                return;
            }

            var hands = interactor != null ? interactor.GetComponent<PlayerHandsController>() : null;
            if (hands == null)
            {
                Debug.LogWarning($"WarehouseBox '{boxId}': PlayerHandsController not found on interactor.");
                CompleteInteraction(interactor);
                return;
            }

            var tray = EnsureTrayInHands(hands);
            if (tray == null)
            {
                CompleteInteraction(interactor);
                return;
            }

            if (!tray.UseWarehouseAutoMode)
            {
                Debug.LogWarning($"WarehouseBox '{boxId}': interacted while tray auto mode is disabled. Use old ResourcePlaceHolder flow or enable UseWarehouseAutoMode on TrayItem.");
                CompleteInteraction(interactor);
                return;
            }

            bool success = tray.TryToggleWarehouseResource(resourceType, CreateResourceForTray, out string message);
            if (!success && !string.IsNullOrEmpty(message))
                Debug.Log(message);

            CompleteInteraction(interactor);
        }

        private ResourceItem CreateResourceForTray(ResourceType type)
        {
            var factory = Linker.Instance != null ? Linker.Instance.ItemsFactory : null;
            if (factory == null)
            {
                Debug.LogError($"WarehouseBox '{boxId}': ItemsFactory is not available, cannot create resource '{type}'.");
                return null;
            }

            return factory.CreateResource(type);
        }

        private TrayItem EnsureTrayInHands(PlayerHandsController hands)
        {
            var tray = hands.GetTrayInHands();
            if (tray != null)
                return tray;

            var factory = Linker.Instance != null ? Linker.Instance.ItemsFactory : null;
            if (factory == null)
            {
                Debug.LogError($"WarehouseBox '{boxId}': ItemsFactory is not available, cannot auto-create tray.");
                return null;
            }

            tray = factory.CreateTray();
            if (tray == null)
                return null;

            if (hands.GiveItem(tray))
                return tray;

            Debug.LogWarning($"WarehouseBox '{boxId}': failed to give auto-created tray to player hands.");
            Destroy(tray.gameObject);
            return null;
        }

        private void SetIconActive(bool active)
        {
            SetRootActive(iconVisualRoot, active);
            if (iconRenderer != null)
                iconRenderer.enabled = active;
        }

        private void CacheCollidersIfNeeded()
        {
            if (boxColliders != null && boxColliders.Length > 0)
                return;

            boxColliders = GetComponentsInChildren<Collider>(true);
        }

        private void SetBoxCollidersActive(bool active)
        {
            CacheCollidersIfNeeded();

            if (boxColliders == null)
                return;

            for (int i = 0; i < boxColliders.Length; i++)
            {
                if (boxColliders[i] == null)
                    continue;

                boxColliders[i].enabled = active;
            }
        }

        private void SetRootActive(GameObject root, bool active)
        {
            if (root != null)
                root.SetActive(active);
            else
                gameObject.SetActive(active);
        }
    }
}
