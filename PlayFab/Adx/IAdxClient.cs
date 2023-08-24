// Copyright (c) Microsoft. All rights reserved.

namespace PlayFab.Adx;

public interface IAdxClient
{
    /// <summary>
    /// <summary>
    /// Execute the given KQL query and return the results as a list of dictionaries.
    /// </summary>
    /// <param name="kqlQuery">The KQL to execute</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>The results of the query</returns>
    Task<AdxTableResult[]> ExecuteQueryAsync(string kqlQuery, CancellationToken cancellationToken);
}

public class AdxTableResult : List<AdxRowResult>
{
    /// <summary>
    /// Get the list of table headers
    /// </summary>
    public IReadOnlyList<string> Headers => this.FirstOrDefault()?.Keys.ToList() ?? new List<string>();
};

public class AdxRowResult : Dictionary<string, object> { };
