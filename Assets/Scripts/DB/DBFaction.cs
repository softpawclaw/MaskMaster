using System;
using UnityEngine;

namespace DB
{
    public class DBFaction : MonoBehaviour
    {
        [Serializable]
        public struct FactionData
        {
            public string Id;
            public string RecipeName;
        }

        [SerializeField] private FactionData[] config;

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
    }
}
