using System;
using DB;
using Enums;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Items
{
    public class CatalogPageItem : ItemBase
    {
        [Header("Catalog Page Base")]
        [SerializeField] private CatalogPageKind pageKind = Enums.CatalogPageKind.None;
        [SerializeField] private string pageId;
        [SerializeField] private string sourceDrawerId;

        [Header("Sockets")] [SerializeField] private TextMeshPro titleText = null;
        [Header("Sockets | MR")] [SerializeField] private SpriteRenderer mrIcon = null;
        [Header("Sockets | FC")] [SerializeField] private SpriteRenderer fcIcon = null;
        
        private string recipeName;
        private string productionName;
        private Sprite image;
        private ResourceType mistResourceType = ResourceType.None;
        private MaskSize faceCoverMaskSize;
        
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
            InitTitle();
            mrIcon.sprite = image;
        }

        public void Init(CatalogPageData data, DBFaceCover.FaceCoverData resolvedData)
        {
            Init(data);
            recipeName = resolvedData.RecipeName;
            productionName = resolvedData.ProductionName;
            image = resolvedData.Image;
            faceCoverMaskSize = resolvedData.MaskSize;
            InitTitle();
            fcIcon.sprite = image;
        }

        public void Init(CatalogPageData data, DBDistrict.DistrictData resolvedData)
        {
            Init(data);
            recipeName = resolvedData.RecipeName;
            InitTitle();
        }

        public void Init(CatalogPageData data, DBFaction.FactionData resolvedData)
        {
            Init(data);
            recipeName = resolvedData.RecipeName;
            InitTitle();
        }

        private void InitTitle()
        {
            titleText.enabled = true;
            titleText.text = recipeName;
        }

        private void ClearAllSockets()
        {
            titleText.text = "";
            titleText.enabled = false;
        }
    }
}