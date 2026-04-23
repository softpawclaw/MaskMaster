using System;
using UnityEngine;

namespace DB
{
    public class DBDistrict : MonoBehaviour
    {
        [Serializable]
        public struct DistrictData
        {
            public string Id;
            public string RecipeName;
            public Sprite TopShapeImage;
            public Sprite MidShapeImage;
            public Sprite BotShapeImage;
        }

        [SerializeField] private DistrictData[] config;

        public bool TryGetRecipeName(string id, out string value)
        {
            value = string.Empty;

            if (string.IsNullOrEmpty(id) || config == null || config.Length == 0)
                return false;

            for (int i = 0; i < config.Length; i++)
            {
                if (config[i].Id == id)
                {
                    value = config[i].RecipeName;
                    return true;
                }
            }

            return false;
        }

        public DistrictData[] GetAll()
        {
            return config != null ? ( DistrictData[] )config.Clone() : System.Array.Empty<DistrictData>();
        }
    }
}
