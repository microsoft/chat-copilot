// Copyright (c) Quartech. All rights reserved.

namespace CopilotChat.WebApi.Models.Request;

/// <summary>
/// Parameters for editing chat specialization.
/// </summary>
public class EditChatSpecializationParameters
{
    /// <summary>
    /// Specialization used to generate responses.
    /// </summary>
    public string SpecializationKey { get; set; } = "general";
}
