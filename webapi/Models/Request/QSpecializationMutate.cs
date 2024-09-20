// Copyright (c) Microsoft. All rights reserved.

using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace CopilotChat.WebApi.Models.Request;

/// <summary>
/// Form for mutating a Specialization with a POST Http request.
/// Includes raw files to be added to Blob Storage Container
/// </summary>
public class QSpecializationMutate : QSpecializationBase
{
    /// <summary>
    /// Image file of the Specialization.
    /// </summary>
    [JsonPropertyName("imageFile")]
    public IFormFile? IconFile { get; set; } = null;

    /// <summary>
    /// Icon file of the Specialization.
    /// </summary>
    [JsonPropertyName("iconFile")]
    public IFormFile? ImageFile { get; set; } = null;

    /// <summary>
    /// String (Boolean) flag to delete the image file.
    /// </summary>
    [JsonPropertyName("deleteImageFile")]
    public string? DeleteImageFile { get; set; } = null;

    /// <summary>
    /// Boolean (Boolean) flag to delete the icon file.
    /// </summary>
    [JsonPropertyName("deleteIconFile")]
    public string? DeleteIconFile { get; set; } = null;

    /// <summary>
    /// Overrides the GroupMemberships property from the base class.
    ///
    /// Why? Mutate payloads are `FromForm` ie: `FormData` which expect all property values to be strings.
    /// This value will need to be deserialized into a list of strings.
    /// </summary>
    [JsonPropertyName("groupMemberships")]
    public new string GroupMemberships { get; set; } = string.Empty;
}
