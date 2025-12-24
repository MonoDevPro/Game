using Arch.Core;
using Game.Domain.Attributes.Stats.ValueObjects;
using Game.Domain.Commons.Enums;
using GameECS.Client.Combat;
using GameECS.Shared.Combat.Data;
using GameECS.Shared.Entities.Components;
using GameECS.Shared.Entities.Data;
using DamageType = GameECS.Shared.Combat.Data.DamageType;

namespace GameECS.Server.Combat.Examples;

/// <summary>
/// Exemplo de uso do módulo de combate.
/// </summary>
public static class CombatModuleExample
{
    /// <summary>
    /// Demonstra o uso básico do módulo de combate no servidor.
    /// </summary>
    public static void ServerExample()
    {
        // Cria o mundo ECS
        var world = World.Create();

        // Configuração opcional personalizada
        var config = new CombatConfig
        {
            BaseAttackCooldownTicks = 60,
            CriticalDamageMultiplier = 1.5f,
            BaseCriticalChance = 5f,
            MaxMeleeRange = 1,
            MaxRangedRange = 8,
            MaxMagicRange = 6
        };

        // Cria o módulo de combate
        using var combatModule = new ServerCombatModule(world, config);

        // Registra eventos
        combatModule.OnDamageDealt += msg =>
        {
            Console.WriteLine($"[SERVER] Dano: {msg.FinalDamage} de #{msg.AttackerId} em #{msg.TargetId}");
            if (msg.IsCritical) Console.WriteLine("  -> CRÍTICO!");
        };

        combatModule.OnEntityDeath += msg =>
        {
            Console.WriteLine($"[SERVER] Entidade #{msg.EntityId} morreu! Assassino: #{msg.KillerId}");
        };

        // Cria combatentes de diferentes vocações
        var knight = combatModule.CreateCombatant(VocationType.Warrior, level: 5);
        var mage = combatModule.CreateCombatant(VocationType.Mage, level: 5);
        var archer = combatModule.CreateCombatant(VocationType.Archer, level: 5);

        Console.WriteLine($"Knight criado: ID={knight.Id}");
        Console.WriteLine($"Mage criado: ID={mage.Id}");
        Console.WriteLine($"Archer criado: ID={archer.Id}");

        // Simula combate
        long serverTick = 0;

        // Knight ataca Mage
        combatModule.RequestAttack(knight, mage.Id, serverTick);
        combatModule.Tick(serverTick++);

        // Archer ataca Mage
        combatModule.RequestAttack(archer, mage.Id, serverTick);
        combatModule.Tick(serverTick++);

        // Avança cooldown
        for (int i = 0; i < 60; i++)
        {
            combatModule.Tick(serverTick++);
        }

        // Mage contra-ataca Knight
        combatModule.RequestAttack(mage, knight.Id, serverTick);
        combatModule.Tick(serverTick++);

        // Verifica estatísticas
        var knightStats = combatModule.GetStatistics(knight);
        var mageStats = combatModule.GetStatistics(mage);

        Console.WriteLine($"\nEstatísticas Knight: Dano={knightStats?.TotalDamageDealt}, Recebido={knightStats?.TotalDamageReceived}");
        Console.WriteLine($"Estatísticas Mage: Dano={mageStats?.TotalDamageDealt}, Recebido={mageStats?.TotalDamageReceived}");

        // Cura entidade
        int healed = combatModule.HealEntity(mage, 50);
        Console.WriteLine($"\nMage curado em {healed} pontos");

        // Verifica se estão vivos
        Console.WriteLine($"\nKnight está vivo: {combatModule.IsAlive(knight)}");
        Console.WriteLine($"Mage está vivo: {combatModule.IsAlive(mage)}");

        // Limpa recursos
        World.Destroy(world);
    }

    /// <summary>
    /// Demonstra o uso básico do módulo de combate no cliente.
    /// </summary>
    public static void ClientExample()
    {
        // Cria o mundo ECS
        var world = World.Create();

        // Cria o módulo de combate do cliente
        using var clientCombat = new ClientCombatModule(world);

        // Cria entidades visuais (sincronizadas do servidor)
        var playerEntity = clientCombat.CreateEntity(
            serverId: 1,
            vocation: VocationType.Warrior,
            health: 150,
            maxHealth: 150,
            mana: 30,
            maxMana: 30,
            isLocalPlayer: true);

        var enemyEntity = clientCombat.CreateEntity(
            serverId: 2,
            vocation: VocationType.Mage,
            health: 80,
            maxHealth: 80,
            mana: 150,
            maxMana: 150);

        Console.WriteLine($"Jogador local criado: ID={playerEntity.Id}");
        Console.WriteLine($"Inimigo criado: ID={enemyEntity.Id}");

        // Simula recebimento de dano do servidor
        clientCombat.OnDamageReceived(new DamageMessage
        {
            AttackerId = 1,
            TargetId = 2,
            FinalDamage = 25,
            Type = DamageType.Physical,
            IsCritical = false,
            Result = AttackResult.Hit
        });

        // Atualiza health sincronizado
        clientCombat.OnHealthUpdated(2, 55, 80, 101);

        // Simula loop de atualização visual
        for (int i = 0; i < 60; i++)
        {
            clientCombat.Update(1f / 60f);  // 60 FPS
        }

        // Adiciona texto flutuante de dano crítico
        clientCombat.AddFloatingDamage(2, 50, true, DamageType.Magic, 100f, 100f);

        // Inicia animação de ataque
        clientCombat.StartAttackAnimation(1, 2, VocationType.Warrior, 0.5f);

        Console.WriteLine("\nSimulação de cliente completada!");

        // Limpa recursos
        World.Destroy(world);
    }

    /// <summary>
    /// Demonstra as estatísticas de cada vocação.
    /// </summary>
    public static void ShowVocationStats()
    {
        Console.WriteLine("=== Estatísticas das Vocações ===\n");

        foreach (VocationType vocation in Enum.GetValues<VocationType>())
        {
            if (vocation == VocationType.Unknown) continue;

            var stats = Stats.GetForVocation(vocation);
            
            Console.WriteLine($"--- {vocation} ---");
            Console.WriteLine($"  Vida: {stats.BaseHealth}");
            Console.WriteLine($"  Mana: {stats.BaseMana}");
            Console.WriteLine($"  Dano Físico: {stats.BasePhysicalDamage}");
            Console.WriteLine($"  Dano Mágico: {stats.BaseMagicDamage}");
            Console.WriteLine($"  Defesa Física: {stats.BasePhysicalDefense}");
            Console.WriteLine($"  Defesa Mágica: {stats.BaseMagicDefense}");
            Console.WriteLine($"  Alcance: {stats.BaseAttackRange}");
            Console.WriteLine($"  Velocidade de Ataque: {stats.BaseAttackSpeed:F1}x");
            Console.WriteLine($"  Chance Crítico: {stats.BaseCriticalChance}%");
            Console.WriteLine($"  Custo Mana/Ataque: {stats.ManaCostPerAttack}");
            Console.WriteLine();
        }
    }
}
