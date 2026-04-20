using System;
using Enums;

namespace DB
{
    [Serializable]
    public struct CatalogPageData
    {
        public CatalogPageKind PageKind;
        public string PageId;
        public string SourceDrawerId;

        public bool IsValid()
        {
            return !string.IsNullOrWhiteSpace(PageId);
        }
        
        public CatalogPageData(CatalogPageKind pageKind, string pageId, string sourceDrawerId)
        {
            PageKind = pageKind;
            PageId = pageId;
            SourceDrawerId = sourceDrawerId;
        }
    }
}