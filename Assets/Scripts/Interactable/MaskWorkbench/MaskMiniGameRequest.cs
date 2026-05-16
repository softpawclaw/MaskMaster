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
        public readonly MaskSocket Socket;

        public MaskMiniGameRequest(
            MaskMiniGameKind kind,
            MaskSegment segment = MaskSegment.Middle,
            ResourceType resourceType = ResourceType.None,
            MaskSocket socket = MaskSocket.None,
            string configId = null,
            Transform worldAnchor = null)
        {
            ConfigId = configId;
            WorldAnchor = worldAnchor;
            Kind = kind;
            Segment = segment;
            ResourceType = resourceType;
            Socket = socket;
        }

        public MaskMiniGameRequest WithConfigAndAnchor(string configId, Transform worldAnchor)
        {
            return new MaskMiniGameRequest(Kind, Segment, ResourceType, Socket, configId, worldAnchor);
        }
    }
}
