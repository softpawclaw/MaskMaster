using System;
using Enums;
using UnityEngine;

namespace DB
{
    public class DBFaceCover : MonoBehaviour
    {
        [Serializable]
        public struct FaceCoverData
        {
            public string Id;
            public string RecipeName;
            public string ProductionName;
            public Sprite Image;
            public MaskSize MaskSize;
        }

        [SerializeField] private FaceCoverData[] config;

        public bool TryGetData(string id, out FaceCoverData result)
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
    }
}
