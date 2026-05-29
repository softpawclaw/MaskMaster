using Enums;
using UnityEngine;

namespace Interactable.MaskWorkbench
{
    public readonly struct MaskMiniGameRequest
    {
        public readonly string ConfigId;
        public readonly Transform WorldAnchor;
        public readonly MaskMiniGameKind Kind;
        public readonly MaskSegment Segment;
        public readonly ResourceType ResourceType;

        public MaskMiniGameRequest(
            MaskMiniGameKind kind,
            MaskSegment segment = MaskSegment.Middle,
            ResourceType resourceType = ResourceType.None,
            string configId = null,
            Transform worldAnchor = null)
        {
            ConfigId = configId;
            WorldAnchor = worldAnchor;
            Kind = kind;
            Segment = segment;
            ResourceType = resourceType;
        }

        public MaskMiniGameRequest WithConfigAndAnchor(string configId, Transform worldAnchor)
        {
            return new MaskMiniGameRequest(Kind, Segment, ResourceType, configId, worldAnchor);
        }
    }
}
