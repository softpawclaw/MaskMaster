using System;
using UnityEngine;

namespace DB
{
    public class DBMainRecipe : MonoBehaviour
    {
        [Serializable]
        public struct TagTextData
        {
            public string Id;
            public string Value;
        }

        [SerializeField] private TagTextData[] config;

        public bool TryGetValue(string id, out string value)
        {
            value = string.Empty;

            if (string.IsNullOrEmpty(id))
                return false;

            if (config == null || config.Length == 0)
                return false;

            for (int i = 0; i < config.Length; i++)
            {
                if (config[i].Id == id)
                {
                    value = config[i].Value;
                    return true;
                }
            }

            return false;
        }
    }
}
