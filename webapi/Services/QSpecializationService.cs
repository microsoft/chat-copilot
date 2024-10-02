// Copyright (c) Quartech. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using CopilotChat.WebApi.Models.Request;
using CopilotChat.WebApi.Models.Storage;
using CopilotChat.WebApi.Plugins.Chat.Ext;
using CopilotChat.WebApi.Storage;
using CopilotChat.WebApi.Utilities;
using Microsoft.AspNetCore.Http;

namespace CopilotChat.WebApi.Services;

/// <summary>
/// The implementation class for specialization service.
/// </summary>
public class QSpecializationService : IQSpecializationService
{
    private SpecializationRepository _specializationSourceRepository;

    private QAzureOpenAIChatOptions _qAzureOpenAIChatOptions;

    private QBlobStorage _qBlobStorage;

    public QSpecializationService(
        SpecializationRepository specializationSourceRepository,
        QAzureOpenAIChatOptions qAzureOpenAIChatOptions
    )
    {
        this._specializationSourceRepository = specializationSourceRepository;
        this._qAzureOpenAIChatOptions = qAzureOpenAIChatOptions;

        BlobServiceClient blobServiceClient = new(qAzureOpenAIChatOptions.BlobStorage.ConnectionString);

        BlobContainerClient blobContainerClient = blobServiceClient.GetBlobContainerClient(
            qAzureOpenAIChatOptions.BlobStorage.SpecializationContainerName
        );

        this._qBlobStorage = new QBlobStorage(blobContainerClient);
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
    /// <param name="qSpecializationMutate">Specialization mutate payload</param>
    /// <returns>The task result contains the specialization source</returns>
    public async Task<Specialization> SaveSpecialization(QSpecializationMutate qSpecializationMutate)
    {
        // Add the image to the blob storage or use the default image
        var imageFilePath =
            qSpecializationMutate.ImageFile == null
                ? ResourceUtils.GetImageAsDataUri(this._qAzureOpenAIChatOptions.DefaultSpecializationImage)
                : await this._qBlobStorage.AddBlobAsync(qSpecializationMutate.ImageFile);

        // Add the icon to the blob storage or use the default icon
        var iconFilePath =
            qSpecializationMutate.IconFile == null
                ? ResourceUtils.GetImageAsDataUri(this._qAzureOpenAIChatOptions.DefaultSpecializationIcon)
                : await this._qBlobStorage.AddBlobAsync(qSpecializationMutate.IconFile);

        Specialization specializationSource =
            new(
                qSpecializationMutate.Label,
                qSpecializationMutate.Name,
                qSpecializationMutate.Description,
                qSpecializationMutate.RoleInformation,
                qSpecializationMutate.IndexName,
                qSpecializationMutate.Deployment,
                qSpecializationMutate.RestrictResultScope,
                qSpecializationMutate.Strictness,
                qSpecializationMutate.DocumentCount,
                imageFilePath,
                iconFilePath,
                qSpecializationMutate.GroupMemberships.Split(',')
            );

        await this._specializationSourceRepository.CreateAsync(specializationSource);

        return specializationSource;
    }

    /// <summary>
    /// Updates the specialization.
    /// </summary>
    /// <param name="specializationId">Unique identifier of the specialization</param>
    /// <param name="qSpecializationMutate">Specialization mutate payload</param>
    /// <returns>The task result contains the specialization source</returns>
    public async Task<Specialization?> UpdateSpecialization(
        Guid specializationId,
        QSpecializationMutate qSpecializationMutate
    )
    {
        Specialization? specializationToUpdate = await this._specializationSourceRepository.FindByIdAsync(
            specializationId.ToString()
        );

        if (specializationToUpdate == null)
        {
            // Handle the case where no Specialization was found
            return null;
        }

        // Update the image file and set the file path
        specializationToUpdate.ImageFilePath = await this.UpsertSpecializationBlobAsync(
            qSpecializationMutate.ImageFile,
            new Uri(specializationToUpdate.ImageFilePath),
            Convert.ToBoolean(qSpecializationMutate.DeleteImageFile),
            ResourceUtils.GetImageAsDataUri(this._qAzureOpenAIChatOptions.DefaultSpecializationImage)
        );

        // Update the icon file and set the file path
        specializationToUpdate.IconFilePath = await this.UpsertSpecializationBlobAsync(
            qSpecializationMutate.IconFile,
            new Uri(specializationToUpdate.IconFilePath),
            Convert.ToBoolean(qSpecializationMutate.DeleteIconFile),
            ResourceUtils.GetImageAsDataUri(this._qAzureOpenAIChatOptions.DefaultSpecializationIcon)
        );

        specializationToUpdate.IsActive = Convert.ToBoolean(qSpecializationMutate.isActive);
        specializationToUpdate.Name = !string.IsNullOrEmpty(qSpecializationMutate.Name)
            ? qSpecializationMutate.Name
            : specializationToUpdate.Name;
        specializationToUpdate.Label = !string.IsNullOrEmpty(qSpecializationMutate.Label)
            ? qSpecializationMutate.Label
            : specializationToUpdate.Label;
        specializationToUpdate.Description = !string.IsNullOrEmpty(qSpecializationMutate.Description)
            ? qSpecializationMutate.Description
            : specializationToUpdate.Description;
        specializationToUpdate.RoleInformation = !string.IsNullOrEmpty(qSpecializationMutate.RoleInformation)
            ? qSpecializationMutate.RoleInformation
            : specializationToUpdate.RoleInformation;
        specializationToUpdate.IndexName = qSpecializationMutate.IndexName ?? specializationToUpdate.IndexName;

        // Group memberships (mutate payload) are a comma separated list of UUIDs.
        specializationToUpdate.GroupMemberships = !string.IsNullOrEmpty(qSpecializationMutate.GroupMemberships)
            ? qSpecializationMutate.GroupMemberships.Split(',')
            : specializationToUpdate.GroupMemberships;

        specializationToUpdate.Deployment = qSpecializationMutate.Deployment ?? specializationToUpdate.Deployment;
        specializationToUpdate.RestrictResultScope =
            qSpecializationMutate.RestrictResultScope ?? specializationToUpdate.RestrictResultScope;
        specializationToUpdate.Strictness = qSpecializationMutate.Strictness ?? specializationToUpdate.Strictness;
        specializationToUpdate.DocumentCount =
            qSpecializationMutate.DocumentCount ?? specializationToUpdate.DocumentCount;

        await this._specializationSourceRepository.UpsertAsync(specializationToUpdate);

        return specializationToUpdate;
    }

    /// <summary>
    /// Deletes the specialization.
    /// </summary>
    /// <param name="specializationId">Unique identifier of the specialization</param>
    /// <returns>The task result contains the delete state</returns>
    public async Task<bool> DeleteSpecialization(Guid specializationId)
    {
        Specialization? specializationToDelete = await this._specializationSourceRepository.FindByIdAsync(
            specializationId.ToString()
        );

        await this._specializationSourceRepository.DeleteAsync(specializationToDelete);

        var imageFileUri = new Uri(specializationToDelete.ImageFilePath);
        var iconFileUri = new Uri(specializationToDelete.IconFilePath);

        // Remove the image file from the blob storage if it is a Blob Storage URI
        if (await this._qBlobStorage.BlobExistsAsync(imageFileUri))
        {
            await this._qBlobStorage.DeleteBlobByURIAsync(imageFileUri);
        }

        // Remove the icon file from the blob storage if it is a Blob Storage URI
        if (await this._qBlobStorage.BlobExistsAsync(iconFileUri))
        {
            await this._qBlobStorage.DeleteBlobByURIAsync(iconFileUri);
        }

        return true;
    }

    /// <summary>
    /// Upsert the specialization blob and return filepath or blob storage URI.
    /// </summary>
    /// <param name="file">File to store in blob storage</param>
    /// <param name="fileUri">File path URI</param>
    /// <param name="delete">Flag to delete the file from the blob storage</param>
    /// <param name="filePathDefault">File path default value</param>
    /// <returns>FilePath or Blob Storage URI</returns>
    private async Task<string> UpsertSpecializationBlobAsync(
        IFormFile? file,
        System.Uri fileUri,
        bool delete = false,
        string filePathDefault = ""
    )
    {
        var blobExists = await this._qBlobStorage.BlobExistsAsync(fileUri);

        // 1. File provided and a default file path is stored in the DB
        if (file != null && !blobExists)
        {
            return await this._qBlobStorage.AddBlobAsync(file);
        }

        // 2. File provided and a Blob Storage URI is stored in the DB
        if (file != null && blobExists)
        {
            await this._qBlobStorage.DeleteBlobByURIAsync(fileUri);
            return await this._qBlobStorage.AddBlobAsync(file);
        }

        // 3. File not provided and a default file path is stored in the DB and delete flag is set
        if (file == null && blobExists && delete)
        {
            await this._qBlobStorage.DeleteBlobByURIAsync(fileUri);

            return filePathDefault;
        }

        return fileUri.ToString();
    }
}
