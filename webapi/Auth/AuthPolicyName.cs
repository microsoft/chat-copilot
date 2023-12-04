// Copyright (c) Microsoft. All rights reserved.
namespace CopilotChat.WebApi.Auth;

/// <summary>
/// Defines policy names for custom authorization policies.
/// </summary>
public static class AuthPolicyName
{
    /// <summary>
    /// Name of policy that ensures user has access to a given chat.
    /// </summary>
    public const string RequireChatParticipant = nameof(RequireChatParticipant);

    /// <summary>
    /// Name of policy that ensure user is admin.
    /// </summary>
    public const string RequireChatAdmin = nameof(RequireChatAdmin);
}
