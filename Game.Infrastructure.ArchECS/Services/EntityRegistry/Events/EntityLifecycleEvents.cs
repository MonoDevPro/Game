using Arch.Core;
using Game.Infrastructure.ArchECS.Services.Navigation.Components;

namespace Game.Infrastructure.ArchECS.Services.EntityRegistry.Events;

public readonly record struct EntityRegisteredEvent(Entity Entity, EntityMetadata Metadata);

public readonly record struct EntityUnregisteredEvent(Entity Entity, EntityMetadata Metadata);