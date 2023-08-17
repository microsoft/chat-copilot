// Copyright (c) Microsoft. All rights reserved.

namespace CopilotChat.WebApi.Options;

public class PlayFabOptions
{
    public static readonly string PropertyName = "PlayFab";

    public string Endpoint { get; set; }
    public string TitleId { get; set; }
    public string TitleApiEndpoint { get; set; }
    public string TitleSecretKey { get; set; }
    public string SwaggerEndpoint { get; set; }
    public string ReportsCosmosDBEndpoint { get; set; }
    public string ReportsCosmosDBKey { get; set; }
    public string ReportsCosmosDBDatabaseName { get; set; }
    public string ReportsCosmosDBContainerName { get; set; }
}
