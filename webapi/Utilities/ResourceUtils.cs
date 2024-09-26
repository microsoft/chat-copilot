using System;
using System.IO;

namespace CopilotChat.WebApi.Utilities;

/// <summary>
/// Utility class for handling resource-related operations.
/// </summary>
internal static class ResourceUtils
{
    /// <summary>
    /// Converts an image file from the 'wwwroot' directory into a data URI for embedding in HTML.
    /// </summary>
    /// <param name="imageFileName">The name of the image file, including its extension, located in the 'wwwroot' directory.</param>
    /// <returns>A data URI string representing the image's content, suitable for use in HTML img src attributes.</returns>
    /// <exception cref="FileNotFoundException">Thrown if the image file does not exist at the specified path.</exception>
    /// <exception cref="IOException">Thrown if there's an error reading the image file.</exception>
    public static string GetImageAsDataUri(string imageFileName)
    {
        string imageFilePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", imageFileName);

        if (!File.Exists(imageFilePath))
        {
            throw new FileNotFoundException(
                $"The image '{imageFileName}' was not found in the wwwroot directory.",
                imageFileName
            );
        }

        byte[] imageBytes = File.ReadAllBytes(imageFilePath);
        string base64ImageRepresentation = Convert.ToBase64String(imageBytes);

        return $"data:image/png;base64,{base64ImageRepresentation}";
    }
}
