using System.Collections.Generic;
using DB;
using Enums;
using Global;
using TMPro;
using UnityEngine;

namespace Items
{
    public class MainRecipeItem : ItemBase
    {
        private const string NameClientTitle = "N_CLIENT";
        private const string NameOrderTitle = "N_ORDER";
        private const string NameFaceCover = "N_FaceCover";
        private const string NameMistResistance = "N_MistResistance";
        private const string NameDistrict = "N_District";
        private const string NameFaction = "N_Faction";

        [Header("Recipe")]
        [SerializeField] private DBMask.MaskData maskData;

        [Header("World Text")]
        [SerializeField] private TMP_Text clientTitleText;
        [SerializeField] private TMP_Text clientDescriptionText;
        [SerializeField] private TMP_Text orderTitleText;
        [SerializeField] private TMP_Text faceCoverLabelText;
        [SerializeField] private TMP_Text faceCoverValueText;
        [SerializeField] private TMP_Text mistResistanceLabelText;
        [SerializeField] private TMP_Text mistResistanceValueText;
        [SerializeField] private TMP_Text districtLabelText;
        [SerializeField] private TMP_Text districtValueText;
        [SerializeField] private TMP_Text factionLabelText;
        [SerializeField] private TMP_Text factionValueText;

        public DBMask.MaskData MaskData => maskData;
        public string OrderId => maskData.OR_Id;
        public string MaskId => maskData.Id;
        public string ClientId => maskData.ClientId;
        public string FaceCoverId => maskData.FaceCoverId;
        public string MistResistanceId => maskData.MistResistanceId;
        public string DistrictId => maskData.DistrictId;
        public string FactionId => maskData.FactionId;
        public MaskSize MaskSize => maskData.Size;
        public ResourceType Material => maskData.Material;
        public DBMask.MaskSocketResource[] Sockets => maskData.Sockets;

        public void Init(DBMask.MaskData data)
        {
            maskData = data;
            RefreshVisuals();
        }

        private void Start()
        {
            RefreshVisuals();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (!Application.isPlaying)
            {
                RefreshVisuals();
            }
        }
#endif

        public void RefreshVisuals()
        {
            string clientTitle = ResolveName(NameClientTitle, "CLIENT");
            string orderTitle = ResolveName(NameOrderTitle, "ORDER");
            string faceCoverLabel = ResolveName(NameFaceCover, "FaceCover");
            string mistResistanceLabel = ResolveName(NameMistResistance, "MistResistance");
            string districtLabel = ResolveName(NameDistrict, "District");
            string factionLabel = ResolveName(NameFaction, "Faction");

            SetText(clientTitleText, clientTitle);
            SetText(orderTitleText, orderTitle);
            SetText(faceCoverLabelText, faceCoverLabel);
            SetText(mistResistanceLabelText, mistResistanceLabel);
            SetText(districtLabelText, districtLabel);
            SetText(factionLabelText, factionLabel);

            SetText(clientDescriptionText, ResolveClientDescription(maskData.ClientId, maskData.ClientId));
            SetText(faceCoverValueText, ResolveTag(maskData.FaceCoverId));
            SetText(mistResistanceValueText, ResolveTag(maskData.MistResistanceId));
            SetText(districtValueText, ResolveTag(maskData.DistrictId));
            SetText(factionValueText, ResolveTag(maskData.FactionId));
        }

        public ResourceType GetBlankResourceType()
        {
            return maskData.Material;
        }

        public List<ResourceType> GetAllRequiredResourceTypes()
        {
            var result = new List<ResourceType>();

            if (maskData.Material != ResourceType.None)
            {
                result.Add(maskData.Material);
            }

            if (maskData.Sockets != null)
            {
                for (int i = 0; i < maskData.Sockets.Length; i++)
                {
                    if (maskData.Sockets[i].ResourceType != ResourceType.None)
                    {
                        result.Add(maskData.Sockets[i].ResourceType);
                    }
                }
            }

            return result;
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target == null)
                return;

            target.text = value;
        }

        private string ResolveClientDescription(string clientId, string fallback)
        {
            var linker = Linker.Instance;
            if (linker != null && linker.DBClients != null && linker.DBClients.TryGetDescription(clientId, out var description))
            {
                return description;
            }

            return fallback;
        }

        private string ResolveTag(string tagId)
        {
            var linker = Linker.Instance;
            if (linker != null && linker.DBMainRecipe != null && linker.DBMainRecipe.TryGetValue(tagId, out var value))
            {
                return value;
            }

            return tagId;
        }

        private string ResolveName(string nameId, string fallback)
        {
            var linker = Linker.Instance;
            if (linker != null && linker.DBNames != null && linker.DBNames.TryGetValue(nameId, out var value))
            {
                return value;
            }

            return fallback;
        }
    }
}
