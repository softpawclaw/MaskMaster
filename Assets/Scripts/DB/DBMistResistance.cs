using System;
using Enums;
using UnityEngine;

namespace DB
{
    public class DBMistResistance : MonoBehaviour
    {
        [Serializable]
        public struct MistResistanceData
        {
            public string Id;
            public string RecipeName;
            public string ProductionName;
            public Sprite Image;
            public ResourceType ResourceType;
        }

        [SerializeField] private MistResistanceData[] config;

        public bool TryGetData(string id, out MistResistanceData result)
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

        public bool TryGetRecipeName(string id, out string value)
        {
            value = string.Empty;
            if (!TryGetData(id, out var data))
                return false;

            value = data.RecipeName;
            return true;
        }

        public bool TryGetResourceType(string id, out ResourceType value)
        {
            value = ResourceType.None;
            if (!TryGetData(id, out var data))
                return false;

            value = data.ResourceType;
            return true;
        }

        public MistResistanceData[] GetAll()
        {
            return config != null ? ( MistResistanceData[] )config.Clone() : System.Array.Empty<MistResistanceData>();
        }
    }
}
