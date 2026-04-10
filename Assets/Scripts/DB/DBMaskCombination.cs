using System;
using Enums;
using UnityEngine;

namespace DB
{
    public class DBMaskCombination : MonoBehaviour
    {
        [Serializable]
        public struct MaskSocketResource
        {
            public MaskSocket Socket;
            public ResourceType ResourceType;

            public MaskSocketResource(MaskSocket socket, ResourceType resourceType)
            {
                Socket = socket;
                ResourceType = resourceType;
            }
        }

        [Serializable]
        public struct CombinationData
        {
            public string Id;
            public MaskSocketResource[] Sockets;
        }

        [SerializeField] private CombinationData[] config;

        public static string BuildId(string districtId, string factionId)
        {
            return $"{districtId}_{factionId}";
        }

        public bool TryGetCombination(string districtId, string factionId, out CombinationData result)
        {
            return TryGetCombinationById(BuildId(districtId, factionId), out result);
        }

        public bool TryGetCombinationById(string id, out CombinationData result)
        {
            result = default;

            if (string.IsNullOrEmpty(id) || config == null || config.Length == 0)
                return false;

            for (int i = 0; i < config.Length; i++)
            {
                if (config[i].Id == id)
                {
                    result = config[i];
                    return true;
                }
            }

            return false;
        }
    }
}
