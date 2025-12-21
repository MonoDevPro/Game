
using Arch.Core;
using Game.ECS.Shared.Components.Entities;

namespace Game.ECS.Shared.Services.Entities;

/// <summary>
/// Gerador de IDs únicos para entidades de rede.
/// </summary>
public class IdGenerator
{
    private int _nextId = 1;
    private readonly Queue<int> _freeIds = new();

    /// <summary>
    /// Gera o próximo ID disponível.
    /// </summary>
    public int Next() => _freeIds.TryDequeue(out var id) ? id : _nextId++;

    /// <summary>
    /// Devolve um ID para reciclagem.
    /// </summary>
    public void Recycle(int id)
    {
        if (id > 0)
            _freeIds. Enqueue(id);
    }

    /// <summary>
    /// Retorna o próximo ID que seria gerado (sem consumir).
    /// </summary>
    public int Peek() => _freeIds.TryPeek(out var id) ? id : _nextId;

    /// <summary>
    /// Quantidade de IDs reciclados disponíveis. 
    /// </summary>
    public int RecycledCount => _freeIds. Count;
}

/// <summary>
/// Registro centralizado que combina geração de IDs com índice bidirecional.
/// Gerencia o ciclo de vida completo:  criar ID -> registrar Entity -> lookup reverso -> reciclar. 
/// </summary>
public class EntityRegistry(World world, IdGenerator idGenerator, EntityIndex<int> index)
{
    public EntityRegistry(World world) : this(world, new IdGenerator(), new EntityIndex<int>())
    {
    }

    /// <summary>
    /// Quantidade de entidades registradas. 
    /// </summary>
    public int Count => index.Count;

    /// <summary>
    /// Gera um novo ID único e registra a entidade. 
    /// Retorna o NetworkId gerado. 
    /// </summary>
    public int Register(Entity entity)
    {
        int uniqueID = idGenerator.Next();
        index.Register(uniqueID, entity);
        world.Add<UniqueID>(entity, new UniqueID { Value = uniqueID });
        return uniqueID;
    }

    /// <summary>
    /// Registra uma entidade com um ID específico (útil para sync do servidor).
    /// </summary>
    public bool RegisterWithId(int uniqueID, Entity entity)
    {
        if (!index.TryRegisterUnique(uniqueID, entity)) 
            return false;
        
        world.Add<UniqueID>(entity, new UniqueID { Value = uniqueID });
        return true;
    }

    /// <summary>
    /// Remove a entidade pelo NetworkId e recicla o ID.
    /// </summary>
    public bool Unregister(int uniqueID)
    {
        if (index. TryGetEntity(uniqueID, out Entity e))
        {
            index.RemoveByKey(uniqueID);
            idGenerator.Recycle(uniqueID);
            world.Remove<UniqueID>(e);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Remove a entidade e recicla seu ID.
    /// </summary>
    public bool Unregister(Entity entity)
    {
        if (index.TryGetKeyByEntity(entity, out var uniqueID) && uniqueID > 0)
        {
            index.RemoveByEntity(entity);
            idGenerator.Recycle(uniqueID);
            world.Remove<UniqueID>(entity);
            return true;
        }
        return false;
    }

    /// <summary>
    /// Obtém a Entity pelo NetworkId. 
    /// </summary>
    public bool TryGetEntity(int uniqueID, out Entity entity)
    {
        return index.TryGetEntity(uniqueID, out entity);
    }

    /// <summary>
    /// Obtém o NetworkId pela Entity. 
    /// </summary>
    public bool TryGetNetworkId(Arch.Core.Entity entity, out int uniqueID)
    {
        if (index.TryGetKeyByEntity(entity, out var id) && id > 0)
        {
            uniqueID = id;
            return true;
        }
        uniqueID = 0;
        return false;
    }

    /// <summary>
    /// Atualiza a Entity associada a um NetworkId (útil após recreate no Arch).
    /// </summary>
    public bool UpdateEntity(int uniqueID, Arch.Core.Entity newEntity)
    {
        return index.TryUpdateEntity(uniqueID, newEntity);
    }

    /// <summary>
    /// Verifica se um NetworkId está registrado. 
    /// </summary>
    public bool Contains(int uniqueID) => index.TryGetEntity(uniqueID, out _);

    /// <summary>
    /// Verifica se uma Entity está registrada. 
    /// </summary>
    public bool Contains(Arch.Core.Entity entity) => index.TryGetKeyByEntity(entity, out _);

    /// <summary>
    /// Limpa todo o registro (não recicla IDs, reseta tudo).
    /// </summary>
    public void Clear() => index.Clear();

    /// <summary>
    /// Snapshot somente leitura do estado atual.
    /// </summary>
    public IReadOnlyDictionary<int, Arch.Core.Entity> Snapshot() => index.Snapshot();
}