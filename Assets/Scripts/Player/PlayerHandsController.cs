using Enums;
using Items;
using UnityEngine;

namespace Player
{
    public class PlayerHandsController : MonoBehaviour
    {
        [SerializeField] private Transform rightHand;
        [SerializeField] private Transform leftHand;
        
        private ItemBase currentItem;
        
        public ItemBase ChooseItem(PlacementType placementType, ItemSize size)
        {
            return currentItem;
        }

        public void GiveItem(ItemBase item)
        {
            currentItem = item;
            item.transform.SetParent(rightHand);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
        }
        public void FreeItem(ItemBase item)
        {
            if (currentItem == item)
            {
                currentItem = null;
            }
        }
    }
}