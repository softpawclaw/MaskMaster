using Enums;
using UnityEngine;

namespace Items
{
    public class ResourceItem : ItemBase
    {
        [Header("Resource")]
        [SerializeField] private ResourceType resourceType = ResourceType.None;

        public ResourceType Type => resourceType;
    }
}