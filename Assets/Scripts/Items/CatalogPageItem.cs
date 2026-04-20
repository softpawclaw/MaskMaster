using Enums;
using UnityEngine;

namespace Items
{
    public class CatalogPageItem : ItemBase
    {
        public enum CatalogPageKind
        {
            MistResistance = 0,
            FaceCover = 1,
            District = 2,
            Faction = 3
        }

        [Header("Catalog Page")]
        [SerializeField] private CatalogPageKind pageKind;
        [SerializeField] private string pageValueId;
        [SerializeField] private string sourceDrawerId;
        [SerializeField] private ResourceType resourceType;

        public CatalogPageKind PageKind => pageKind;
        public string PageValueId => pageValueId;
        public string SourceDrawerId => sourceDrawerId;
        public ResourceType ResourceType => resourceType;

        public void Init(ResourceType type)
        {
            resourceType = type;
        }

        public void Init(CatalogPageKind kind, string valueId, string drawerId, ResourceType type = ResourceType.None)
        {
            pageKind = kind;
            pageValueId = valueId;
            sourceDrawerId = drawerId;
            resourceType = type;
        }

        public bool IsSameResource(ResourceType other)
        {
            return resourceType == other;
        }
    }
}
