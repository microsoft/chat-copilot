using System.Text.Json;
using ADME.Helpers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace ADME.Controllers;

/// <summary>
/// Returns the aiPlugin json for configuring chatGpt plugin
/// </summary>
[AllowAnonymous]
[ApiController]
[Route("/.well-known/")]
[Produces("application/json")]
public class AiPluginController : ControllerBase
{
    private readonly IConfiguration _configuration;

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="configuration"></param>
    public AiPluginController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("ai-plugin.json")]
    public async Task<IActionResult> GetAiPlugin()
    {
        var currentDomain = $"{Request.Scheme}://{Request.Host}";

        var aiPluginSettings = new AIPluginSettings();

        _configuration.GetSection("aiPlugin").Bind(aiPluginSettings);

        var json = JsonSerializer.Serialize(aiPluginSettings);

        // replace {url} with the current domain
        json = json.Replace("{url}", currentDomain, StringComparison.OrdinalIgnoreCase);

        return Ok(JsonSerializer.Deserialize<AIPluginSettings>(json));
    }

    /// <summary>
    /// 
    /// </summary>
    /// <returns></returns>
    [HttpGet("/logo.png")]
    public async Task<FileStreamResult> GetLogo()
    {
        FileStream stream = new FileStream("logo.png", FileMode.Open);

        return new FileStreamResult(stream, "image/png")
        {
            FileDownloadName = "logo.png"
        };
    }

    [HttpGet("openapi/ai.json")]
    public IActionResult GetAiOpenApiInfo()
    {
        using FileStream stream = new FileStream("openapi_ai.json", FileMode.Open);
        using StreamReader reader = new StreamReader(stream);
        var str = reader.ReadToEnd();
        object? myOut = JsonSerializer.Deserialize<object>(str);
        return Ok(myOut);
    }
}