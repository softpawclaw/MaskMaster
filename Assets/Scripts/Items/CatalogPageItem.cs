using Enums;
using UnityEngine;

namespace Items
{
    public class CatalogPageItem : ItemBase
    {
        [Header("Catalog Page")]
        [SerializeField] private ResourceType resourceType;

        public ResourceType ResourceType => resourceType;

        public void Init(ResourceType type)
        {
            resourceType = type;
        }

        public bool IsSameResource(ResourceType other)
        {
            return resourceType == other;
        }
    }
}