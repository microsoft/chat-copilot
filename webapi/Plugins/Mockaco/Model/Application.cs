using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using System;

namespace CopilotChat.WebApi.Plugins.OpenApi.GitHubPlugin.Model;
public class Application
{
  public string Id { get; set; } // {AppId}.1...
  public string PartitionKey { get; set; }  // AppId...large cardinality...
  public string UserId { get; set; }
  public string AppId { get; set; }
  public string AppName { get; set; }
  public string KeyValue { get; set; }
  public string[] Scopes { get; set; }
  public DateTime? Expiry { get; set; }
  public DateTime Created { get; set; }
  public DateTime Updated { get; set; }

  public override string ToString()
  {
    return JsonConvert.SerializeObject(this, new JsonSerializerSettings() { ContractResolver = new CamelCasePropertyNamesContractResolver() });
  }
}
