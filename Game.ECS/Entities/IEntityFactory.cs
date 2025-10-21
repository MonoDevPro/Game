using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Entities.Data;

namespace Game.ECS.Entities;

public interface IEntityFactory
{
    Entity CreatePlayer(in PlayerCharacter data);
    Entity CreateRemotePlayer(in PlayerCharacter data);
    Entity CreateLocalPlayer(in PlayerCharacter data);
}