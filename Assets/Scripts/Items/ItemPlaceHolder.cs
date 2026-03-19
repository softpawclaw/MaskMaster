using Enums;
using Player;
using UnityEngine;
namespace Items
{
    public class ItemPlaceHolder : Interactable.Interactable
    {
        [SerializeField] private PlacementType placementType;
        [SerializeField] private ItemSize size;
        
        private ItemBase currentItem = null;

        protected override void OnInteract(GameObject interactor)
        {
            var hands = interactor.GetComponent<PlayerHandsController>();
            
            if (!hands) return;
            
            if (currentItem ==null)
            {
                PlaceItem(hands);
            }
            else
            {
                ReplaceItem(hands);
            }
            
            CompleteInteraction(interactor);
        }
        
        private void PlaceItem(PlayerHandsController hands)
        {
            var inItem = hands.ChooseItem(placementType, size);
            
            if (!inItem)
            {
                Debug.Log("Refused to place empty item");
                return;
            }

            if (placementType != inItem.PlacementType)
            {
                Debug.Log($"Refused to place item {inItem.PlacementType} into {placementType}");
                return;
            }

            if (size < inItem.Size)
            {
                Debug.Log($"Refused to place item {inItem.Size} into {size}");
                return;
            }

            hands.FreeItem(inItem);
            currentItem = inItem;
            //TODO implement sockets
            currentItem.transform.SetParent(transform);
            currentItem.transform.SetPositionAndRotation(transform.position, transform.rotation);
        }

        private void ReplaceItem(PlayerHandsController hands)
        {
            var inItem = hands.ChooseItem(placementType, size);

            if (inItem && placementType != inItem.PlacementType)
            {
                Debug.Log($"Refused to place item {inItem.PlacementType} into {placementType}");
                return;
            }

            if (inItem&& size < inItem.Size)
            {
                Debug.Log($"Refused to place item {inItem.Size} into {size}");
                return;
            }

            if (inItem)
            {
                hands.FreeItem(inItem);
            }

            hands.GiveItem(currentItem);
            
            currentItem = inItem;
            currentItem?.transform.SetPositionAndRotation(transform.position, transform.rotation);
        }
    }
}