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
    private SpecializationRepository _specializationSourceRepository;

    public QSpecializationService(SpecializationRepository specializationSourceRepository)
    {
        this._specializationSourceRepository = specializationSourceRepository;
    }

    /// <summary>
    /// Retrieve all specializations.
    /// </summary>
    /// <returns>The task result contains all specializations</returns>
    public Task<IEnumerable<Specialization>> GetAllSpecializations()
    {
        return this._specializationSourceRepository.GetAllSpecializationsAsync();
    }

    /// <summary>
    /// Retrieve a specialization based on key.
    /// </summary>
    /// <param name="key">Specialization key</param>
    /// <returns>Returns the specialization source</returns>
    public Task<Specialization> GetSpecializationAsync(string id)
    {
        return this._specializationSourceRepository.GetSpecializationAsync(id);
    }

    /// <summary>
    /// Creates new specialization.
    /// </summary>
    /// <param name="qSpecializationParameters">Specialization parameters</param>
    /// <returns>The task result contains the specialization source</returns>
    public async Task<Specialization> SaveSpecialization(QSpecializationParameters qSpecializationParameters)
    {
        Specialization specializationSource =
            new(
                qSpecializationParameters.label,
                qSpecializationParameters.Name,
                qSpecializationParameters.Description,
                qSpecializationParameters.RoleInformation,
                qSpecializationParameters.IndexName,
                qSpecializationParameters.Deployment,
                qSpecializationParameters.ImageFilePath!,
                qSpecializationParameters.IconFilePath,
                qSpecializationParameters.GroupMemberships
            );
        await this._specializationSourceRepository.CreateAsync(specializationSource);
        return specializationSource;
    }

    /// <summary>
    /// Updates the specialization.
    /// </summary>
    /// <param name="specializationId">Unique identifier of the specialization</param>
    /// <param name="qSpecializationParameters">Specialization parameters</param>
    /// <returns>The task result contains the specialization source</returns>
    public async Task<Specialization?> UpdateSpecialization(
        Guid specializationId,
        QSpecializationParameters qSpecializationParameters
    )
    {
        Specialization? specializationToUpdate = null;
        if (
            await this._specializationSourceRepository.TryFindByIdAsync(
                specializationId.ToString(),
                callback: v => specializationToUpdate = v
            )
        )
        {
            specializationToUpdate!.IsActive = qSpecializationParameters.isActive;
            specializationToUpdate!.Name = !string.IsNullOrEmpty(qSpecializationParameters.Name)
                ? qSpecializationParameters.Name
                : specializationToUpdate!.Name;
            specializationToUpdate!.Description = !string.IsNullOrEmpty(qSpecializationParameters.Description)
                ? qSpecializationParameters.Description
                : specializationToUpdate!.Description;
            specializationToUpdate!.RoleInformation = !string.IsNullOrEmpty(qSpecializationParameters.RoleInformation)
                ? qSpecializationParameters.RoleInformation
                : specializationToUpdate!.RoleInformation;
            specializationToUpdate!.IndexName =
                qSpecializationParameters.IndexName != null
                    ? qSpecializationParameters.IndexName
                    : specializationToUpdate!.IndexName;
            specializationToUpdate!.Deployment =
                qSpecializationParameters.Deployment != null
                    ? qSpecializationParameters.Deployment
                    : specializationToUpdate!.Deployment;
            specializationToUpdate!.ImageFilePath =
                qSpecializationParameters.ImageFilePath != null
                    ? qSpecializationParameters.ImageFilePath
                    : specializationToUpdate!.ImageFilePath;
            specializationToUpdate!.IconFilePath =
                qSpecializationParameters.IconFilePath != null
                    ? qSpecializationParameters.IconFilePath
                    : specializationToUpdate!.IconFilePath;
            specializationToUpdate!.GroupMemberships =
                qSpecializationParameters.GroupMemberships != null
                    ? qSpecializationParameters.GroupMemberships
                    : specializationToUpdate!.GroupMemberships;

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
            Specialization? specializationToDelete = await this._specializationSourceRepository.FindByIdAsync(
                specializationId.ToString()
            );
            await this._specializationSourceRepository.DeleteAsync(specializationToDelete);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException)
        {
            return false;
        }
    }
}
