// Copyright (c) Quartech. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CopilotChat.WebApi.Models.Request;
using CopilotChat.WebApi.Models.Storage;
using CopilotChat.WebApi.Storage;

namespace CopilotChat.WebApi.Services;

/// <summary>
/// The implementation class for specialization service.
/// </summary>
public class QSpecializationService : IQSpecializationService
{
    private SpecializationSourceRepository _specializationSourceRepository;

    public QSpecializationService(SpecializationSourceRepository specializationSourceRepository)
    {
        this._specializationSourceRepository = specializationSourceRepository;
    }

    /// <summary>
    /// Retrieve all specializations.
    /// </summary>
    /// <returns>The task result contains all specializations</returns>
    public Task<IEnumerable<SpecializationSource>> GetAllSpecializations()
    {
        return this._specializationSourceRepository.GetAllSpecializationsAsync();
    }

    /// <summary>
    /// Retrieve a specialization based on key.
    /// </summary>
    /// <param name="key">Specialization key</param>
    /// <returns>Returns the specialization source</returns>
    public SpecializationSource GetSpecializationSource(string key)
    {
        return this._specializationSourceRepository.GetSpecializationAsync(key);
    }

    /// <summary>
    /// Creates new specialization.
    /// </summary>
    /// <param name="qSpecializationParameters">Specialization parameters</param>
    /// <returns>The task result contains the specialization source</returns>
    public async Task<SpecializationSource> SaveSpecialization(QSpecializationParameters qSpecializationParameters)
    {
        SpecializationSource specializationSource = new(qSpecializationParameters.key, qSpecializationParameters.Name, qSpecializationParameters.Description, qSpecializationParameters.RoleInformation, qSpecializationParameters.IndexName, qSpecializationParameters.ImageFilePath);
        await this._specializationSourceRepository.CreateAsync(specializationSource);
        return specializationSource;
    }

    /// <summary>
    /// Updates the specialization.
    /// </summary>
    /// <param name="specializationId">Unique identifier of the specialization</param>
    /// <param name="qSpecializationParameters">Specialization parameters</param>
    /// <returns>The task result contains the specialization source</returns>
    public async Task<SpecializationSource?> UpdateSpecialization(Guid specializationId, QSpecializationParameters qSpecializationParameters)
    {
        SpecializationSource? specializationToUpdate = null;
        if (await this._specializationSourceRepository.TryFindByIdAsync(specializationId.ToString(), callback: v => specializationToUpdate = v))
        {
            specializationToUpdate!.Name = !string.IsNullOrEmpty(qSpecializationParameters.Name) ? qSpecializationParameters.Name : specializationToUpdate!.Name;
            specializationToUpdate!.Description = !string.IsNullOrEmpty(qSpecializationParameters.Description) ? qSpecializationParameters.Description : specializationToUpdate!.Description;
            specializationToUpdate!.RoleInformation = !string.IsNullOrEmpty(qSpecializationParameters.RoleInformation) ? qSpecializationParameters.RoleInformation : specializationToUpdate!.RoleInformation;
            specializationToUpdate!.IndexName = !string.IsNullOrEmpty(qSpecializationParameters.IndexName) ? qSpecializationParameters.IndexName : specializationToUpdate!.IndexName;
            specializationToUpdate!.ImageFilePath = !string.IsNullOrEmpty(qSpecializationParameters.ImageFilePath) ? qSpecializationParameters.ImageFilePath : specializationToUpdate!.ImageFilePath;
            specializationToUpdate!.IsActive = qSpecializationParameters.isActive;
            await this._specializationSourceRepository.UpsertAsync(specializationToUpdate);
            return specializationToUpdate;
        }
        return null;
    }

    /// <summary>
    /// Deletes the specialization.
    /// </summary>
    /// <param name="specializationId">Unique identifier of the specialization</param>
    /// <returns>The task result contains the delete state</returns>
    public async Task<bool> DeleteSpecialization(Guid specializationId)
    {
        try
        {
            SpecializationSource? specializationToDelete = await this._specializationSourceRepository.FindByIdAsync(specializationId.ToString());
            await this._specializationSourceRepository.DeleteAsync(specializationToDelete);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException)
        {
            return false;
        }
    }
}
