// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using CopilotChat.WebApi.Auth;
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
using Microsoft.SemanticKernel.AI;
using Microsoft.SemanticMemory.Client;
using Microsoft.SemanticMemory.Client.Models;

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
    private readonly ChatSessionRepository _sessionRepository;
    private readonly ChatMemorySourceRepository _sourceRepository;
    private readonly ChatMessageRepository _messageRepository;
    private readonly ChatParticipantRepository _participantRepository;
    private readonly IAuthInfo _authInfo;
    private readonly IContentSafetyService? _contentSafetyService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DocumentImportController"/> class.
    /// </summary>
    public DocumentController(
        ILogger<DocumentController> logger,
        IOptions<DocumentMemoryOptions> documentMemoryOptions,
        IOptions<PromptsOptions> promptOptions,
        ChatSessionRepository sessionRepository,
        ChatMemorySourceRepository sourceRepository,
        ChatMessageRepository messageRepository,
        ChatParticipantRepository participantRepository,
        IAuthInfo authInfo,
        IContentSafetyService? contentSafety = null)
    {
        this._logger = logger;
        this._options = documentMemoryOptions.Value;
        this._promptOptions = promptOptions.Value;
        this._sessionRepository = sessionRepository;
        this._sourceRepository = sourceRepository;
        this._messageRepository = messageRepository;
        this._participantRepository = participantRepository;
        this._authInfo = authInfo;
        this._contentSafetyService = contentSafety;
    }

    /// <summary>
    /// Gets the status of content safety.
    /// </summary>
    /// <returns></returns>
    [HttpGet]
    [Route("document/safetystatus")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public bool ContentSafetyStatus()
    {
        return this._contentSafetyService?.ContentSafetyStatus(this._logger) ?? false;
    }

    /// <summary>
    /// Service API for importing a document.
    /// </summary>
    [Route("document/importstatus")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DocumentStatusAsync(
        [FromServices] ISemanticMemoryClient memoryClient,
        [FromServices] IHubContext<MessageRelayHub> messageRelayHubContext,
        [FromBody] DocumentStatusForm documentStatusForm)
    {
        try
        {
            await this.ValidateDocumentStatusFormAsync(documentStatusForm);
        }
        catch (ArgumentException ex)
        {
            return this.BadRequest(ex.Message);
        }

        var userId = this._authInfo.UserId;
        var chatId = documentStatusForm.ChatId.ToString();
        var targetCollectionName = documentStatusForm.DocumentScope == DocumentScopes.Global
            ? this._options.GlobalDocumentCollectionName
            : this._options.ChatDocumentCollectionNamePrefix + chatId;

        var statusResults = await QueryAsync().ToArrayAsync();

        //// Broadcast the document status event to other users.
        //if (documentStatusForm.DocumentScope == DocumentScopes.Chat) $$$ TODO - OPEN DESIGN POINT
        //{
        //    await messageRelayHubContext.Clients.Group(chatId)
        //        .SendAsync(ReceiveMessageClientCall, chatId, userId, chatMessage); // $$$ STATUS

        //    return this.Ok(chatMessage);
        //}

        //await messageRelayHubContext.Clients.All.SendAsync(
        //    GlobalDocumentUploadedClientCall, // $$$ STATUS
        //    documentMessageContent.ToFormattedStringNamesOnly(),
        //    documentStatusForm.UserName
        //);

        return this.Ok("Documents status reported.");

        async IAsyncEnumerable<StatusResult> QueryAsync()
        {
            foreach (var documentReference in documentStatusForm.FileReferences)
            {
                var status = await memoryClient.GetDocumentStatusAsync(targetCollectionName!, documentReference!);
                if (status == null)
                {
                    yield return new StatusResult(targetCollectionName, documentReference);
                }
                else
                {
                    yield return new StatusResult(targetCollectionName, documentReference)
                    {
                        IsStarted = true,
                        IsCompleted = status.Completed,
                        LastUpdate = status.LastUpdate,
                        Keys = Array.Empty<string>(), // $$$ DCR MEMORY | DCR CHAT
                        Tokens = 0, // $$$ DCR MEMORY | DCR CHAT
                    };
                }
            }
        }
    }

    /// <summary>
    /// Service API for importing a document.
    /// </summary>
    [Route("document/import")]
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> DocumentImportAsync(
        [FromServices] ISemanticMemoryClient memoryClient,
        [FromServices] IHubContext<MessageRelayHub> messageRelayHubContext,
        [FromForm] DocumentImportForm documentImportForm)
    {
        try
        {
            await this.ValidateDocumentImportFormAsync(documentImportForm);
        }
        catch (ArgumentException ex)
        {
            return this.BadRequest(ex.Message);
        }

        this._logger.LogInformation("Importing {0} document(s)...", documentImportForm.FormFiles.Count());

        // Pre-create chat-message
        DocumentMessageContent documentMessageContent = new();

        // TODO: [Issue #49] Perform the import in parallel.
        var importResults = await ImportAsync();

        // Broadcast the document uploaded event to other users.
        if (documentImportForm.DocumentScope == DocumentScopes.Chat)
        {
            var chatMessage = await this.TryCreateDocumentUploadMessage(
                documentMessageContent,
                documentImportForm);
            if (chatMessage == null)
            {
                //foreach (var importResult in importResults)
                //{
                // $$$ MEMORY DCR - await this.RemoveMemoriesAsync(kernel, importResult);
                //}
                return this.BadRequest("Failed to create chat message. All documents are removed.");
            }

            var chatId = documentImportForm.ChatId.ToString();
            var userId = this._authInfo.UserId;
            await messageRelayHubContext.Clients.Group(chatId)
                .SendAsync(ReceiveMessageClientCall, chatId, userId, chatMessage);

            return this.Ok(chatMessage);
        }

        await messageRelayHubContext.Clients.All.SendAsync(
            GlobalDocumentUploadedClientCall,
            documentMessageContent.ToFormattedStringNamesOnly(),
            this._authInfo.Name
        );

        return this.Ok("Documents imported successfully to global scope.");

        async Task<IList<ImportResult>> ImportAsync()
        {
            IEnumerable<ImportResult> importResults = new List<ImportResult>();

            await Task.WhenAll(
                documentImportForm.FormFiles.Select(
                    async formFile =>
                    await this.ImportDocumentHelperAsync(memoryClient, formFile, documentImportForm).ContinueWith(
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
    /// A class to store a document import results.
    /// </summary>
    private sealed class StatusResult
    {
        /// <summary>
        /// A boolean indicating whether the import is started.
        /// </summary>
        public bool IsStarted { get; set; } = false;

        /// <summary>
        /// A boolean indicating whether the import is completed.
        /// </summary>
        public bool IsCompleted { get; set; } = false;

        /// <summary>
        /// The name of the collection that the document is inserted to.
        /// </summary>
        public string CollectionName { get; set; }

        /// <summary>
        /// The file identifier.
        /// </summary>
        public string FileReference { get; }

        /// <summary>
        /// The file identifier.
        /// </summary>
        public DateTimeOffset LastUpdate { get; set; } = default;

        /// <summary>
        /// The keys of the inserted document chunks.
        /// </summary>
        public IEnumerable<string> Keys { get; set; } = Array.Empty<string>();

        /// <summary>
        /// The number of tokens in the document.
        /// </summary>
        public long Tokens { get; set; } = 0;

        /// <summary>
        /// Create a new instance of the <see cref="StatusResult"/> class.
        /// </summary>
        /// <param name="collectionName">The name of the collection that the document is inserted to.</param>
        public StatusResult(string collectionName, string fileReference)
        {
            this.CollectionName = collectionName;
            this.FileReference = fileReference;
        }

        /// <summary>
        /// Create a new instance of the <see cref="StatusResult"/> class representing a failed import.
        /// </summary>
        public static StatusResult Fail { get; } = new(string.Empty, string.Empty);

        /// <summary>
        /// Add a key to the list of keys.
        /// </summary>
        /// <param name="key">The key to be added.</param>
        public void AddKey(string key)
        {
            this.Keys = this.Keys.Append(key);
        }
    }

    /// <summary>
    /// Validates the document import form.
    /// </summary>
    /// <param name="documentImportForm">The document import form.</param>
    /// <returns></returns>
    /// <exception cref="ArgumentException">Throws ArgumentException if validation fails.</exception>
    private async Task ValidateDocumentImportFormAsync(DocumentImportForm documentImportForm)
    {
        // Make sure the user has access to the chat session if the document is uploaded to a chat session.
        if (documentImportForm.DocumentScope == DocumentScopes.Chat
                && !(await this.UserHasAccessToChatAsync(this._authInfo.UserId, documentImportForm.ChatId)))
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

            // Make sure the file type is supported. $$$ MEMORY
            //var fileType = this.GetFileType(Path.GetFileName(formFile.FileName));
            //switch (fileType)
            //{
            //    case SupportedFileType.Txt:
            //    case SupportedFileType.Pdf:
            //        break;
            //    case SupportedFileType.Jpg:
            //    case SupportedFileType.Png:
            //    case SupportedFileType.Tiff:
            //    {
            //        // $$$ OCR
            //        throw new ArgumentException($"Unsupported image file type: {fileType} when " +
            //            $"{OcrSupportOptions.PropertyName}:{nameof(OcrSupportOptions.Type)} is set to " +
            //            nameof(OcrSupportOptions.OcrSupportType.None));
            //    }
            //    default:
            //        throw new ArgumentException($"Unsupported file type: {fileType}");
            //}

            // $$$ ISIMAGE ???
            if (documentImportForm.UseContentSafety)
            {
                if (this._contentSafetyService == null || !this._contentSafetyService.ContentSafetyStatus(this._logger))
                {
                    throw new ArgumentException("Unable to analyze image. Content Safety is currently disabled in the backend.");
                }

                var violations = new List<string>();
                try
                {
                    // Call the content safety controller to analyze the image
                    var imageAnalysisResponse = await this._contentSafetyService.ImageAnalysisAsync(formFile, default);
                    violations = this._contentSafetyService.ParseViolatedCategories(imageAnalysisResponse, this._contentSafetyService.Options.ViolationThreshold);
                }
                catch (Exception ex) when (!ex.IsCriticalException())
                {
                    this._logger.LogError(ex, "Failed to analyze image {0} with Content Safety. ErrorCode: {{1}}", formFile.FileName, (ex as AIException)?.ErrorCode);
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
    /// Import a single document.
    /// </summary>
    /// <param name="orchestrator">The orchestrator.</param>
    /// <param name="formFile">The form file.</param>
    /// <param name="documentImportForm">The document import form.</param>
    /// <returns>Import result.</returns>
    private async Task<ImportResult> ImportDocumentHelperAsync(ISemanticMemoryClient memoryClient, IFormFile formFile, DocumentImportForm documentImportForm)
    {
        this._logger.LogInformation("Importing document {0}", formFile.FileName);

        // Create memory source
        var memorySource = this.CreateMemorySource(formFile, documentImportForm);

        using var stream = formFile.OpenReadStream();
        var uploadRequest = new DocumentUploadRequest
        {
            DocumentId = memorySource.Id,
            Files = new List<DocumentUploadRequest.UploadedFile> { new DocumentUploadRequest.UploadedFile(formFile.FileName, stream) },
            Index = this._promptOptions.MemoryIndexName,
        };

        uploadRequest.Tags.Add("chatid", documentImportForm.DocumentScope == DocumentScope.Chat ? documentImportForm.ChatId.ToString() : Guid.Empty.ToString());
        uploadRequest.Tags.Add("memory", this._promptOptions.DocumentMemoryName);

        await memoryClient.ImportDocumentAsync(uploadRequest);

        var importResult = new ImportResult(memorySource.Id);

        if (!(await this.TryUpsertMemorySourceAsync(memorySource)))
        {
            this._logger.LogDebug("Failed to upsert memory source for file {0}.", formFile.FileName);
            // $$$ MEMORY DCR - await this.RemoveMemoriesAsync(kernel, importResult);
            return ImportResult.Fail;
        }

        return importResult;
    }

    /// <summary>
    /// Create a memory source.
    /// </summary>
    /// <param name="formFile">The file to be uploaded</param>
    /// <param name="documentImportForm">The document upload form that contains additional necessary info</param>
    /// <returns>A MemorySource object.</returns>
    private MemorySource CreateMemorySource(
        IFormFile formFile,
        DocumentImportForm documentImportForm)
    {
        var chatId = documentImportForm.ChatId.ToString();
        var userId = this._authInfo.UserId;

        return new MemorySource(
            chatId,
            formFile.FileName,
            userId,
            MemorySourceType.File,
            formFile.Length,
            null);
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
        catch (Exception ex) when (ex is ArgumentOutOfRangeException)
        {
            return false;
        }
    }

    /// <summary>
    /// Try to create a chat message that represents document upload.
    /// </summary>
    /// <param name="documentMessageContent">The document message content</param>
    /// <param name="documentImportForm">The document upload form that contains additional necessary info</param>
    /// <returns>A ChatMessage object if successful, null otherwise</returns>
    private async Task<ChatMessage?> TryCreateDocumentUploadMessage(
        DocumentMessageContent documentMessageContent,
        DocumentImportForm documentImportForm)
    {
        var chatId = documentImportForm.ChatId.ToString();
        var userId = this._authInfo.UserId;
        var userName = this._authInfo.Name;

        var chatMessage = ChatMessage.CreateDocumentMessage(
            userId,
            userName,
            chatId,
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
