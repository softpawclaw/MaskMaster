using System;
using UnityEngine;

namespace DB
{
    public class DBClients : MonoBehaviour
    {
        [Serializable]
        public struct ClientData
        {
            public string Id;
            [TextArea(2, 6)] public string Description;
        }

        [SerializeField] private ClientData[] config;

        public bool TryGetDescription(string clientId, out string description)
        {
            description = string.Empty;

            if (string.IsNullOrEmpty(clientId))
                return false;

            if (config == null || config.Length == 0)
                return false;

            for (int i = 0; i < config.Length; i++)
            {
                if (config[i].Id == clientId)
                {
                    description = config[i].Description;
                    return true;
                }
            }

            return false;
        }
    }
}
