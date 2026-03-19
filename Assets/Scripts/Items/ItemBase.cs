using Enums;
using UnityEngine;

namespace Items
{
    public class ItemBase : MonoBehaviour
    {
        [SerializeField] private string itemId;
        [SerializeField] private PlacementType placementType;
        [SerializeField] private ItemSize size;
        [SerializeField] private bool canStackInHand;
        [SerializeField] private bool isContainer;
        [SerializeField] private bool isFreeStack;
        [SerializeField] private bool requiresContainer;
        [SerializeField] private bool isSelectableInStack = true;

        public string ItemId => itemId;
        public PlacementType PlacementType => placementType;
        public ItemSize Size => size;
        public bool CanStackInHand => canStackInHand;
        public bool IsContainer => isContainer;
        public bool IsFreeStack => isFreeStack;
        public bool RequiresContainer => requiresContainer;
        public bool IsSelectableInStack => isSelectableInStack;
    }
}
