using System.Linq.Expressions;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using GameWeb.Application.Common;
using GameWeb.Application.Common.Mapping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace GameWeb.Infrastructure.Data
{
    /// <summary>
    /// Implementação base de repositório usando DbContext/DbSet direto.
    /// </summary>
    public abstract class EfRepoBase<TEntity>(DbContext db, IMapper mapper)
        where TEntity : class
    {
        protected readonly DbContext _db = db;
        protected readonly DbSet<TEntity> _dbSet = db.Set<TEntity>();
        protected readonly IMapper _mapper = mapper;

        public IQueryable<TEntity> Query(bool asNoTracking = true)
        {
            var q = _dbSet.AsQueryable();
            return asNoTracking ? q.AsNoTracking() : q;
        } 
        
        #region Métodos de Leitura (Entidade)

        public async Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> predicate, bool asNoTracking = true, CancellationToken ct = default, params Expression<Func<TEntity, object>>[] includes)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            IQueryable<TEntity> q = _dbSet;
            if (asNoTracking) q = q.AsNoTracking();
            if (includes.Length > 0)
                q = includes.Aggregate(q, (current, inc) => current.Include(inc));
            return await q.FirstOrDefaultAsync(predicate, ct).ConfigureAwait(false);
        }

        public async Task<List<TEntity>> ListAsync(Expression<Func<TEntity, bool>>? predicate = null, bool asNoTracking = true, CancellationToken ct = default, params Expression<Func<TEntity, object>>[] includes)
        {
            IQueryable<TEntity> q = _dbSet;
            if (asNoTracking) q = q.AsNoTracking();
            if (includes.Length > 0)
                q = includes.Aggregate(q, (current, inc) => current.Include(inc));
            if (predicate != null) q = q.Where(predicate);
            return await q.ToListAsync(ct).ConfigureAwait(false);
        }

        #endregion

        #region Métodos de Leitura com Projeção (DTOs)

        public async Task<PaginatedList<TProjected>> GetPagedAsync<TProjected>(
            int page, 
            int pageSize, 
            Expression<Func<TEntity, bool>>? predicate = null, 
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, 
            bool asNoTracking = true, 
            CancellationToken ct = default, 
            params Expression<Func<TEntity, object>>[] includes)
        {
            if (page <= 0) throw new ArgumentOutOfRangeException(nameof(page));
            if (pageSize <= 0) throw new ArgumentOutOfRangeException(nameof(pageSize));

            IQueryable<TEntity> q = _dbSet;
            if (asNoTracking) q = q.AsNoTracking();
            
            // Includes são geralmente desnecessários com ProjectTo, mas podem ser úteis para lógicas complexas de filtro ou ordenação.
            if (includes.Length > 0)
                q = includes.Aggregate(q, (current, inc) => current.Include(inc));
            
            if (predicate != null) q = q.Where(predicate);

            if (orderBy != null)
                q = orderBy(q);
            
            var projectedQuery = q.ProjectTo<TProjected>(_mapper.ConfigurationProvider);
            
            return await PaginatedList<TProjected>.CreateAsync(projectedQuery, page, pageSize, ct).ConfigureAwait(false);
        }

        /// <summary>
        /// OBTÉM UM ÚNICO RESULTADO PROJETADO.
        /// </summary>
        public async Task<TProjected?> GetProjectedAsync<TProjected>(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default) where TProjected : class
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            
            return await _dbSet
                .FirstOrDefaultAsync(predicate, ct)
                .ContinueWith(t => t.Result == null ? null : _mapper.Map<TProjected>(t.Result), ct)
                .ConfigureAwait(false);
        }

        /// <summary>
        /// OBTÉM UMA LISTA COMPLETA PROJETADA.
        /// </summary>
        public async Task<List<TProjected>> ListProjectedAsync<TProjected>(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default) where TProjected : class
        {
            IQueryable<TEntity> q = _dbSet;
            if (predicate != null) q = q.Where(predicate);

            return await q.ProjectToListAsync<TProjected>(_mapper, cancellationToken: ct).ConfigureAwait(false);
        }

        #endregion
        
        public async Task<TEntity> FindByKeyAsync(object[] keyValues, CancellationToken ct = default)
        {
            if (keyValues == null || keyValues.Length == 0) throw new ArgumentNullException(nameof(keyValues));
            var found = await _dbSet.FindAsync(keyValues, ct).ConfigureAwait(false);
            return found ?? throw new InvalidOperationException("Entity not found with given key values.");
        }

        public async Task AddAsync(TEntity entity, CancellationToken ct = default)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            await _dbSet.AddAsync(entity, ct).ConfigureAwait(false);
        }

        public async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            await _dbSet.AddRangeAsync(entities, ct).ConfigureAwait(false);
        }

        public void Update(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            _dbSet.Update(entity);
        }

        public void UpdateRange(IEnumerable<TEntity> entities)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            _dbSet.UpdateRange(entities);
        }

        public void Remove(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            _dbSet.Remove(entity);
        }

        public void RemoveRange(IEnumerable<TEntity> entities)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            _dbSet.RemoveRange(entities);
        }

        public async Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default)
        {
            if (predicate == null) return await _dbSet.AnyAsync(ct).ConfigureAwait(false);
            return await _dbSet.AnyAsync(predicate, ct).ConfigureAwait(false);
        }

        public async Task<long> CountAsync(Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default)
        {
            if (predicate == null) return await _dbSet.LongCountAsync(ct).ConfigureAwait(false);
            return await _dbSet.LongCountAsync(predicate, ct).ConfigureAwait(false);
        }

        public async Task<int> SaveChangesAsync(CancellationToken ct = default)
        {
            return await _db.SaveChangesAsync(ct).ConfigureAwait(false);
        }

        public async Task<int> ExecuteSqlRawAsync(string sql, object[] parameters, CancellationToken ct = default)
        {
            return await _db.Database.ExecuteSqlRawAsync(sql, parameters, ct).ConfigureAwait(false);
        }

        public async Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken ct = default)
        {
            return await _db.Database.BeginTransactionAsync(ct).ConfigureAwait(false);
        }
    }
}
