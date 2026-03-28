using System.Collections.Generic;
using DB;
using Enums;
using UnityEngine;

namespace Items
{
    public class MainRecipeItem : ItemBase
    {
        [Header("Recipe")]
        [SerializeField] private DBMask.MaskData maskData;

        public DBMask.MaskData MaskData => maskData;
        public string OrderId => maskData.OR_Id;
        public string MaskId => maskData.Id;
        public MaskSize MaskSize => maskData.Size;
        public ResourceType Material => maskData.Material;
        public DBMask.MaskSocketResource[] Sockets => maskData.Sockets;

        public void Init(DBMask.MaskData data)
        {
            maskData = data;
        }

        public ResourceType GetBlankResourceType()
        {
            return maskData.Material;
        }

        public List<ResourceType> GetAllRequiredResourceTypes()
        {
            var result = new List<ResourceType>();

            if (maskData.Material != ResourceType.None)
            {
                result.Add(maskData.Material);
            }

            if (maskData.Sockets != null)
            {
                for (int i = 0; i < maskData.Sockets.Length; i++)
                {
                    if (maskData.Sockets[i].ResourceType != ResourceType.None)
                    {
                        result.Add(maskData.Sockets[i].ResourceType);
                    }
                }
            }

            return result;
        }
    }
}