using System;
using Enums;
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
            public SocketData[] TopSockets;
            public SocketData[] MidSockets;
            public SocketData[] BotSockets;
        }
        
        [Serializable]
        public struct SocketData
        {
            public ResourceType ResourceType;
            public Sprite ResourceSprite;
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

        public FactionData[] GetAll()
        {
            return config != null ? ( FactionData[] )config.Clone() : System.Array.Empty<FactionData>();
        }
    }
}
