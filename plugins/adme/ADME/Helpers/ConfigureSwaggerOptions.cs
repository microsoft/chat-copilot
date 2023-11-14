using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Mvc.ApiExplorer;

namespace ADME.Helpers;

/// <summary>
/// Helper class for configuration of Swagger options.
/// </summary>
public class ConfigureSwaggerOptions
        : IConfigureNamedOptions<SwaggerGenOptions>
{
    private readonly IApiVersionDescriptionProvider _provider;

    
    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigureSwaggerOptions"/> class with the provided _provider.
    /// </summary>
    /// <param name="provider">The _provider of the API version description.</param>
    public ConfigureSwaggerOptions(
        IApiVersionDescriptionProvider provider)
    {
        _provider = provider;
    }
    
    /// <summary>
    /// Configures Swagger with the provided options.
    /// </summary>
    /// <param name="options">The Swagger options used for configuration.</param>
    public void Configure(SwaggerGenOptions options)
    {
        // add swagger document for every API version discovered
        foreach (var description in _provider.ApiVersionDescriptions)
        {
            options.SwaggerDoc(
                description.GroupName,
                CreateVersionInfo(description));
        }
    }

    /// <summary>
    /// Configures Swagger with the provided name and options.
    /// </summary>
    /// <param name="name">The name of the configuration</param>
    /// <param name="options">The Swagger options used for configuration.</param>
    public void Configure(string? name, SwaggerGenOptions options)
    {
        Configure(options);
    }

    private static OpenApiInfo CreateVersionInfo(
            ApiVersionDescription description)
    {
        var info = new OpenApiInfo
        {
            Title = "Subsurface Cognitive Search API",
            Version = description.ApiVersion.ToString(),
            Description = "This is the API for the subsurface congitive search engine and uses Azure AD Bearer tokens for authentication",
            Contact = new OpenApiContact() { Name = "DataTrawlers", Email = "fg_datatrawlers@equinor.com" },
        };

        if (description.IsDeprecated)
            info.Description += " This API version has been deprecated.";

        return info;
    }
}

/// <summary>
/// Represents a filter for deprecated API versions.
/// </summary>
public class SwaggerDeprecatedFilter : IOperationFilter
{
    /// <inheritdoc />
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        if (operation.Parameters == null)
            operation.Parameters = new List<OpenApiParameter>();

        var apiDescription = context.ApiDescription;
        if (apiDescription.IsDeprecated())
        {
            operation.Deprecated = true;
        }
    }
}