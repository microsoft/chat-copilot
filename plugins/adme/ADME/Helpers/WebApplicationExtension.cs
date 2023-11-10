using System.Reflection;
using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace ADME.Helpers;

public static class WebApplicationExtensions
{
    /// <summary>
    /// Adds Swagger extension to the provided web application builder.
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="scopes"></param>
    /// <param name="securitySchemeType"></param>
    /// <param name="includeXml"></param>
    public static void AddSwagger(this WebApplicationBuilder builder, string[]? scopes = default,
        SecuritySchemeType securitySchemeType = SecuritySchemeType.OAuth2, bool includeXml = true)
    {
        builder.Services.AddSwaggerGen(c =>
        {
            c.AddSecurityDefinition("OAuth2", new OpenApiSecurityScheme
            {
                Type = securitySchemeType,
                Flows = new OpenApiOAuthFlows()
                {
                    AuthorizationCode = new OpenApiOAuthFlow()
                    {
                        TokenUrl = new(
                            $"https://login.microsoftonline.com/{builder.Configuration["AzureAD:TenantId"]}/oauth2/v2.0/token"),
                        AuthorizationUrl =
                            new(
                                $"https://login.microsoftonline.com/{builder.Configuration["AzureAD:TenantId"]}/oauth2/v2.0/authorize"),
                        Scopes = scopes?.Aggregate(new Dictionary<string, string>(), (acc, scope) =>
                        {
                            acc.Add($"api://{builder.Configuration["AzureAd:ClientId"]}/{scope}", "Access the API");
                            return acc;
                        })
                    }
                }
            });


            c.AddSecurityRequirement(new OpenApiSecurityRequirement()
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "OAuth2"
                        },
                    },
                    new List<string>()
                }
            });

            // if (includeXml)
            //     c.IncludeXmlComments(Path.Combine(AppContext.BaseDirectory,
            //         $"{Assembly.GetEntryAssembly()!.GetName().Name}.xml"));

            c.OperationFilter<SwaggerDeprecatedFilter>();
        });

        builder.Services.ConfigureOptions<ConfigureSwaggerOptions>();
    }

    /// <summary>
    /// Configures the Swagger UI for the provided web application.
    /// </summary>
    /// <param name="app"></param>
    /// <param name="swaggerClientId"></param>
    /// <param name="realmClientId"></param>
    public static void UseSwaggerUi(this WebApplication app, string swaggerClientId, string realmClientId)
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.OAuthClientId(swaggerClientId);
            c.OAuthRealm(realmClientId);
            c.OAuthAppName(Assembly.GetExecutingAssembly().FullName);
            c.OAuthScopeSeparator(" ");
            c.OAuthUsePkce();
            //c.RoutePrefix = string.Empty; // Changes swagger ui from api.azurewebsites.net/swagger to api.azurewebsites.net

            var apiVersionDescriptionProvider = app.Services.GetRequiredService<IApiVersionDescriptionProvider>();
            foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions.Reverse())
            {
                c.SwaggerEndpoint($"/swagger/{description.GroupName}/swagger.json",
                    description.GroupName.ToUpperInvariant());
            }
        });
    }
}