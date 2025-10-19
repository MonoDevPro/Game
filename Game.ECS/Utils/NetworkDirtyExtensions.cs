using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Utils;

public static class NetworkDirtyExtensions
{
    /// <summary>
    /// Marca entidade como dirty para sincronização (adiciona flags).
    /// Só chama world.Set se houver alteração.
    /// </summary>
    public static void MarkNetworkDirty(this World world, Entity e, SyncFlags f)
    {
        if (!world.IsAlive(e)) return;
        
        if (world.TryGet(e, out NetworkDirty dirty))
        {
            // só atualiza se houver diferença
            if ((dirty.Flags & f) != f)
            {
                dirty.Flags |= f;
                world.Set(e, dirty);
            }
        }
        else
        {
            // Inicializa LastSyncTick conforme sua lógica (0 == nunca sincronizado)
            world.Add(e, new NetworkDirty
            {
                Flags = f
            });
        }
    }

    /// <summary>
    /// Adiciona flags diretamente na struct (por ref) e retorna se houve alteração.
    /// Útil para chamar antes de world.Set: if (dirty.AddFlags(...)) world.Set(entity, dirty);
    /// </summary>
    public static bool AddFlags(this ref NetworkDirty dirty, SyncFlags flags)
    {
        SyncFlags before = dirty.Flags;
        dirty.Flags |= flags;
        return dirty.Flags != before;
    }

    /// <summary>
    /// Remove flags. Se ficar sem flags, remove o componente do World (se existir).
    /// </summary>
    public static void ClearNetworkDirty(this World world, Entity e, SyncFlags f)
    {
        if (!world.IsAlive(e)) return;
        
        if (world.TryGet(e, out NetworkDirty dirty))
        {
            SyncFlags before = dirty.Flags;
            dirty.Flags &= ~f;

            if (dirty.Flags == 0)
            {
                world.Remove<NetworkDirty>(e);
            }
            else if (dirty.Flags != before)
            {
                // só Set se realmente mudou
                world.Set(e, dirty);
            }
        }
    }

    /// <summary>
    /// Remove flags na struct (por ref). Retorna true se o componente ficou vazio (flags==0).
    /// </summary>
    public static bool RemoveFlags(this ref NetworkDirty dirty, SyncFlags flags)
    {
        dirty.Flags &= ~flags;
        return dirty.Flags == 0;
    }

    /// <summary>
    /// Verifica se entidade está dirty para as flags informadas.
    /// </summary>
    public static bool IsNetworkDirty(this World world, Entity e, SyncFlags flags = SyncFlags.All)
    {
        if (world.TryGet(e, out NetworkDirty dirty))
        {
            return (dirty.Flags & flags) != 0;
        }
        return false;
    }

    /// <summary>
    /// Verifica flags na struct.
    /// </summary>
    public static bool HasFlags(this in NetworkDirty dirty, SyncFlags flags = SyncFlags.All)
    {
        return (dirty.Flags & flags) != 0;
    }
}
