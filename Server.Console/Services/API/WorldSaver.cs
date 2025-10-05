using GameWeb.Application.Players.Models;
using Simulation.Core.Ports.ECS;

namespace Server.Console.Services.API;

public class WorldSaver : IWorldSaver
{
    public void StageSave(PlayerDto dto)
    {
        // Implementar a lógica de salvamento do mundo aqui
        System.Console.WriteLine($"Salvando dados do jogador: {dto.Id}");
    }
}