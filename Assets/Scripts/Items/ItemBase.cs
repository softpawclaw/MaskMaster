using Enums;
using UnityEngine;

namespace Items
{
    public class ItemBase : MonoBehaviour
    {
        [Header("Base")]
        [SerializeField] private string itemId;
        [SerializeField] private PlacementType placementType;
        [SerializeField] private ItemSize size;
        [SerializeField] private HandType preferredHand = HandType.Right;

        [Header("Flags")]
        [SerializeField] private bool canStackInHand;
        [SerializeField] private bool isSelectableInStack = true;

        public string ItemId => itemId;
        public PlacementType PlacementType => placementType;
        public ItemSize Size => size;
        public HandType PreferredHand => preferredHand;
        public bool CanStackInHand => canStackInHand;
        public bool IsSelectableInStack => isSelectableInStack;

        public virtual bool IsContainer => false;

        /// <summary>
        /// Вызывается рукой, когда предмет был выдан в руку.
        /// </summary>
        public virtual void OnTakenToHand(Transform handSocket)
        {
            transform.SetParent(handSocket);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }

        /// <summary>
        /// Вызывается перед извлечением из руки.
        /// </summary>
        public virtual void OnRemovedFromHand()
        {
        }
    }
}
