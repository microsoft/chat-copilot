// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace CopilotChat.WebApi.Storage;

/// <summary>
/// A storage context that stores entities in memory.
/// </summary>
[DebuggerDisplay($"{{{nameof(GetDebuggerDisplay)}(),nq}}")]
public class VolatileContext<T> : IStorageContext<T> where T : IStorageEntity
{
    /// <summary>
    /// Using a concurrent dictionary to store entities in memory.
    /// </summary>
    private readonly ConcurrentDictionary<string, T> _entities;

    /// <summary>
    /// Initializes a new instance of the InMemoryContext class.
    /// </summary>
    public VolatileContext()
    {
        this._entities = new ConcurrentDictionary<string, T>();
    }

    /// <inheritdoc/>
    public Task<IEnumerable<T>> QueryEntitiesAsync(Func<T, bool> predicate)
    {
        return Task.FromResult(this._entities.Values.Where(predicate));
    }

    /// <inheritdoc/>
    public Task CreateAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new ArgumentException("Entity Id cannot be null or empty.", nameof(entity));
        }

        this._entities.TryAdd(entity.Id, entity);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task DeleteAsync(T entity)
    {
        return this.DeleteAsync(entity.Id, entity.Partition);
    }

    /// <inheritdoc/>
    public Task DeleteAsync(string entityId, string partitionKey)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentNullException(nameof(entityId), "Entity Id cannot be null or empty.");
        }

        this._entities.TryRemove(entityId, out _);

        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task<T> ReadAsync(string entityId, string partitionKey)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentNullException(nameof(entityId), "Entity Id cannot be null or empty.");
        }

        if (this._entities.TryGetValue(entityId, out T? entity))
        {
            return Task.FromResult(entity);
        }

        throw new KeyNotFoundException($"Entity with id {entityId} not found.");
    }

    /// <inheritdoc/>
    public Task UpsertAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new ArgumentException("Entity Id cannot be null or empty.", nameof(entity));
        }

        this._entities.AddOrUpdate(entity.Id, entity, (key, oldValue) => entity);

        return Task.CompletedTask;
    }

    private string GetDebuggerDisplay()
    {
        return this.ToString() ?? string.Empty;
    }
}
