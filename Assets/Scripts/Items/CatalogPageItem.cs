using DB;
using Enums;
using TMPro;
using UnityEngine;

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
        [Header("Sockets | D")] [SerializeField] private SpriteRenderer dTop = null;
        [Header("Sockets | D")] [SerializeField] private SpriteRenderer dMid = null;
        [Header("Sockets | D")] [SerializeField] private SpriteRenderer dBot = null;
        [Header("Sockets | F")] [SerializeField] private SpriteRenderer fTemplate = null;
        [Header("Sockets | F")] [SerializeField] private SpriteRenderer[] fTopTemplate;
        [Header("Sockets | F")] [SerializeField] private SpriteRenderer[] fMidTemplate;
        [Header("Sockets | F")] [SerializeField] private SpriteRenderer[] fBotTemplate;
        [Header("Sockets | F")] [SerializeField] private Sprite fTemplateImage;
        
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
            dTop.sprite = resolvedData.TopShapeImage;
            dMid.sprite = resolvedData.MidShapeImage;
            dBot.sprite = resolvedData.BotShapeImage;
        }

        public void Init(CatalogPageData data, DBFaction.FactionData resolvedData)
        {
            Init(data);
            recipeName = resolvedData.RecipeName;
            InitTitle();
            fTemplate.sprite = fTemplateImage;

            ParseIcons(resolvedData.TopSockets, fTopTemplate);
            ParseIcons(resolvedData.MidSockets, fMidTemplate);
            ParseIcons(resolvedData.BotSockets, fBotTemplate);
        }

        private void ParseIcons(DBFaction.SocketData[] data, SpriteRenderer[] containers)
        {
            for (int i = 0; i < data.Length; i++)
            {
                var dataPart = data[i];
                if (dataPart.ResourceType != ResourceType.None && dataPart.ResourceSprite)
                {
                    if (containers[i])
                    {
                        containers[i].sprite = dataPart.ResourceSprite;
                    }
                }
            }
        }

        private void InitTitle()
        {
            titleText.enabled = true;
            titleText.text = recipeName;
        }
    }
}