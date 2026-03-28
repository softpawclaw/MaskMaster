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
            }

            OnContainerChanged();
        }

        protected virtual void OnContainerChanged()
        {
        }
    }
}