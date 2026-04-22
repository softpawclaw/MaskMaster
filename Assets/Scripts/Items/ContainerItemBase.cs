using System.Collections.Generic;
using UnityEngine;

namespace Items
{
    public abstract class ContainerItemBase : ItemBase
    {
        [Header("Container")]
        [SerializeField] protected List<ItemBase> items = new();

        public override bool IsContainer => true;

        public IReadOnlyList<ItemBase> Items => items;
        public int Count => items.Count;

        public virtual bool CanAccept(ItemBase item)
        {
            return item != null;
        }

        public virtual bool TryAdd(ItemBase item)
        {
            if (item == null) return false;
            if (!CanAccept(item)) return false;
            if (items.Contains(item)) return false;

            items.Add(item);

            // Базово просто цепляем к контейнеру.
            // Наследник потом сам раскидает по нужным сокетам.
            item.transform.SetParent(transform);
            item.transform.localPosition = Vector3.zero;
            item.transform.localRotation = Quaternion.identity;
            item.SetRenderLayerRecursive(gameObject.layer);

            OnContainerChanged();
            return true;
        }

        public virtual bool TryRemove(ItemBase item)
        {
            if (item == null) return false;
            if (!items.Remove(item)) return false;

            OnContainerChanged();
            return true;
        }

        public virtual ItemBase GetSelectedItem()
        {
            return null;
        }

        public virtual void SelectNext() { }
        public virtual void SelectPrevious() { }

        public virtual List<ItemBase> ExtractAllItems()
        {
            var result = new List<ItemBase>(items);
            items.Clear();
            OnContainerChanged();
            return result;
        }

        public virtual void LoadItems(IEnumerable<ItemBase> source)
        {
            items.Clear();

            foreach (var item in source)
            {
                if (item == null) continue;
                items.Add(item);
                item.transform.SetParent(transform);
                item.SetRenderLayerRecursive(gameObject.layer);
            }

            OnContainerChanged();
        }
        
        public virtual int GetDisplayCount(int maxSlots)
        {
            return Mathf.Min(items.Count, maxSlots);
        }

        public virtual ItemBase GetDisplayItemAt(int displayIndex)
        {
            if (displayIndex < 0 || displayIndex >= items.Count)
                return null;

            return items[displayIndex];
        }

        public override void OnTakenToHand(Transform handSocket)
        {
            base.OnTakenToHand(handSocket);

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null)
                    continue;

                items[i].SetRenderLayerRecursive(gameObject.layer);
            }
        }

        public override void OnRemovedFromHand()
        {
            base.OnRemovedFromHand();

            for (int i = 0; i < items.Count; i++)
            {
                if (items[i] == null)
                    continue;

                items[i].SetRenderLayerRecursive(gameObject.layer);
            }
        }

        protected virtual void OnContainerChanged()
        {
        }
    }
}
