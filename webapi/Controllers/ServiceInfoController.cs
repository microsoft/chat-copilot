// Copyright (c) Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using CopilotChat.WebApi.Models.Response;
using CopilotChat.WebApi.Options;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.KernelMemory;

namespace CopilotChat.WebApi.Controllers;

/// <summary>
/// Controller responsible for returning information on the service.
/// </summary>
[ApiController]
public class ServiceInfoController : ControllerBase
{
    private static readonly string[] memoryStoreTypes = Enum.GetNames(typeof(MemoryStoreType));
    private static readonly string assemblyFileVersion = GetAssemblyFileVersion();

    private readonly ILogger<ServiceInfoController> _logger;

    private readonly IConfiguration Configuration;

    private readonly KernelMemoryConfig _memoryOptions;
    private readonly DocumentMemoryOptions _documentOptions;
    private readonly ChatAuthenticationOptions _chatAuthenticationOptions;
    private readonly FrontendOptions _frontendOptions;
    private readonly IEnumerable<Plugin> availablePlugins;
    private readonly ContentSafetyOptions _contentSafetyOptions;

    public ServiceInfoController(
        ILogger<ServiceInfoController> logger,
        IConfiguration configuration,
        IDictionary<string, Plugin> availablePlugins,
        IOptions<KernelMemoryConfig> memoryOptions,
        IOptions<DocumentMemoryOptions> documentOptions,
        IOptions<ChatAuthenticationOptions> chatAuthenticationOptions,
        IOptions<FrontendOptions> frontendOptions,
        IOptions<ContentSafetyOptions> contentSafetyOptions)
    {
        this._logger = logger;
        this.Configuration = configuration;
        this._memoryOptions = memoryOptions.Value;
        this._documentOptions = documentOptions.Value;
        this._chatAuthenticationOptions = chatAuthenticationOptions.Value;
        this._frontendOptions = frontendOptions.Value;
        this.availablePlugins = this.SanitizePlugins(availablePlugins);
        this._contentSafetyOptions = contentSafetyOptions.Value;
    }

    /// <summary>
    /// Return information on running service.
    /// </summary>
    [Route("info")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult GetServiceInfo()
    {
        var response =
            new ServiceInfoResponse
            {
                MemoryStore =
                    new MemoryStoreInfoResponse
                    {
                        Types = memoryStoreTypes,
                        SelectedType = this._memoryOptions.GetMemoryStoreType(this.Configuration).ToString(),
                    },
                AvailablePlugins = this.availablePlugins,
                Version = assemblyFileVersion,
                IsContentSafetyEnabled = this._contentSafetyOptions.Enabled,
                IsDeleteDocumentEnabled = this._documentOptions.AllowDocumentRemoval,
            };

        return this.Ok(response);
    }

    /// <summary>
    /// Return the auth config to be used by the frontend client to access this service.
    /// </summary>
    [Route("authConfig")]
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [AllowAnonymous]
    public IActionResult GetAuthConfig()
    {
        var azureAd = this._chatAuthenticationOptions.AzureAd!;
        var authorityUriString = string.Empty;
        if (!string.IsNullOrEmpty(azureAd.Instance) &&
            !string.IsNullOrEmpty(azureAd.TenantId))
        {
            var authorityUri = new Uri(azureAd.Instance);
            authorityUri = new Uri(authorityUri, azureAd.TenantId);
            authorityUriString = authorityUri.ToString();
        }

        var config =
            new FrontendAuthConfig
            {
                AuthType = this._chatAuthenticationOptions.Type.ToString(),
                AadAuthority = authorityUriString,
                AadClientId = this._frontendOptions.AadClientId,
                AadApiScope = $"api://{azureAd.ClientId}/{azureAd.Scopes}",
            };

        return this.Ok(config);
    }

    private static string GetAssemblyFileVersion()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        FileVersionInfo fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);

        return fileVersion.FileVersion ?? string.Empty;
    }

    /// <summary>
    /// Sanitize the plugins to only return the name and url.
    /// </summary>
    /// <param name="plugins">The plugins to sanitize.</param>
    /// <returns></returns>
    private IEnumerable<Plugin> SanitizePlugins(IDictionary<string, Plugin> plugins)
    {
        return
            plugins.Select(
                p =>
                    new Plugin
                    {
                        Name = p.Value.Name,
                        ManifestDomain = p.Value.ManifestDomain,
                    });
    }
}
