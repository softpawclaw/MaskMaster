using Enums;
using UnityEngine;

namespace Items
{
    public class ItemBase : MonoBehaviour
    {
        private const string HandsLayerName = "Hands";
        private const string WorldLayerName = "Default";

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
            SetHandsRenderLayer();
        }

        /// <summary>
        /// Вызывается перед извлечением из руки.
        /// </summary>
        public virtual void OnRemovedFromHand()
        {
            SetWorldRenderLayer();
        }

        public void SetHandsRenderLayer()
        {
            SetRenderLayerRecursive(LayerMask.NameToLayer(HandsLayerName));
        }

        public void SetWorldRenderLayer()
        {
            SetRenderLayerRecursive(LayerMask.NameToLayer(WorldLayerName));
        }

        public void SetRenderLayerRecursive(int targetLayer)
        {
            if (targetLayer < 0)
            {
                targetLayer = 0;
            }

            SetLayerRecursive(transform, targetLayer);
        }

        private static void SetLayerRecursive(Transform root, int targetLayer)
        {
            if (root == null)
                return;

            root.gameObject.layer = targetLayer;

            for (int i = 0; i < root.childCount; i++)
            {
                SetLayerRecursive(root.GetChild(i), targetLayer);
            }
        }
    }
}
