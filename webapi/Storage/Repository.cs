// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CopilotChat.WebApi.Models.Storage;

namespace CopilotChat.WebApi.Storage;

/// <summary>
/// Defines the basic CRUD operations for a repository.
/// </summary>
public class Repository<T> : IRepository<T> where T : IStorageEntity
{
    /// <summary>
    /// The storage context.
    /// </summary>
    protected IStorageContext<T> StorageContext { get; set; }

    /// <summary>
    /// Initializes a new instance of the Repository class.
    /// </summary>
    public Repository(IStorageContext<T> storageContext)
    {
        this.StorageContext = storageContext;
    }

    /// <inheritdoc/>
    public Task CreateAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new ArgumentOutOfRangeException(nameof(entity), "Entity ID cannot be null or empty.");
        }

        return this.StorageContext.CreateAsync(entity);
    }

    /// <inheritdoc/>
    public Task DeleteAsync(T entity)
    {
        return this.StorageContext.DeleteAsync(entity);
    }

    /// <inheritdoc/>
    public Task<T> FindByIdAsync(string id, string? partition = null)
    {
        return this.StorageContext.ReadAsync(id, partition ?? id);
    }

    /// <inheritdoc/>
    public async Task<bool> TryFindByIdAsync(string id, string? partition = null, Action<T?>? callback = null)
    {
        try
        {
            T? found = await this.FindByIdAsync(id, partition ?? id);

            callback?.Invoke(found);

            return true;
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException || ex is KeyNotFoundException)
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public Task UpsertAsync(T entity)
    {
        return this.StorageContext.UpsertAsync(entity);
    }
}

/// <summary>
/// Specialization of Repository<T> for CopilotChatMessage.
/// </summary>
public class CopilotChatMessageRepository : Repository<CopilotChatMessage>
{
    private readonly ICopilotChatMessageStorageContext _messageStorageContext;

    public CopilotChatMessageRepository(ICopilotChatMessageStorageContext storageContext)
    : base(storageContext)
    {
        this._messageStorageContext = storageContext;
    }

    /// <summary>
    /// Finds chat messages matching a predicate.
    /// </summary>
    /// <param name="predicate">Predicate that needs to evaluate to true for a particular entryto be returned.</param>
    /// <param name="skip">Number of messages to skip before starting to return messages.</param>
    /// <param name="count">The number of messages to return. -1 returns all messages.</param>
    /// <returns>A list of ChatMessages matching the given chatId sorted from most recent to oldest.</returns>
    public async Task<IEnumerable<CopilotChatMessage>> QueryEntitiesAsync(Func<CopilotChatMessage, bool> predicate, int skip = 0, int count = -1)
    {
        return await Task.Run<IEnumerable<CopilotChatMessage>>(
            () => this._messageStorageContext.QueryEntitiesAsync(predicate, skip, count));
    }
}
