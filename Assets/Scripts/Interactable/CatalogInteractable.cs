using DB;
using Global;
using Items;
using Player;
using UnityEngine;

namespace Interactable
{
    public class CatalogInteractable : Interactable
    {
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
            
            var recipe = itemsFactory.GetRecipe(new DBMask.MaskData(), false);
            playerHandsController.GiveItem(recipe);
            
            CompleteInteraction(interactor);
        }
    }
}