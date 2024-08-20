// Copyright (c) Quartech. All rights reserved.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CopilotChat.WebApi.Models.Storage;

namespace CopilotChat.WebApi.Storage;

/// <summary>
/// A repository for specialization management.
/// </summary>
public class SpecializationSourceRepository : Repository<SpecializationSource>
{
    /// <summary>
    /// Initializes a new instance of the SpecializationSourceRepository class.
    /// </summary>
    /// <param name="storageContext">The storage context.</param>
    public SpecializationSourceRepository(IStorageContext<SpecializationSource> storageContext)
        : base(storageContext)
    {
    }

    /// <summary>
    /// Retrieves all specializations.
    /// </summary>
    /// <returns>A list of specializations.</returns>
    public Task<IEnumerable<SpecializationSource>> GetAllSpecializationsAsync()
    {
        return base.StorageContext.QueryEntitiesAsync(e => true);
    }

    /// <summary>
    /// Retrieves specialization by key.
    /// </summary>
    /// <returns>A specialization matching the key.</returns>
    public SpecializationSource GetSpecializationAsync(string key)
    {
        var specializations = base.StorageContext.QueryEntitiesAsync(e => e.Key == key);
        return specializations.Result.First();
    }
}
