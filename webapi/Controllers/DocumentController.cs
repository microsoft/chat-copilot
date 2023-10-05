// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CopilotChat.WebApi.Auth;
using CopilotChat.WebApi.Extensions;
using CopilotChat.WebApi.Hubs;
using CopilotChat.WebApi.Models.Request;
using CopilotChat.WebApi.Models.Response;
using CopilotChat.WebApi.Models.Storage;
using CopilotChat.WebApi.Options;
using CopilotChat.WebApi.Services;
using CopilotChat.WebApi.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticMemory;

namespace CopilotChat.WebApi.Controllers;

/// <summary>
/// Controller for importing documents.
/// </summary>
/// <remarks>
/// This controller is responsible for contracts that are not possible to fulfill by semantic-memory components.
/// </remarks>
[ApiController]
public class DocumentController : ControllerBase
{
    private const string GlobalDocumentUploadedClientCall = "GlobalDocumentUploaded";
    private const string ReceiveMessageClientCall = "ReceiveMessage";

    private readonly ILogger<DocumentController> _logger;
    private readonly PromptsOptions _promptOptions;
    private readonly DocumentMemoryOptions _options;
    private readonly ContentSafetyOptions _contentSafetyOptions;
    private readonly ChatSessionRepository _sessionRepository;
    private readonly ChatMemorySourceRepository _sourceRepository;
    private readonly ChatMessageRepository _messageRepository;
    private readonly ChatParticipantRepository _participantRepository;
    private readonly DocumentTypeProvider _documentTypeProvider;
    private readonly IAuthInfo _authInfo;
    private readonly IContentSafetyService _contentSafetyService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentImportController"/> class.
    /// </summary>
    public DocumentController(
        ILogger<DocumentController> logger,
        IAuthInfo authInfo,
        IOptions<DocumentMemoryOptions> documentMemoryOptions,
        IOptions<PromptsOptions> promptOptions,
        IOptions<ContentSafetyOptions> contentSafetyOptions,
        ChatSessionRepository sessionRepository,
        ChatMemorySourceRepository sourceRepository,
        ChatMessageRepository messageRepository,
        ChatParticipantRepository participantRepository,
        DocumentTypeProvider documentTypeProvider,
        IContentSafetyService contentSafetyService)
    {
        this._logger = logger;
        this._options = documentMemoryOptions.Value;
        this._promptOptions = promptOptions.Value;
        this._contentSafetyOptions = contentSafetyOptions.Value;
        this._sessionRepository = sessionRepository;
        this._sourceRepository = sourceRepository;
        this._messageRepository = messageRepository;
        this._participantRepository = participantRepository;
        this._documentTypeProvider = documentTypeProvider;
        this._authInfo = authInfo;
        this._contentSafetyService = contentSafetyService;
    }

    /// <summary>
    /// Service API for importing a document.
    /// Documents imported through this route will be considered as global documents.
    /// </summary>
    [Route("documents")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> DocumentImportAsync(
        [FromServices] ISemanticMemoryClient memoryClient,
        [FromServices] IHubContext<MessageRelayHub> messageRelayHubContext,
        [FromForm] DocumentImportForm documentImportForm)
    {
        return this.DocumentImportAsync(
            memoryClient,
            messageRelayHubContext,
            DocumentScopes.Global,
            DocumentMemoryOptions.GlobalDocumentChatId,
            documentImportForm
        );
    }

    /// <summary>
    /// Service API for importing a document.
    /// </summary>
    [Route("chats/{chatId}/documents")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public Task<IActionResult> DocumentImportAsync(
        [FromServices] ISemanticMemoryClient memoryClient,
        [FromServices] IHubContext<MessageRelayHub> messageRelayHubContext,
        [FromRoute] Guid chatId,
        [FromForm] DocumentImportForm documentImportForm)
    {
        return this.DocumentImportAsync(memoryClient, messageRelayHubContext, DocumentScopes.Chat, chatId, documentImportForm);
    }

    private async Task<IActionResult> DocumentImportAsync(
        ISemanticMemoryClient memoryClient,
        IHubContext<MessageRelayHub> messageRelayHubContext,
        DocumentScopes documentScope,
        Guid chatId,
        DocumentImportForm documentImportForm)
    {
        try
        {
            await this.ValidateDocumentImportFormAsync(chatId, documentScope, documentImportForm);
        }
        catch (ArgumentException ex)
        {
            return this.BadRequest(ex.Message);
        }

        this._logger.LogInformation("Importing {0} document(s)...", documentImportForm.FormFiles.Count());

        // Pre-create chat-message
        DocumentMessageContent documentMessageContent = new();

        var importResults = await ImportDocumentsAsync();

        // Broadcast the document uploaded event to other users.
        if (documentScope == DocumentScopes.Chat)
        {
            var chatMessage = await this.TryCreateDocumentUploadMessage(chatId, documentMessageContent);

            // If chat message isn't created, it is still broadcast and visible in the documents tab.
            // The chat message won't, however, be displayed when the chat is freshly rendered.

            var userId = this._authInfo.UserId;
            await messageRelayHubContext.Clients.Group(chatId.ToString())
                .SendAsync(ReceiveMessageClientCall, chatId, userId, chatMessage);

            return this.Ok(chatMessage);
        }

        await messageRelayHubContext.Clients.All.SendAsync(
            GlobalDocumentUploadedClientCall,
            documentMessageContent.ToFormattedStringNamesOnly(),
            this._authInfo.Name
        );

        return this.Ok("Documents imported successfully to global scope.");

        async Task<IList<ImportResult>> ImportDocumentsAsync()
        {
            IEnumerable<ImportResult> importResults = new List<ImportResult>();

            await Task.WhenAll(
                documentImportForm.FormFiles.Select(
                    async formFile =>
                        await ImportDocumentAsync(formFile).ContinueWith(
                            task =>
                            {
                                var importResult = task.Result;
                                if (importResult != null)
                                {
                                    documentMessageContent.AddDocument(
                                        formFile.FileName,
                                        this.GetReadableByteString(formFile.Length),
                                        importResult.IsSuccessful);

                                    importResults = importResults.Append(importResult);
                                }
                            },
                            TaskScheduler.Default)));

            return importResults.ToArray();
        }

        async Task<ImportResult> ImportDocumentAsync(IFormFile formFile)
        {
            this._logger.LogInformation("Importing document {0}", formFile.FileName);

            // Create memory source
            MemorySource memorySource = new(
                chatId.ToString(),
                formFile.FileName,
                this._authInfo.UserId,
                MemorySourceType.File,
                formFile.Length,
                hyperlink: null
            );

            if (!(await this.TryUpsertMemorySourceAsync(memorySource)))
            {
                this._logger.LogDebug("Failed to upsert memory source for file {0}.", formFile.FileName);

                return ImportResult.Fail;
            }

            if (!(await TryStoreMemoryAsync()))
            {
                await this.TryRemoveMemoryAsync(memorySource);
            }

            return new ImportResult(memorySource.Id);

            async Task<bool> TryStoreMemoryAsync()
            {
                try
                {
                    using var stream = formFile.OpenReadStream();
                    await memoryClient.StoreDocumentAsync(
                        this._promptOptions.MemoryIndexName,
                        memorySource.Id,
                        chatId.ToString(),
                        this._promptOptions.DocumentMemoryName,
                        formFile.FileName,
                        stream);

                    return true;
                }
                catch (Exception ex) when (ex is not SystemException)
                {
                    return false;
                }
            }
        }
    }

    #region Private

    /// <summary>
    /// A class to store a document import results.
    /// </summary>
    private sealed class ImportResult
    {
        /// <summary>
        /// A boolean indicating whether the import is successful.
        /// </summary>
        public bool IsSuccessful => !string.IsNullOrWhiteSpace(this.CollectionName);

        /// <summary>
        /// The name of the collection that the document is inserted to.
        /// </summary>
        public string CollectionName { get; set; }

        /// <summary>
        /// Create a new instance of the <see cref="ImportResult"/> class.
        /// </summary>
        /// <param name="collectionName">The name of the collection that the document is inserted to.</param>
        public ImportResult(string collectionName)
        {
            this.CollectionName = collectionName;
        }

        /// <summary>
        /// Create a new instance of the <see cref="ImportResult"/> class representing a failed import.
        /// </summary>
        public static ImportResult Fail { get; } = new(string.Empty);
    }

    /// <summary>
    /// Validates the document import form.
    /// </summary>
    /// <param name="documentImportForm">The document import form.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">Throws ArgumentException if validation fails.</exception>
    private async Task ValidateDocumentImportFormAsync(Guid chatId, DocumentScopes scope, DocumentImportForm documentImportForm)
    {
        // Make sure the user has access to the chat session if the document is uploaded to a chat session.
        if (scope == DocumentScopes.Chat
                && !(await this.UserHasAccessToChatAsync(this._authInfo.UserId, chatId)))
        {
            throw new ArgumentException("User does not have access to the chat session.");
        }

        var formFiles = documentImportForm.FormFiles;

        if (!formFiles.Any())
        {
            throw new ArgumentException("No files were uploaded.");
        }
        else if (formFiles.Count() > this._options.FileCountLimit)
        {
            throw new ArgumentException($"Too many files uploaded. Max file count is {this._options.FileCountLimit}.");
        }

        // Loop through the uploaded files and validate them before importing.
        foreach (var formFile in formFiles)
        {
            if (formFile.Length == 0)
            {
                throw new ArgumentException($"File {formFile.FileName} is empty.");
            }

            if (formFile.Length > this._options.FileSizeLimit)
            {
                throw new ArgumentException($"File {formFile.FileName} size exceeds the limit.");
            }

            // Make sure the file type is supported.
            var fileType = Path.GetExtension(formFile.FileName);
            if (!this._documentTypeProvider.IsSupported(fileType, out bool isSafetyTarget))
            {
                throw new ArgumentException($"Unsupported file type: {fileType}");
            }

            if (isSafetyTarget && documentImportForm.UseContentSafety)
            {
                if (!this._contentSafetyOptions.Enabled)
                {
                    throw new ArgumentException("Unable to analyze image. Content Safety is currently disabled in the backend.");
                }

                var violations = new List<string>();
                try
                {
                    // Call the content safety controller to analyze the image
                    var imageAnalysisResponse = await this._contentSafetyService.ImageAnalysisAsync(formFile, default);
                    violations = this._contentSafetyService.ParseViolatedCategories(imageAnalysisResponse, this._contentSafetyOptions.ViolationThreshold);
                }
                catch (Exception ex) when (!ex.IsCriticalException())
                {
                    this._logger.LogError(ex, "Failed to analyze image {0} with Content Safety. Details: {{1}}", formFile.FileName, ex.Message);
                    throw new AggregateException($"Failed to analyze image {formFile.FileName} with Content Safety.", ex);
                }

                if (violations.Count > 0)
                {
                    throw new ArgumentException($"Unable to upload image {formFile.FileName}. Detected undesirable content with potential risk: {string.Join(", ", violations)}");
                }
            }
        }
    }

    /// <summary>
    /// Validates the document import form.
    /// </summary>
    /// <param name="documentStatusForm">The document import form.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">Throws ArgumentException if validation fails.</exception>
    private async Task ValidateDocumentStatusFormAsync(DocumentStatusForm documentStatusForm)
    {
        // Make sure the user has access to the chat session if the document is uploaded to a chat session.
        if (documentStatusForm.DocumentScope == DocumentScopes.Chat
                && !(await this.UserHasAccessToChatAsync(documentStatusForm.UserId, documentStatusForm.ChatId)))
        {
            throw new ArgumentException("User does not have access to the chat session.");
        }

        var fileReferences = documentStatusForm.FileReferences;

        if (!fileReferences.Any())
        {
            throw new ArgumentException("No files identified.");
        }
        else if (fileReferences.Count() > this._options.FileCountLimit)
        {
            throw new ArgumentException($"Too many files requested. Max file count is {this._options.FileCountLimit}.");
        }

        // Loop through the uploaded files and validate them before importing.
        foreach (var fileReference in fileReferences)
        {
            if (string.IsNullOrWhiteSpace(fileReference))
            {
                throw new ArgumentException($"File {fileReference} is empty.");
            }
        }
    }

    /// <summary>
    /// Try to upsert a memory source.
    /// </summary>
    /// <param name="memorySource">The memory source to be uploaded</param>
    /// <returns>True if upsert is successful. False otherwise.</returns>
    private async Task<bool> TryUpsertMemorySourceAsync(MemorySource memorySource)
    {
        try
        {
            await this._sourceRepository.UpsertAsync(memorySource);
            return true;
        }
        catch (Exception ex) when (ex is not SystemException)
        {
            return false;
        }
    }

    /// <summary>
    /// Try to upsert a memory source.
    /// </summary>
    /// <param name="memorySource">The memory source to be uploaded</param>
    /// <returns>True if upsert is successful. False otherwise.</returns>
    private async Task<bool> TryRemoveMemoryAsync(MemorySource memorySource)
    {
        try
        {
            await this._sourceRepository.DeleteAsync(memorySource);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    /// <summary>
    /// Try to upsert a memory source.
    /// </summary>
    /// <param name="memorySource">The memory source to be uploaded</param>
    /// <returns>True if upsert is successful. False otherwise.</returns>
    private async Task<bool> TryStoreMemoryAsync(MemorySource memorySource)
    {
        try
        {
            await this._sourceRepository.UpsertAsync(memorySource);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    /// <summary>
    /// Try to create a chat message that represents document upload.
    /// </summary>
    /// <param name="chatId">The target chat-id</param>
    /// <param name="documentMessageContent">The document message content</param>
    /// <returns>A ChatMessage object if successful, null otherwise</returns>
    private async Task<ChatMessage?> TryCreateDocumentUploadMessage(
        Guid chatId,
        DocumentMessageContent documentMessageContent)
    {
        var chatMessage = ChatMessage.CreateDocumentMessage(
            this._authInfo.UserId,
            this._authInfo.Name, // User name
            chatId.ToString(),
            documentMessageContent);

        try
        {
            await this._messageRepository.CreateAsync(chatMessage);
            return chatMessage;
        }
        catch (Exception ex) when (ex is ArgumentOutOfRangeException)
        {
            return null;
        }
    }

    /// <summary>
    /// Converts a `long` byte count to a human-readable string.
    /// </summary>
    /// <param name="bytes">Byte count</param>
    /// <returns>Human-readable string of bytes</returns>
    private string GetReadableByteString(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int i;
        double dblsBytes = bytes;
        for (i = 0; i < sizes.Length && bytes >= 1024; i++, bytes /= 1024)
        {
            dblsBytes = bytes / 1024.0;
        }

        return string.Format(CultureInfo.InvariantCulture, "{0:0.#}{1}", dblsBytes, sizes[i]);
    }

    /// <summary>
    /// Check if the user has access to the chat session.
    /// </summary>
    /// <param name="userId">The user ID.</param>
    /// <param name="chatId">The chat session ID.</param>
    /// <returns>A boolean indicating whether the user has access to the chat session.</returns>
    private async Task<bool> UserHasAccessToChatAsync(string userId, Guid chatId)
    {
        return await this._participantRepository.IsUserInChatAsync(userId, chatId.ToString());
    }

    #endregion
}
