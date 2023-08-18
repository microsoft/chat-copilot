// Copyright (c) Microsoft. All rights reserved.

using System.Threading.Tasks;
using CopilotChat.WebApi.Storage;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace CopilotChat.WebApi.Auth;

/// <summary>
/// Class implementing "authorization" that validates the user has access to a chat.
/// </summary>
public class ChatParticipantAuthorizationHandler : AuthorizationHandler<ChatParticipantRequirement, HttpContext>
{
    private readonly IAuthInfo _authInfo;
    private readonly ChatSessionRepository _chatSessionRepository;
    private readonly ChatParticipantRepository _chatParticipantRepository;

    /// <summary>
    /// Constructor
    /// </summary>
    public ChatParticipantAuthorizationHandler(
        IAuthInfo authInfo,
        ChatSessionRepository chatSessionRepository,
        ChatParticipantRepository chatParticipantRepository) : base()
    {
        this._authInfo = authInfo;
        this._chatSessionRepository = chatSessionRepository;
        this._chatParticipantRepository = chatParticipantRepository;
    }

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ChatParticipantRequirement requirement,
        HttpContext resource)
    {
        try
        {
            string? chatId = resource.GetRouteValue("chatId")?.ToString();
            if (chatId == null)
            {
                // delegate to downstream validation
                context.Succeed(requirement);
                return;
            }

            var session = await this._chatSessionRepository.FindByIdAsync(chatId);
            if (session == null)
            {
                // delegate to downstream validation
                context.Succeed(requirement);
                return;
            }

            bool isUserInChat = await this._chatParticipantRepository.IsUserInChatAsync(this._authInfo.UserId, chatId);
            if (!isUserInChat)
            {
                context.Fail(new AuthorizationFailureReason(this, "User does not have access to the requested chat."));
            }

            context.Succeed(requirement);
        }
        catch (Azure.Identity.CredentialUnavailableException ex)
        {
            context.Fail(new AuthorizationFailureReason(this, ex.Message));
        }
    }
}
