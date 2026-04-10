using System;
using UnityEngine;

namespace DB
{
    public class DBMask : MonoBehaviour
    {
        [Serializable]
        public struct MaskData
        {
            public string Id;
            public string OR_Id;
            public string ClientId;
            public string FaceCoverId;
            public string MistResistanceId;
            public string DistrictId;
            public string FactionId;

            public MaskData(
                string id,
                string orId,
                string clientId,
                string faceCoverId,
                string mistResistanceId,
                string districtId,
                string factionId)
            {
                Id = id;
                OR_Id = orId;
                ClientId = clientId;
                FaceCoverId = faceCoverId;
                MistResistanceId = mistResistanceId;
                DistrictId = districtId;
                FactionId = factionId;
            }
        }

        [SerializeField] private MaskData[] config;

        public bool TryGetMaskDataByOrderId(string orId, out MaskData result)
        {
            result = default;

            if (string.IsNullOrEmpty(orId))
            {
                Debug.LogError("[MaskConfig] OR_Id is null or empty.");
                return false;
            }

            if (config == null || config.Length == 0)
            {
                Debug.LogError("[MaskConfig] Mask config array is null or empty.");
                return false;
            }

            for (int i = 0; i < config.Length; i++)
            {
                if (string.IsNullOrEmpty(config[i].OR_Id))
                {
                    Debug.LogWarning($"[MaskConfig] MaskData at index {i} has empty OR_Id. MaskId: {config[i].Id}");
                    continue;
                }

                if (config[i].OR_Id == orId)
                {
                    result = config[i];
                    return true;
                }
            }

            Debug.LogError($"[MaskConfig] MaskData not found for OR_Id: {orId}");
            return false;
        }
    }
}
