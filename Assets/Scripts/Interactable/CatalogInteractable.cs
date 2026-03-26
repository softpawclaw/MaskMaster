using Global;
using Items;
using Player;
using UnityEngine;
using Enums;

namespace Interactable
{
    public class CatalogInteractable : Interactable
    {
        [SerializeField] private ResourceType pageType;

        private PlayerHandsController playerHandsController = null;
        private ItemsFactory itemsFactory = null;

        public void Link()
        {
            playerHandsController = Linker.Instance.PlayerHandsController;
            itemsFactory = Linker.Instance.ItemsFactory;
        }

        protected override void OnInteract(GameObject interactor)
        {
            if (playerHandsController == null)
            {
                playerHandsController = interactor.GetComponent<PlayerHandsController>();
            }

            var page = itemsFactory.CreateCatalogPage(pageType);
            playerHandsController.GiveItem(page);

            CompleteInteraction(interactor);
        }
    }
}