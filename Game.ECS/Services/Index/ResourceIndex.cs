using Arch.LowLevel;

namespace Game.ECS.Services.Index;

/// <summary>
/// Gerencia o ciclo de vida de recursos unmanaged/managed associados a entidades.
/// </summary>
/// <typeparam name="T">O tipo do recurso (ex: string, NpcPath, AIState)</typeparam>
public class ResourceIndex<T>(int capacity = 50)
    where T : notnull
{
    // O banco de dados de baixo nível do Arch
    private readonly Resources<T> _repository = new(capacity);
    
    // Cache para evitar duplicação (Interning)
    // Mapeia Valor -> Handle existente
    private readonly Dictionary<T, Handle<T>> _cache = new();
    
    // Contagem de referências (Opcional, mas útil se recursos são compartilhados)
    private readonly Dictionary<int, int> _refCount = new();

    /// <summary>
    /// Registra um recurso e retorna um Handle.
    /// Se o recurso já existe (Equals), retorna o Handle existente.
    /// </summary>
    public Handle<T> Register(T item)
    {
        if (_cache.TryGetValue(item, out var handle))
        {
            // Incrementa ref count se estiver usando sistema compartilhado
            if (_refCount.TryGetValue(handle.Id, out int value)) _refCount[handle.Id] = ++value;
            return handle;
        }

        handle = _repository.Add(item);
        _cache[item] = handle;
        _refCount[handle.Id] = 1;
        
        return handle;
    }

    /// <summary>
    /// Remove uma referência ao recurso. Se a contagem chegar a zero, libera a memória.
    /// </summary>
    public void Unregister(Handle<T> handle)
    {
        if (!_repository.IsValid(handle)) return;

        // Decrementa Ref Count
        if (--_refCount[handle.Id] > 0) return; // Ainda tem gente usando

        // Ninguém mais usa, pode apagar de verdade
        ref T item = ref _repository.Get(handle);
        _cache.Remove(item); // Remove do cache de busca
        _refCount.Remove(handle.Id);
        _repository.Remove(handle); // Libera slot no array
    }

    /// <summary>
    /// Acesso direto de alta performance ao valor.
    /// </summary>
    public ref T Get(in Handle<T> handle)
    {
        return ref _repository.Get(in handle);
    }
}