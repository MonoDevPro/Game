using Arch.Core;
using FluentAssertions;
using Game.Infrastructure.ArchECS.Services.Navigation;
using Game.Infrastructure.ArchECS.Services.Navigation.Components;
using Game.Infrastructure.ArchECS.Services.Navigation.Map;
using Xunit;

namespace Game.Tests;

public class NavDirectionalMovementSystemTests
{
    [Fact]
    public void RequestDirectionalMove_Twice_MovesEntityTwice()
    {
        using var world = World.Create();
        using var navigation = new NavigationModule(world, new WorldMap(1, "TestMap", 20, 20));

        var entity = world.Create();
        navigation.AddNavigationComponents(
            entity,
            x: 5,
            y: 5,
            dirX: 0,
            dirY: 1,
            floor: 0,
            settings: new NavAgentSettings
            {
                MoveDurationTicks = 1,
                DiagonalDurationTicks = 1,
                AllowDiagonal = true,
                MaxPathRetries = 1
            });

        navigation.RequestDirectionalMove(entity, new Direction { X = 1, Y = 0 });
        navigation.Tick(serverTick: 0);
        navigation.Tick(serverTick: 1);

        world.Get<Position>(entity).X.Should().Be(6);

        navigation.RequestDirectionalMove(entity, new Direction { X = 1, Y = 0 });
        navigation.Tick(serverTick: 2);
        navigation.Tick(serverTick: 3);

        world.Get<Position>(entity).X.Should().Be(7);
        world.Has<NavDirectionalMode>(entity).Should().BeFalse();
    }
}
