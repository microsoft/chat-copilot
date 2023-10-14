using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace CopilotChat.WebApi.Storage
{
    public class MySqlStorageContext<T> : IStorageContext<T>, IDisposable where T : class, IStorageEntity
    {
        private readonly MySqlDbContext _context;
        private readonly DbSet<T> _dbSet;

        public MySqlStorageContext(MySqlDbContext context)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _dbSet = _context.Set<T>();
        }

        private void ValidateEntityId(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                throw new ArgumentOutOfRangeException(nameof(id), "Entity Id cannot be null or empty.");
            }
        }

        public async Task<IEnumerable<T>> QueryEntitiesAsync(Func<T, bool> predicate)
        {
            if (predicate == null)
            {
                throw new ArgumentNullException(nameof(predicate));
            }

            return await Task.Run(() => _dbSet.Where(predicate).ToList()).ConfigureAwait(false);
        }

        public async Task CreateAsync(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            ValidateEntityId(entity.Id);

            _dbSet.Add(entity);
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task DeleteAsync(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            ValidateEntityId(entity.Id);

            _dbSet.Remove(entity);
            await _context.SaveChangesAsync().ConfigureAwait(false);
        }

        public async Task<T> ReadAsync(string entityId, string partitionKey)
        {
            ValidateEntityId(entityId);

            var entity = await _dbSet.FindAsync(entityId).ConfigureAwait(false);
            if (entity == null)
            {
                throw new KeyNotFoundException($"Entity with id {entityId} not found.");
            }

            return entity;
        }

        public async Task UpsertAsync(T entity)
        {
            if (entity == null)
            {
                throw new ArgumentNullException(nameof(entity));
            }

            ValidateEntityId(entity.Id);

            var existingEntity = await _dbSet.FindAsync(entity.Id).ConfigureAwait(false);
            if (existingEntity != null)
            {
                _context.Entry(entity).State = EntityState.Modified;
            }
            else
            {
                _dbSet.Add(entity);
            }

            await _context.SaveChangesAsync().ConfigureAwait(false);
        }

        public void Dispose() => _context.Dispose();
    }
}