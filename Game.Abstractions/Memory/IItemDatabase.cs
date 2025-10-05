using Game.Domain.Enums;

namespace Game.Abstractions.Memory
{
    /// <summary>
    /// Contrato para um repositório/cache em memória de dados de itens.
    /// Implementações devem ser seguras para uso concorrente e expor operações
    /// para inicialização, lookup por GUID, refresh e gerenciamento do cache.
    /// </summary>
    public interface IItemDatabase
    {
        /// <summary>
        /// Inicializa o cache carregando dados do banco.
        /// Deve ser chamado na inicialização do host/servidor.
        /// </summary>
        Task InitializeAsync();
        
        bool TryGetItem(Guid itemGuid, out ItemData? item);

        /// <summary>
        /// Obtém um item pelo seu <see cref="Guid"/>.
        /// Retorna <c>null</c> se o item não existir.
        /// </summary>
        /// <param name="itemGuid">GUID do item.</param>
        Task<ItemData?> GetItemAsync(Guid itemGuid);

        /// <summary>
        /// Retorna um snapshot de todos os itens atualmente em cache.
        /// </summary>
        Task<ItemData[]> GetAllItemsAsync();

        /// <summary>
        /// Retorna todos os itens do cache filtrados por tipo.
        /// </summary>
        /// <param name="itemType">Tipo de item.</param>
        Task<ItemData[]> GetItemsByTypeAsync(ItemType itemType);

        /// <summary>
        /// Recarrega todo o cache a partir da fonte de dados (database).
        /// </summary>
        Task RefreshCacheAsync();

        /// <summary>
        /// Limpa o cache em memória imediatamente.
        /// </summary>
        void ClearCache();

        /// <summary>
        /// Retorna a quantidade de itens atualmente indexados em cache.
        /// </summary>
        int GetCachedItemCount();

        /// <summary>
        /// Verifica se um item com o GUID informado existe no índice em memória.
        /// </summary>
        /// <param name="itemGuid">GUID do item.</param>
        bool Exists(Guid itemGuid);
    }
}