// Copyright (c) Quartech. All rights reserved.

namespace CopilotChat.WebApi.Models.Request;

using System.Collections.Generic;

/// <summary>
/// Parameters for editing chat specialization.
/// </summary>
public class EditChatSpecializationParameters
{
    /// <summary>
    /// Specialization used to generate responses.
    /// </summary>
    public string SpecializationId { get; set; } = string.Empty;
    public List<string> ChatCompletionDeployments { get; set; } = new List<string>();
}
