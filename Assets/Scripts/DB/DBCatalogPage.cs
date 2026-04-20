using System;
using UnityEngine;

namespace DB
{
    public class DBCatalogPage : MonoBehaviour
    {
        [Serializable]
        private struct CatalogPageRow
        {
            public CatalogPageData Data;
        }

        [SerializeField] private CatalogPageRow[] config;

        public bool TryGetPage(string pageId, out CatalogPageData data)
        {
            data = default;

            if (string.IsNullOrWhiteSpace(pageId) || config == null || config.Length == 0)
                return false;

            for (int i = 0; i < config.Length; i++)
            {
                if (string.Equals(config[i].Data.PageId, pageId, StringComparison.Ordinal))
                {
                    data = config[i].Data;
                    return true;
                }
            }

            return false;
        }

        public CatalogPageData GetPageOrDefault(string pageId)
        {
            return TryGetPage(pageId, out CatalogPageData data) ? data : default;
        }

        public bool Contains(string pageId)
        {
            return TryGetPage(pageId, out _);
        }

        public CatalogPageData[] GetAll()
        {
            if (config == null || config.Length == 0)
                return Array.Empty<CatalogPageData>();

            CatalogPageData[] result = new CatalogPageData[config.Length];
            for (int i = 0; i < config.Length; i++)
                result[i] = config[i].Data;

            return result;
        }
    }
}
