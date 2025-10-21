using Game.ECS.Components;
using Game.ECS.Examples;

namespace Game.ECS.Examples;

/// <summary>
/// Exemplo prático de como usar o ECS.
/// Demonstra:
/// - Setup de servidor
/// - Setup de cliente
/// - Sincronização básica
/// - Renderização de estado
/// </summary>
public static class ECSUsageExample
{
    public static void RunServerExample()
    {
        Console.WriteLine("=== SERVER EXAMPLE ===\n");
        
        var server = new ServerGameSimulation();
        
        // Registra novo jogador
        server.RegisterNewPlayer(playerId: 1, networkId: 1);
        server.RegisterNewPlayer(playerId: 2, networkId: 2);
        
        // Spawna um NPC
        server.SpawnNPC("Goblin", npcId: 100, x: 60, y: 50);
        
        // Simula alguns ticks
        for (int i = 0; i < 5; i++)
        {
            Console.WriteLine($"\n--- TICK {server.CurrentTick} ---");
            
            // Aplica input de cliente 1
            server.ApplyPlayerInput(networkId: 1, inputX: 1, inputY: 0, flags: InputFlags.None);
            
            // Atualiza simulação
            server.Update(deltaTime: 1f / 60f);
        }
    }

    public static void RunClientExample()
    {
        Console.WriteLine("\n=== CLIENT EXAMPLE ===\n");
        
        var client = new ClientGameSimulation();
        
        // Spawn jogador local
        client.SpawnLocalPlayer(playerId: 1, networkId: 1);
        
        // Spawn outro jogador remoto
        client.SpawnRemotePlayer(networkId: 2, x: 60, y: 50);
        
        // Simula alguns ticks
        for (int i = 0; i < 5; i++)
        {
            Console.WriteLine($"\n--- CLIENT TICK {client.CurrentTick} ---");
            
            // Input do jogador local
            client.HandlePlayerInput(inputX: 1, inputY: 0, flags: InputFlags.Sprint);
            
            // Atualiza simulação
            client.Update(deltaTime: 1f / 60f);
        }
    }

    public static void RunFullSyncExample()
    {
        Console.WriteLine("\n=== FULL SYNC EXAMPLE (Server + Client) ===\n");
        
        var server = new ServerGameSimulation();
        var client = new ClientGameSimulation();
        
        // Setup inicial
        server.RegisterNewPlayer(playerId: 1, networkId: 1);
        client.SpawnLocalPlayer(playerId: 1, networkId: 1);
        
        Console.WriteLine("\n--- TICK 1 ---");
        
        // Cliente envia input
        client.HandlePlayerInput(inputX: 1, inputY: 0, flags: InputFlags.None);
        
        // Servidor processa
        server.Update(deltaTime: 1f / 60f);
        
        // Cliente atualiza localmente
        client.Update(deltaTime: 1f / 60f);
        
        Console.WriteLine("\n--- TICK 2 ---");
        
        // Mais input
        client.HandlePlayerInput(inputX: 1, inputY: 1, flags: InputFlags.Sprint);
        
        server.Update(deltaTime: 1f / 60f);
        client.Update(deltaTime: 1f / 60f);
    }

    public static void RunCombatExample()
    {
        Console.WriteLine("\n=== COMBAT EXAMPLE ===\n");
        
        var server = new ServerGameSimulation();
        
        // Registra jogador
        server.RegisterNewPlayer(playerId: 1, networkId: 1);
        
        // Spawna NPC inimigo
        server.SpawnNPC("Skeleton", npcId: 100, x: 55, y: 50);
        
        Console.WriteLine("Iniciando combate...");
        
        // Simula ticks
        for (int i = 0; i < 10; i++)
        {
            Console.WriteLine($"\n--- TICK {server.CurrentTick} ---");
            server.Update(deltaTime: 1f / 60f);
        }
    }
}

/// <summary>
/// Entry point para testar o ECS.
/// </summary>
public static class Program
{
    public static void Main(string[] args)
    {
        try
        {
            // Descomenta o exemplo que deseja rodar
            
            // ECSUsageExample.RunServerExample();
            // ECSUsageExample.RunClientExample();
            // ECSUsageExample.RunFullSyncExample();
            // ECSUsageExample.RunCombatExample();
            
            Console.WriteLine("ECS Exemplos - Descomente a linha desejada em Main()");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Erro: {ex}");
        }
    }
}
