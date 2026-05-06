using Enums;

namespace Interactable.MaskWorkbench
{
    public readonly struct MaskMiniGameRequest
    {
        public readonly MaskMiniGameKind Kind;
        public readonly MaskSegment Segment;
        public readonly ResourceType ResourceType;
        public readonly MaskSocket Socket;

        public MaskMiniGameRequest(
            MaskMiniGameKind kind,
            MaskSegment segment = MaskSegment.Middle,
            ResourceType resourceType = ResourceType.None,
            MaskSocket socket = MaskSocket.None)
        {
            Kind = kind;
            Segment = segment;
            ResourceType = resourceType;
            Socket = socket;
        }
    }
}
