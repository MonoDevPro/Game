using System.Collections.Generic;
using FluentAssertions;
using Game.Abstractions;
using Game.Core;
using Game.Domain.Entities;
using Game.Domain.Enums;
using Game.Domain.VOs;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Game.Tests.Simulation;

public class GameSimulationTests
{
    private static MapService CreateMapService()
    {
        const int width = 16;
        const int height = 16;
        var tiles = new TileType[width * height];
        for (var i = 0; i < tiles.Length; i++)
        {
            tiles[i] = TileType.Floor;
        }

        var template = new Map
        {
            Id = 1,
            Name = "TestMap",
            Width = width,
            Height = height,
            Tiles = tiles,
            CollisionMask = new byte[width * height],
            BorderBlocked = false,
            UsePadded = false
        };

        return MapService.CreateFromTemplate(template);
    }

    [Fact]
    public void AppliesInputAndReportsDirtyMovement()
    {
        var services = new ServiceCollection();
        services.AddSingleton(CreateMapService());
        var provider = services.BuildServiceProvider();

        var simulation = new GameSimulation(provider);
        var entity = simulation.SpawnPlayer(1, 42, new Coordinate(3, 3), DirectionEnum.East, new Stats());

        simulation.TryApplyPlayerInput(entity, 1, 0, 0, 1).Should().BeTrue();

        for (var i = 0; i < 20; i++)
        {
            simulation.Update(GameSimulation.FixedDeltaTime);
        }

        var states = new List<PlayerNetworkStateData>();
        simulation.CollectDirtyPlayers(states);
        states.Should().NotBeNull();
    }
}
