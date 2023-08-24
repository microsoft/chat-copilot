// Copyright (c) Microsoft. All rights reserved.

using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Data.Net.Client;

namespace PlayFab.Adx;

/// <summary>
/// An Azure Data Explorer client used to interact with the Azure Data Explorer service.
/// </summary>
public class AdxClient : IAdxClient
{
    #region Data Members
    /// <summary>
    /// The name of the Azure Data Explorer database.
    /// </summary>
    private readonly string _kustoDatabaseName;

    /// <summary>
    /// The query provider used to execute Kusto queries.
    /// </summary>
    private readonly ICslQueryProvider _queryProvider;
    #endregion

    #region Constructors
    /// <summary>
    /// Constructor with AAD auth
    /// </summary>
    /// <param name="kustoClusterUri">The azure data explorer Uri</param>
    /// <param name="kustoDatabaseName">The azure data explorer database name</param>
    /// <param name="clientId">The AAD client Id for auth</param>
    /// <param name="clientSecret">The AAD client secret</param>
    public AdxClient(string kustoClusterUri, string kustoDatabaseName, string clientId, string clientSecret)
    {
        string connectionString = $"Data Source={kustoClusterUri};Initial Catalog={kustoDatabaseName};AAD Federated Security=True;Application Client Id={clientId};Application Key={clientSecret}";
        KustoConnectionStringBuilder kustoConnectionStringBuilder = new(connectionString);
        this._queryProvider = KustoClientFactory.CreateCslQueryProvider(kustoConnectionStringBuilder);
        this._kustoDatabaseName = kustoDatabaseName;
    }

    /// <summary>
    /// Constructor with managed identity auth
    /// </summary>
    /// <param name="kustoClusterUri">The azure data explorer Uri</param>
    /// <param name="kustoDatabaseName">The azure data explorer database name</param>
    public AdxClient(string kustoClusterUri, string kustoDatabaseName)
    {
        string connectionString = $"Data Source={kustoClusterUri};Initial Catalog={kustoDatabaseName};AAD Federated Security=True";
        KustoConnectionStringBuilder kustoConnectionStringBuilder = new(connectionString);
        this._queryProvider = KustoClientFactory.CreateCslQueryProvider(kustoConnectionStringBuilder);
        this._kustoDatabaseName = kustoDatabaseName;
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Execute the given KQL query and return the results as a list of dictionaries.
    /// </summary>
    /// <param name="kqlQuery">The KQL to execute</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>The results of the query</returns>
    public async Task<AdxTableResult[]> ExecuteQueryAsync(string kqlQuery, CancellationToken cancellationToken)
    {
        ClientRequestProperties requestOptions = new();
        var ret = new List<AdxTableResult>();
        using (var reader = await this._queryProvider.ExecuteQueryAsync(this._kustoDatabaseName, kqlQuery, requestOptions, cancellationToken))
        {
            do
            {
                var tableResults = new AdxTableResult();
                ret.Add(tableResults);

                while (reader.Read())
                {
                    var resultRow = new AdxRowResult();

                    for (int i = 0; i < reader.FieldCount; i++)
                    {
                        resultRow[reader.GetName(i)] = reader.GetValue(i);
                    }

                    tableResults.Add(resultRow);
                }
            }
            while (reader.NextResult());
        }

        return ret.Take(ret.Count - 3).ToArray();
    }
    #endregion
}

