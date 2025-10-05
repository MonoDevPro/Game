using Arch.LowLevel;
using Game.ECS.Resources;

namespace Game.ECS.Extensions;

public static class ResourcesExtensions
{
    public static bool TryGetNameValue(this Handle<string> handle, out string? name)
    {
        return ResourcesManager.TryGetName(handle, out name);
    }
    public static bool TryGetDescriptionValue(this Handle<string> handle, out string? description)
    {
        return ResourcesManager.TryGetDescription(handle, out description);
    }
}