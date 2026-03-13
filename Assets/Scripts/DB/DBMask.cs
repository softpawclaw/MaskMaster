using System;
using Enums;
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
            public MaskSize Size;
            public MaskMaterial Material;
            public MaskSocket[] Sockets;

            public MaskData(string id, string orId,  MaskSize size, MaskMaterial material, MaskSocket[] sockets)
            {
                Id = id;
                OR_Id = orId;
                Size = size;
                Material = material;
                Sockets = sockets;
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