using DB;
using Enums;
using UnityEngine;

namespace Items
{
    public class CatalogPageItem : ItemBase
    {
        [Header("Catalog Page Base")]
        [SerializeField] private CatalogPageKind pageKind = Enums.CatalogPageKind.None;
        [SerializeField] private string pageId;
        [SerializeField] private string sourceDrawerId;

        [Header("Resolved Display Data")]
        [SerializeField] private string recipeName;
        [SerializeField] private string productionName;
        [SerializeField] private Sprite image;
        [SerializeField] private ResourceType mistResourceType = ResourceType.None;
        [SerializeField] private MaskSize faceCoverMaskSize;

        public Enums.CatalogPageKind PageKind => pageKind;
        public string PageId => pageId;
        public string SourceDrawerId => sourceDrawerId;
        public string RecipeName => recipeName;
        public string ProductionName => productionName;
        public Sprite Image => image;
        public ResourceType MistResourceType => mistResourceType;
        public MaskSize FaceCoverMaskSize => faceCoverMaskSize;

        public void Init(CatalogPageData data)
        {
            pageKind = data.PageKind;
            pageId = data.PageId;
            sourceDrawerId = data.SourceDrawerId;

            recipeName = string.Empty;
            productionName = string.Empty;
            image = null;
            mistResourceType = ResourceType.None;
            faceCoverMaskSize = default;
        }

        public void Init(CatalogPageData data, DBMistResistance.MistResistanceData resolvedData)
        {
            Init(data);
            recipeName = resolvedData.RecipeName;
            productionName = resolvedData.ProductionName;
            image = resolvedData.Image;
            mistResourceType = resolvedData.ResourceType;
        }

        public void Init(CatalogPageData data, DBFaceCover.FaceCoverData resolvedData)
        {
            Init(data);
            recipeName = resolvedData.RecipeName;
            productionName = resolvedData.ProductionName;
            image = resolvedData.Image;
            faceCoverMaskSize = resolvedData.MaskSize;
        }

        public void Init(CatalogPageData data, DBDistrict.DistrictData resolvedData)
        {
            Init(data);
            recipeName = resolvedData.RecipeName;
        }

        public void Init(CatalogPageData data, DBFaction.FactionData resolvedData)
        {
            Init(data);
            recipeName = resolvedData.RecipeName;
        }
    }
}
