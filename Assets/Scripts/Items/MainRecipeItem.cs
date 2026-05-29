using System.Collections.Generic;
using DB;
using Enums;
using Global;
using Interactable.MaskWorkbench;
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
        public MaskSize MaskSize => ResolveMaskSize();
        public MaskSegment[] ExpectedSegments => ResolveExpectedSegments();
        public ResourceType Material => GetBlankResourceType();
        public DBMaskCombination.MaskSegmentResource[] Inlays => GetExpectedInlays();

        public void Init(DBMask.MaskData data)
        {
            maskData = data;
            RefreshVisuals();
        }

        private void Start()
        {
            RefreshVisuals();
        }

        public override void OnTakenToHand(Transform handSocket)
        {
            base.OnTakenToHand(handSocket);
            gameObject.SetActive(true);
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
            SetText(faceCoverValueText, ResolveFaceCover(maskData.FaceCoverId));
            SetText(mistResistanceValueText, ResolveMistResistance(maskData.MistResistanceId));
            SetText(districtValueText, ResolveDistrict(maskData.DistrictId));
            SetText(factionValueText, ResolveFaction(maskData.FactionId));
        }

        public ResourceType GetBlankResourceType()
        {
            var linker = Linker.Instance;
            if (linker != null && linker.DBMistResistance != null && linker.DBMistResistance.TryGetResourceType(maskData.MistResistanceId, out var resourceType))
            {
                return resourceType;
            }

            return ResourceType.None;
        }

        public MaskSize ResolveMaskSize()
        {
            var linker = Linker.Instance;
            if (linker != null && linker.DBFaceCover != null && linker.DBFaceCover.TryGetData(maskData.FaceCoverId, out var faceCoverData))
            {
                return faceCoverData.MaskSize;
            }

            return MaskSize.None;
        }

        public MaskSegment[] ResolveExpectedSegments()
        {
            var linker = Linker.Instance;
            if (linker != null && linker.DBFaceCover != null && linker.DBFaceCover.TryGetData(maskData.FaceCoverId, out var faceCoverData))
            {
                return DBFaceCover.ResolveSegments(faceCoverData);
            }

            return DBFaceCover.ResolveSegmentsFromSize(MaskSize);
        }

        public DBMaskCombination.MaskSegmentResource[] GetExpectedInlays()
        {
            var linker = Linker.Instance;
            if (linker != null && linker.DBMaskCombination != null && linker.DBMaskCombination.TryGetCombination(maskData.DistrictId, maskData.FactionId, out var combination))
            {
                return FilterExpectedInlaysBySegments(combination.Resources, ExpectedSegments);
            }

            return null;
        }

        private static DBMaskCombination.MaskSegmentResource[] FilterExpectedInlaysBySegments(DBMaskCombination.MaskSegmentResource[] source, MaskSegment[] allowedSegments)
        {
            if (source == null || source.Length == 0)
                return source;

            List<DBMaskCombination.MaskSegmentResource> result = new List<DBMaskCombination.MaskSegmentResource>();

            for (int i = 0; i < source.Length; i++)
            {
                if (source[i].ResourceType == ResourceType.None)
                    continue;

                if (!ContainsSegment(allowedSegments, source[i].Segment))
                    continue;

                result.Add(source[i]);
            }

            return result.ToArray();
        }

        private static bool ContainsSegment(MaskSegment[] segments, MaskSegment segment)
        {
            if (segments == null)
                return false;

            for (int i = 0; i < segments.Length; i++)
            {
                if (segments[i] == segment)
                    return true;
            }

            return false;
        }

        public List<ResourceType> GetAllRequiredResourceTypes()
        {
            var result = new List<ResourceType>();
            var material = GetBlankResourceType();

            if (material != ResourceType.None)
            {
                result.Add(material);
            }

            var inlays = GetExpectedInlays();
            if (inlays != null)
            {
                for (int i = 0; i < inlays.Length; i++)
                {
                    if (inlays[i].ResourceType != ResourceType.None)
                    {
                        result.Add(inlays[i].ResourceType);
                    }
                }
            }

            return result;
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target == null)
                return;

            if (target.gameObject != null && !target.gameObject.activeSelf)
            {
                target.gameObject.SetActive(true);
            }

            if (!target.enabled)
            {
                target.enabled = true;
            }

            target.text = value;
            target.ForceMeshUpdate(true, true);
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

        private string ResolveFaceCover(string id)
        {
            var linker = Linker.Instance;
            if (linker != null && linker.DBFaceCover != null && linker.DBFaceCover.TryGetRecipeName(id, out var value))
            {
                return value;
            }

            return id;
        }

        private string ResolveMistResistance(string id)
        {
            var linker = Linker.Instance;
            if (linker != null && linker.DBMistResistance != null && linker.DBMistResistance.TryGetRecipeName(id, out var value))
            {
                return value;
            }

            return id;
        }

        private string ResolveDistrict(string id)
        {
            var linker = Linker.Instance;
            if (linker != null && linker.DBDistrict != null && linker.DBDistrict.TryGetRecipeName(id, out var value))
            {
                return value;
            }

            return id;
        }

        private string ResolveFaction(string id)
        {
            var linker = Linker.Instance;
            if (linker != null && linker.DBFaction != null && linker.DBFaction.TryGetRecipeName(id, out var value))
            {
                return value;
            }

            return id;
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
