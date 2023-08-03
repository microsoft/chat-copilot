using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SemanticKernel.Service.CopilotChat.Models;
using SemanticKernel.Service.CopilotChat.Storage;
using System;
using System.Threading.Tasks;

namespace SemanticKernel.Service.CopilotChat.Controllers
{
    [Authorize]
    [ApiController]
    public class DocumentDeleteController : ControllerBase
    {
        private readonly ILogger<DocumentDeleteController> _logger;
        private readonly ChatSessionRepository _sessionRepository;
        private readonly ChatMemorySourceRepository _sourceRepository;
        private readonly ChatParticipantRepository _participantRepository;


        public DocumentDeleteController(
            ILogger<DocumentDeleteController> logger,
            ChatSessionRepository sessionRepository,
            ChatMemorySourceRepository sourceRepository,
            ChatParticipantRepository participantRepository
        )
        {
            _logger = logger;
            _sessionRepository = sessionRepository;
            _sourceRepository = sourceRepository;
            _participantRepository = participantRepository;
        }

        [Route("deleteDocument")]
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> DeleteDocumentAsync([FromForm] DocumentDeleteForm documentDeleteForm)
        {
            try
            {
                await ValidateDocumentDeleteFormAsync(documentDeleteForm);
            }
            catch (ArgumentException ex)
            {
                return BadRequest(ex.Message);
            }

            // Check if the document exists in the specified chat session.
            var memorySource = await _sourceRepository.FindByIdAsync(documentDeleteForm.DocumentId.ToString());
            if (memorySource == null || memorySource.ChatId.ToString() != documentDeleteForm.ChatId.ToString())
            {
                return BadRequest("Document not found in the specified chat session.");
            }

            // Check if the user has access to the chat session.
            if (!await UserHasAccessToChatAsync(documentDeleteForm.UserId, documentDeleteForm.ChatId))
            {
                return BadRequest("User does not have access to the chat session.");
            }

            try
            {
                // Delete the document from the repository.
                await _sourceRepository.DeleteAsync(memorySource);
            }
            catch (Exception ex)
            {
                // Handle any exceptions that occur during the deletion process.
                return BadRequest($"Failed to delete the document: {ex.Message}");
            }

            return Ok("Document deleted successfully.");
        }

        private async Task ValidateDocumentDeleteFormAsync(DocumentDeleteForm documentDeleteForm)
        {
            // Make sure the user has access to the chat session where the document exists.
            if (!(await UserHasAccessToChatAsync(documentDeleteForm.UserId, documentDeleteForm.ChatId)))
            {
                throw new ArgumentException("User does not have access to the chat session.");
            }

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


    }
}
