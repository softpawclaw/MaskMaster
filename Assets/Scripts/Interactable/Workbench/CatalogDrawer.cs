using System;
using System.Collections.Generic;
using DB;
using UnityEngine;
using UnityEngine.Serialization;

namespace Interactable.Workbench
{
    public class CatalogDrawer : MonoBehaviour
    {
        [SerializeField] private string drawerId;
        [FormerlySerializedAs("pageDatabase")] [SerializeField] private DBCatalogPage page;
        [SerializeField] private string[] initialPageIds;

        private readonly List<string> currentPageIds = new();

        public string DrawerId => drawerId;
        public DBCatalogPage Page => page;
        public int Count => currentPageIds.Count;

        private void Awake()
        {
            ResetToInitialState();
        }

        public void Link(DBCatalogPage database)
        {
            if (database != null)
                page = database;

            ResetToInitialState();
        }

        public void ResetToInitialState()
        {
            currentPageIds.Clear();

            if (initialPageIds == null || initialPageIds.Length == 0)
                return;

            for (int i = 0; i < initialPageIds.Length; i++)
            {
                string pageId = initialPageIds[i];
                if (string.IsNullOrWhiteSpace(pageId))
                    continue;

                if (page != null && !page.Contains(pageId))
                {
                    Debug.LogWarning($"{name}: page id '{pageId}' is missing in CatalogPageDatabase.");
                    continue;
                }

                currentPageIds.Add(pageId);
            }
        }

        public bool HasAnyPages()
        {
            return currentPageIds.Count > 0;
        }

        public string[] GetPageIdsSnapshot()
        {
            return currentPageIds.ToArray();
        }

        public bool TryGetPageDataSnapshot(out CatalogPageData[] pages)
        {
            pages = Array.Empty<CatalogPageData>();

            if (page == null || currentPageIds.Count == 0)
                return false;

            List<CatalogPageData> result = new(currentPageIds.Count);

            for (int i = 0; i < currentPageIds.Count; i++)
            {
                if (!page.TryGetPage(currentPageIds[i], out CatalogPageData data))
                    continue;

                result.Add(data);
            }

            if (result.Count == 0)
                return false;

            pages = result.ToArray();
            return true;
        }

        public bool ContainsPage(string pageId)
        {
            if (string.IsNullOrWhiteSpace(pageId))
                return false;

            for (int i = 0; i < currentPageIds.Count; i++)
            {
                if (string.Equals(currentPageIds[i], pageId, StringComparison.Ordinal))
                    return true;
            }

            return false;
        }

        public bool RemovePage(string pageId)
        {
            if (string.IsNullOrWhiteSpace(pageId))
                return false;

            for (int i = 0; i < currentPageIds.Count; i++)
            {
                if (!string.Equals(currentPageIds[i], pageId, StringComparison.Ordinal))
                    continue;

                currentPageIds.RemoveAt(i);
                return true;
            }

            return false;
        }

        public bool AddPage(string pageId)
        {
            if (string.IsNullOrWhiteSpace(pageId))
                return false;

            if (page != null && !page.Contains(pageId))
            {
                Debug.LogWarning($"{name}: page id '{pageId}' is missing in CatalogPageDatabase.");
                return false;
            }

            if (ContainsPage(pageId))
                return false;

            currentPageIds.Add(pageId);
            return true;
        }

        public void ClearPages()
        {
            currentPageIds.Clear();
        }
    }
}
