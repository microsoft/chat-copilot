using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;

namespace ADME.Helpers;

public class AIPluginSettings
{
    [ConfigurationKeyName("schema_version")]
    [JsonPropertyName("schema_version")]
    public string SchemaVersion { get; set; } = "v1";

    [ConfigurationKeyName("name_for_model")]
    [JsonPropertyName("name_for_model")]
    public string NameForModel { get; set; } = string.Empty;

    [ConfigurationKeyName("name_for_human")]
    [JsonPropertyName("name_for_human")]
    public string NameForHuman { get; set; } = string.Empty;

    [ConfigurationKeyName("description_for_model")]
    [JsonPropertyName("description_for_model")]
    public string DescriptionForModel { get; set; } = string.Empty;

    [ConfigurationKeyName("description_for_human")]
    [JsonPropertyName("description_for_human")]
    public string DescriptionForHuman { get; set; } = string.Empty;

    [ConfigurationKeyName("auth")]
    [JsonPropertyName("auth")]
    public AuthModel Auth { get; set; } = new AuthModel();

    [ConfigurationKeyName("api")]
    [JsonPropertyName("api")]
    public ApiModel Api { get; set; } = new ApiModel();

    [ConfigurationKeyName("logo_url")]
    [JsonPropertyName("logo_url")]
    public string LogoUrl { get; set; } = string.Empty;

    [ConfigurationKeyName("contact_email")]
    [JsonPropertyName("contact_email")]
    public string ContactEmail { get; set; } = string.Empty;

    [ConfigurationKeyName("legal_info_url")]
    [JsonPropertyName("legal_info_url")]
    public string LegalInfoUrl { get; set; } = string.Empty;

}

public class AuthModel
{
    [ConfigurationKeyName("type")]
    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [ConfigurationKeyName("client_url")]
    [JsonPropertyName("client_url")]
    public string? ClientUrl { get; set; }

    [ConfigurationKeyName("authorization_type")]
    [JsonPropertyName("authorization_type")]
    public string? AuthorizationType { get; set; }

    [ConfigurationKeyName("authorization_url")]
    [JsonPropertyName("authorization_url")]
    public string? AuthorizationUrl { get; set; }

    [ConfigurationKeyName("scope")]
    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [ConfigurationKeyName("authorization_content_type")]
    [JsonPropertyName("authorization_content_type")]
    public string? AuthorizationContentType { get; set; }

    [ConfigurationKeyName("verification_tokens")]
    [JsonPropertyName("verification_tokens")]
    public VerificationToken? VerificationTokens { get; set; } = new VerificationToken();
}

public class ApiModel
{
    [ConfigurationKeyName("type")]
    [JsonPropertyName("type")]
    public string Type { get; set; } = "openapi";

    [ConfigurationKeyName("url")]
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [ConfigurationKeyName("has_user_authentication")]
    [JsonPropertyName("has_user_authentication")]
    public bool HasUserAuthentication { get; set; } = false;
}

public class VerificationToken
{
    [ConfigurationKeyName("openai")]
    [JsonPropertyName("openai")]
    public string? OpenApi { get; set; }
}