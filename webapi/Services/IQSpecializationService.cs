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
    Task<IEnumerable<SpecializationSource>> GetAllSpecializations();

    /// <summary>
    /// Retrieve a specialization based on key.
    /// </summary>
    /// <param name="key">Specialization key</param>
    /// <returns>Returns the specialization source</returns>
    SpecializationSource GetSpecializationSource(string key);

    /// <summary>
    /// Creates new specialization.
    /// </summary>
    /// <param name="qSpecializationParameters">Specialization parameters</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the specialization source</returns>
    Task<SpecializationSource> SaveSpecialization(QSpecializationParameters qSpecializationParameters);

    /// <summary>
    /// Updates the specialization.
    /// </summary>
    /// <param name="specializationId">Unique identifier of the specialization</param>
    /// <param name="qSpecializationParameters">Specialization parameters</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the specialization source</returns>
    Task<SpecializationSource?> UpdateSpecialization(Guid specializationId, QSpecializationParameters qSpecializationParameters);

    /// <summary>
    /// Deletes the specialization.
    /// </summary>
    /// <param name="specializationId">Unique identifier of the specialization</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the delete state</returns>
    Task<bool> DeleteSpecialization(Guid specializationId);
}
