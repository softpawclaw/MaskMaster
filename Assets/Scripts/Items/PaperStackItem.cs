using System.Collections.Generic;
using Enums;
using UnityEngine;

namespace Items
{
    public class PaperStackItem : ContainerItemBase
    {
        [Header("Paper Stack")]
        [SerializeField] private int selectedIndex = 0;

        [Header("Hand View")]
        [SerializeField] private List<Transform> pageSockets = new();

        public int SelectedIndex => selectedIndex;

        public override bool CanAccept(ItemBase item)
        {
            if (item == null) return false;
            if (item == this) return false;

            return item.PlacementType == PlacementType.Hangable && item.CanStackInHand;
        }

        public override void OnTakenToHand(Transform handSocket)
        {
            base.OnTakenToHand(handSocket);
            RefreshHandView();
        }

        public override void OnRemovedFromHand()
        {
            base.OnRemovedFromHand();
        }

        public override ItemBase GetSelectedItem()
        {
            if (items.Count == 0) return null;
            if (selectedIndex < 0 || selectedIndex >= items.Count) return null;

            return items[selectedIndex];
        }

        public override void SelectNext()
        {
            if (items.Count <= 1) return;

            selectedIndex++;
            if (selectedIndex >= items.Count)
            {
                selectedIndex = 0;
            }

            RefreshHandView();
        }

        public override void SelectPrevious()
        {
            if (items.Count <= 1) return;

            selectedIndex--;
            if (selectedIndex < 0)
            {
                selectedIndex = items.Count - 1;
            }

            RefreshHandView();
        }

        public override List<ItemBase> ExtractAllItems()
        {
            var extracted = base.ExtractAllItems();
            selectedIndex = 0;
            RefreshHandView();
            return extracted;
        }

        public override void LoadItems(IEnumerable<ItemBase> source)
        {
            base.LoadItems(source);

            if (items.Count == 0)
            {
                selectedIndex = 0;
            }
            else
            {
                selectedIndex = Mathf.Clamp(selectedIndex, 0, items.Count - 1);
            }

            RefreshHandView();
        }

        protected override void OnContainerChanged()
        {
            if (items.Count == 0)
            {
                selectedIndex = 0;
            }
            else
            {
                selectedIndex = Mathf.Clamp(selectedIndex, 0, items.Count - 1);
            }

            RefreshHandView();
        }

        private void RefreshHandView()
        {
            if (pageSockets == null || pageSockets.Count == 0)
            {
                HideAllPages();
                return;
            }

            if (items.Count == 0)
            {
                HideAllPages();
                return;
            }

            HideAllPages();

            int visibleCount = Mathf.Min(items.Count, pageSockets.Count);

            for (int visualIndex = 0; visualIndex < visibleCount; visualIndex++)
            {
                int itemIndex = (selectedIndex + visualIndex) % items.Count;

                var page = items[itemIndex];
                var socket = pageSockets[visualIndex];

                if (page == null || socket == null) continue;

                page.gameObject.SetActive(true);
                page.transform.SetParent(socket);
                page.transform.localPosition = Vector3.zero;
                page.transform.localRotation = Quaternion.identity;
            }
        }

        private void HideAllPages()
        {
            foreach (var page in items)
            {
                if (page == null) continue;
                page.gameObject.SetActive(false);
            }
        }
    }
}