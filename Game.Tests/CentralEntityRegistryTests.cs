using System.Linq;
using Arch.Core;
using FluentAssertions;
using Game.Infrastructure.ArchECS.Services.EntityRegistry;
using Xunit;

namespace Game.Tests;

[Collection(nameof(EcsCollection))]
public class CentralEntityRegistryTests
{
    [Fact]
    public void GetEntitiesByDomain_WithCombinedFlags_ReturnsUnionWithoutDuplicates()
    {
        using var world = World.Create();
        using var registry = new CentralEntityRegistry();

        var multiDomainEntity = world.Create();
        var navigationOnlyEntity = world.Create();

        registry.RegisterMultiDomain(1, multiDomainEntity, EntityDomain.Combat | EntityDomain.Navigation);
        registry.Register(2, navigationOnlyEntity, EntityDomain.Navigation);

        var result = registry.GetEntitiesByDomain(EntityDomain.Combat | EntityDomain.Navigation).ToList();

        result.Should().Contain(multiDomainEntity);
        result.Should().Contain(navigationOnlyEntity);
        result.Should().HaveCount(2);
    }

    [Fact]
    public void GetEntitiesByDomain_WithNone_ReturnsEmpty()
    {
        using var registry = new CentralEntityRegistry();

        registry.GetEntitiesByDomain(EntityDomain.None).Should().BeEmpty();
    }
}
