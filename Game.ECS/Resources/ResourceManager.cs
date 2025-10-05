using Arch.LowLevel;
using Game.ECS.Components;

namespace Game.ECS.Resources;

public static class ResourcesManager
{
    private static readonly Resources<string> Names = new();
    private static readonly Resources<string> Descriptions = new();
    
    public static Name CreateName(string name)
    {
        var handle = Names.Add(name);
        return new Name { Value = handle };
    }
    public static Description CreateDescription(string description)
    {
        var handle = Descriptions.Add(description);
        return new Description { Value = handle };
    }
    
    public static void RemoveName(Name name)
    {
        if (Names.IsValid(name.Value))
            Names.Remove(name.Value);
    }
    public static void RemoveDescription(Description description)
    {
        if (Descriptions.IsValid(description.Value))
            Descriptions.Remove(description.Value);
    }
    
    public static bool TryGetName(Handle<string> handle, out string? name)
    {
        if (Names.IsValid(handle))
        {
            name = Names.Get(handle);
            return true;
        }
        name = null;
        return false;
    }
    public static bool TryGetDescription(Handle<string> handle, out string? description)
    {
        if (Descriptions.IsValid(handle))
        {
            description = Descriptions.Get(handle);
            return true;
        }
        description = null;
        return false;
    }
}