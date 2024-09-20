// Copyright (c) Quartech. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CopilotChat.WebApi.Models.Request;
using CopilotChat.WebApi.Models.Storage;

namespace CopilotChat.WebApi.Services;

/// <summary>
/// Defines specialization service
/// </summary>
public interface IQSpecializationService
{
    /// <summary>
    /// Retrieve all specializations.
    /// </summary>
    /// <returns>A task that represents the asynchronous operation. The task result contains all specializations</returns>
    Task<IEnumerable<Specialization>> GetAllSpecializations();

    /// <summary>
    /// Retrieve a specialization based on id.
    /// </summary>
    /// <param name="id">Specialization id</param>
    /// <returns>Returns the specialization</returns>
    Task<Specialization> GetSpecializationAsync(string id);

    /// <summary>
    /// Creates new specialization.
    /// </summary>
    /// <param name="qSpecializationMutate">Specialization mutate payload</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the specialization</returns>
    Task<Specialization> SaveSpecialization(QSpecializationMutate qSpecializationMutate);

    /// <summary>
    /// Updates the specialization.
    /// </summary>
    /// <param name="specializationId">Unique identifier of the specialization</param>
    /// <param name="qSpecializationMutate">Specialization mutate payload</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the specialization</returns>
    Task<Specialization?> UpdateSpecialization(Guid specializationId, QSpecializationMutate qSpecializationMutate);

    /// <summary>
    /// Deletes the specialization.
    /// </summary>
    /// <param name="specializationId">Unique identifier of the specialization</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the delete state</returns>
    Task<bool> DeleteSpecialization(Guid specializationId);
}
