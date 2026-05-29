using System;
using Enums;
using Interactable.MaskWorkbench;
using UnityEngine;

namespace DB
{
    public class DBFaceCover : MonoBehaviour
    {
        [Serializable]
        public struct FaceCoverData
        {
            public string Id;
            public string RecipeName;
            public string ProductionName;
            public Sprite Image;
            public MaskSize MaskSize;
            public MaskSegment[] Segments;
        }

        [SerializeField] private FaceCoverData[] config;

        public bool TryGetData(string id, out FaceCoverData result)
        {
            result = default;

            if (string.IsNullOrEmpty(id) || config == null || config.Length == 0)
                return false;

            for (int i = 0; i < config.Length; i++)
            {
                if (config[i].Id == id)
                {
                    result = config[i];
                    return true;
                }
            }

            return false;
        }

        public bool TryGetRecipeName(string id, out string value)
        {
            value = string.Empty;
            if (!TryGetData(id, out var data))
                return false;

            value = data.RecipeName;
            return true;
        }

        public MaskSegment[] ResolveSegments(string id)
        {
            if (TryGetData(id, out var data))
            {
                return ResolveSegments(data);
            }

            return System.Array.Empty<MaskSegment>();
        }

        public static MaskSegment[] ResolveSegments(FaceCoverData data)
        {
            if (data.Segments != null && data.Segments.Length > 0)
                return (MaskSegment[])data.Segments.Clone();

            return ResolveSegmentsFromSize(data.MaskSize);
        }

        public static MaskSegment[] ResolveSegmentsFromSize(MaskSize size)
        {
            switch (size)
            {
                case MaskSize.Small:
                    return new[] { MaskSegment.Middle };

                case MaskSize.Medium:
                    return new[] { MaskSegment.Upper, MaskSegment.Middle };

                case MaskSize.Large:
                    return new[] { MaskSegment.Upper, MaskSegment.Middle, MaskSegment.Lower };

                default:
                    return System.Array.Empty<MaskSegment>();
            }
        }

        public FaceCoverData[] GetAll()
        {
            return config != null ? ( FaceCoverData[] )config.Clone() : System.Array.Empty<FaceCoverData>();
        }
    }
}
