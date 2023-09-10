// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
namespace CopilotChat.WebApi.Storage;

/// <summary>
/// A storage context that stores entities in a MongoDB container.
/// </summary>
public class MongoDbContext<T> : IStorageContext<T>, IDisposable where T : IStorageEntity
{
    /// <summary>
    /// The MongoDB client.
    /// </summary>
    private readonly MongoClient _client;

    /// <summary>
    /// MongoDB database.
    /// </summary>
    private readonly IMongoDatabase _database;

    /// <summary>
    /// MongoDB collection.
    /// </summary>
    private readonly IMongoCollection<T> _collection;

    /// <summary>
    /// Initializes a new instance of the MongoDbContext class.
    /// </summary>
    /// <param name="connectionString">The MongoDB connection string.</param>
    /// <param name="database">The MongoDB database name.</param>
    /// <param name="collection">The MongoDB collection name.</param>
    public MongoDbContext(string connectionString, string database, string collection)
    {
        this._client = new MongoClient(connectionString);
        this._database = this._client.GetDatabase(database);
        this._collection = this._database.GetCollection<T>(collection);
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<T>> QueryEntitiesAsync(Func<T, bool> predicate)
    {
        return await Task.Run<IEnumerable<T>>(
            () => this._collection.AsQueryable<T>().Where(predicate).AsEnumerable());
    }

    /// <inheritdoc/>
    public async Task CreateAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new ArgumentOutOfRangeException(nameof(entity.Id), "Entity Id cannot be null or empty.");
        }

        await this._collection.InsertOneAsync(entity);
    }

    /// <inheritdoc/>
    public async Task DeleteAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new ArgumentOutOfRangeException(nameof(entity.Id), "Entity Id cannot be null or empty.");
        }
        var filter = Builders<T>.Filter.Where(t => t.Id == entity.Id);
        await this._collection.FindOneAndDeleteAsync<T>(filter);
    }

    /// <inheritdoc/>
    public async Task<T> ReadAsync(string entityId, string partitionKey)
    {
        if (string.IsNullOrWhiteSpace(entityId))
        {
            throw new ArgumentOutOfRangeException(nameof(entityId), "Entity Id cannot be null or empty.");
        }

        var filter = Builders<T>.Filter.Where(t => t.Id == entityId);
        var response = await this._collection.Find(filter).FirstOrDefaultAsync();
        return response;

    }

    /// <inheritdoc/>
    public async Task UpsertAsync(T entity)
    {
        if (string.IsNullOrWhiteSpace(entity.Id))
        {
            throw new ArgumentOutOfRangeException(nameof(entity.Id), "Entity Id cannot be null or empty.");
        }
        var filter = Builders<T>.Filter.Where(t => t.Id == entity.Id);
        var replaceOptions = new ReplaceOptions
        {
            IsUpsert = true // This option specifies that an upsert should be performed.
        };

        await this._collection.ReplaceOneAsync(filter, entity, replaceOptions);
    }

    public void Dispose()
    {
        this.Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            this._client.Cluster.Dispose();
        }
    }
}
