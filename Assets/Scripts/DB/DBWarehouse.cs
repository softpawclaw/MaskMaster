using System;
using Enums;
using UnityEngine;

namespace DB
{
    public class DBWarehouse : MonoBehaviour
    {
        [Serializable]
        public struct ResourceBoxesData
        {
            public ResourceType ResourceType;
            public string[] BoxIds;
        }

        [Serializable]
        public struct WarehouseDayData
        {
            public int Day;
            public ResourceBoxesData[] Resources;
        }

        [SerializeField] private WarehouseDayData[] config = Array.Empty<WarehouseDayData>();

        public bool TryGetData(int day, out WarehouseDayData result)
        {
            result = default;

            if (config == null || config.Length == 0)
                return false;

            for (int i = 0; i < config.Length; i++)
            {
                if (config[i].Day != day)
                    continue;

                result = config[i];
                return true;
            }

            return false;
        }
    }
}
