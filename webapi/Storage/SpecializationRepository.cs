// Copyright (c) Quartech. All rights reserved.

using System.Collections.Generic;
using System.Threading.Tasks;
using CopilotChat.WebApi.Models.Storage;

namespace CopilotChat.WebApi.Storage;

/// <summary>
/// A repository for specialization management.
/// </summary>
public class SpecializationRepository : Repository<Specialization>
{
    /// <summary>
    /// Initializes a new instance of the SpecializationSourceRepository class.
    /// </summary>
    /// <param name="storageContext">The storage context.</param>
    public SpecializationRepository(IStorageContext<Specialization> storageContext)
        : base(storageContext) { }

    /// <summary>
    /// Retrieves all specializations.
    /// </summary>
    /// <returns>A list of specializations.</returns>
    public Task<IEnumerable<Specialization>> GetAllSpecializationsAsync()
    {
        return base.StorageContext.QueryEntitiesAsync(e => true);
    }

    /// <summary>
    /// Retrieves specialization by key.
    /// </summary>
    /// <returns>A specialization matching the key.</returns>
    public async Task<Specialization> GetSpecializationAsync(string id)
    {
        return await base.StorageContext.ReadAsync(id, id);
    }
}
