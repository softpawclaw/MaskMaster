using Enums;

namespace Helpers
{
    public static class ResourceTypeHelper
    {
        public static bool IsBlank(ResourceType type)
        {
            return type == ResourceType.WhiteWood
                   || type == ResourceType.RedWood
                   || type == ResourceType.BlackWood;
        }

        public static bool IsInlay(ResourceType type)
        {
            return type == ResourceType.Rivet
                   || type == ResourceType.Chain
                   || type == ResourceType.Fang
                   || type == ResourceType.LeatherPatch
                   || type == ResourceType.Hook;
        }
    }
}