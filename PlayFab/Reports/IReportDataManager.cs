// Copyright (c) Microsoft. All rights reserved.

using System.Globalization;
using Microsoft.Extensions.Caching.Memory;
using Polly;
using Polly.Caching;
using Polly.Caching.Memory;
using Polly.Fallback;
using Polly.Wrap;

namespace PlayFab.Reports;

public interface IReportDataManager
{
    Task<IList<PlayFabReport>> GetPlayFabReportsAsync(string titleId, CancellationToken cancellationToken);
}

public class ReportDataManager : IReportDataManager
{
    private readonly AsyncPolicyWrap<PlayFabReport[]> _cachingPolicy;
    private readonly IReportDataAccess _reportDataAccess;

    public ReportDataManager(IReportDataAccess reportDataAccess)
    {
        _reportDataAccess = reportDataAccess ?? throw new ArgumentNullException(nameof(reportDataAccess));

        // Configure Memory Cache
        MemoryCache reportsMemoryCache = new MemoryCache(new MemoryCacheOptions());

        // Configure the MemoryCacheProvider with the MemoryCache instance
        var cacheProvider = new MemoryCacheProvider(reportsMemoryCache);

        // Configure the cache policy with a MemoryCacheProvider
        AsyncCachePolicy<PlayFabReport[]> cachePolicy = Policy.CacheAsync<PlayFabReport[]>(cacheProvider, TimeSpan.FromHours(1));
        AsyncFallbackPolicy<PlayFabReport[]> fallbackPolicy = Policy<PlayFabReport[]>.Handle<Exception>().FallbackAsync(new PlayFabReport[0]);

        _cachingPolicy = Policy.WrapAsync(cachePolicy, fallbackPolicy);
    }

    public async Task<IList<PlayFabReport>> GetPlayFabReportsAsync(string titleId, CancellationToken cancellationToken)
    {
        var context = new Context(titleId);
        context["TitleId"] = titleId;

        PlayFabReport[] playFabReports = await
            _cachingPolicy
            .ExecuteAsync(
                (Context context, CancellationToken ct) => this.LoadPlayFabReportsAsync(context["TitleId"]?.ToString(), ct),
                context,
                cancellationToken);

        return playFabReports;
    }

    private async Task<PlayFabReport[]> LoadPlayFabReportsAsync(string titleId, CancellationToken cancellationToken)
    {
        DateTime today = DateTime.UtcNow;
        IList<GameReport> gameReports = await _reportDataAccess.GetByQueryAsync(
            $"SELECT TOP 30 * FROM c WHERE c.TitleId='{titleId}' ORDER BY c.ReportDate DESC",
            cancellationToken);

        Dictionary<string, GameReport> latestReports = gameReports
          .GroupBy(report => report.ReportName)
          .Select(group => group.OrderByDescending(report => report.ReportDate).First())
          .ToDictionary(report => report.ReportName);

        if (!latestReports.ContainsKey("EngagementMetricsRollupReportCSV"))
        {
            GameReport gameEngagementRollupReport = (await _reportDataAccess.GetByQueryAsync(
                 $"SELECT TOP 1 * FROM c WHERE c.TitleId='{titleId}' and c.ReportName='EngagementMetricsRollupReportCSV' ORDER BY c.ReportDate DESC",
                cancellationToken)).FirstOrDefault();
            latestReports.Add(gameEngagementRollupReport.ReportName, gameEngagementRollupReport);
        }

        var playFabReports = new List<PlayFabReport>();

        // Report 1 - Daily Overview Report
        if (latestReports.TryGetValue("DailyOverviewReport", out GameReport? dailyOverviewReport))
        {
            PlayFabReportColumn[] dailyOverviewReportColumns = new[]
            {
                new PlayFabReportColumn { Name = "Timestamp", Description = "The date and time of a one-hour window when the report was compiled, presented in Coordinated Universal Time (UTC)." },
                new PlayFabReportColumn { Name = "TotalLogins", Description = "The aggregate count of player logins during the specified hour, revealing the volume of player interactions." },
                new PlayFabReportColumn { Name = "UniqueLogins", Description = "The distinct number of players who logged into the game within the same hour, indicating individual engagement." },
                new PlayFabReportColumn { Name = "UniquePayers", Description = "The count of unique players who conducted in-game purchases, reflecting the game's monetization reach." },
                new PlayFabReportColumn { Name = "Revenue", Description = "The cumulative revenue in dollars generated from in-game purchases throughout the hour, demonstrating financial performance." },
                new PlayFabReportColumn { Name = "Purchases", Description = "The total number of in-game transactions carried out by players in the specified hour." },
                // new PlayFabReportColumn { Name = "TotalCalls", Description = "The collective sum of player-initiated interactions, encompassing gameplay actions, API requests, and more." },
                // new PlayFabReportColumn { Name = "TotalSuccessfulCalls", Description = "The count of interactions that succeeded without encountering errors, highlighting player satisfaction." },
                // new PlayFabReportColumn { Name = "TotalErrors", Description = "The overall number of errors encountered during interactions, potential indicators of player experience challenges." },
                new PlayFabReportColumn { Name = "Arpu", Description = "Average Revenue Per User, The average revenue generated per unique player, calculated as Revenue / UniquePayers." },
                new PlayFabReportColumn { Name = "Arppu", Description = "Average Revenue Per Paying User. The average revenue generated per player who made purchases, calculated as Revenue / UniquePayers." },
                new PlayFabReportColumn { Name = "AvgPurchasePrice", Description = "The average price of in-game purchases made by players, calculated as Revenue / Purchases." },
                new PlayFabReportColumn { Name = "NewUsers", Description = "The count of new players who started engaging with the game during the specified hour period." },
            };

            playFabReports.Add(new()
            {
                Columns = dailyOverviewReportColumns,
                Description = "Granular single day data capturing game reports for each hour. The report has 24 rows where every row reprsents one hour of the day.",
                CsvData = PlayFabReport.CreateCsvReportFromJsonArray(dailyOverviewReport.ReportData, dailyOverviewReportColumns),
                ReportName = dailyOverviewReport.ReportName
            });
        }

        // Report 2 - Rolling 30 Day Overview Report
        string ParseDailyReportDate(string str) => DateTime.Parse(str, CultureInfo.InvariantCulture).ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
        if (latestReports.TryGetValue("RollingThirtyDayOverviewReport", out GameReport? rollingThirtyDayOverviewReport))
        {
            PlayFabReportColumn[] rollingThirtyDayOverviewReportColumns = new[]
            {
                new PlayFabReportColumn { Name = "Timestamp", SourceParser=ParseDailyReportDate, Description = "The date of a one-day window when the report was compiled, presented in Coordinated Universal Time (UTC)." },
                new PlayFabReportColumn { Name = "TotalLogins", Description = "The aggregate count of player logins during the specified hour, revealing the volume of player interactions." },
                new PlayFabReportColumn { Name = "UniqueLogins", Description = "The distinct number of players who logged into the game within the same hour, indicating individual engagement." },
                new PlayFabReportColumn { Name = "UniquePayers", Description = "The count of unique players who conducted in-game purchases, reflecting the game's monetization reach." },
                new PlayFabReportColumn { Name = "Revenue", Description = "The cumulative revenue in dollars generated from in-game purchases throughout the hour, demonstrating financial performance." },
                new PlayFabReportColumn { Name = "Purchases", Description = "The total number of in-game transactions carried out by players in the specified hour." },
                // new PlayFabReportColumn { Name = "TotalCalls", Description = "The collective sum of player-initiated interactions, encompassing gameplay actions, API requests, and more." },
                // new PlayFabReportColumn { Name = "TotalSuccessfulCalls", Description = "The count of interactions that succeeded without encountering errors, highlighting player satisfaction." },
                // new PlayFabReportColumn { Name = "TotalErrors", Description = "The overall number of errors encountered during interactions, potential indicators of player experience challenges." },
                new PlayFabReportColumn { Name = "Arpu", Description = "Average Revenue Per User. The average revenue generated per unique player, calculated as Revenue / UniquePayers." },
                new PlayFabReportColumn { Name = "Arppu", Description = "Average Revenue Per Paying User. The average revenue generated per player who made purchases, calculated as Revenue / UniquePayers." },
                new PlayFabReportColumn { Name = "AvgPurchasePrice", Description = "The average price of in-game purchases made by players, calculated as Revenue / Purchases." },
                new PlayFabReportColumn { Name = "NewUsers", Description = "The count of new players who started engaging with the game during the specified hour period." },
            };

            playFabReports.Add(new()
            {
                Columns = rollingThirtyDayOverviewReportColumns,
                Description = "Daily data for the last 30 days capturing game reports for each day. The report has 30 rows where every row reprsents one the day of the last 30 days.",
                CsvData = PlayFabReport.CreateCsvReportFromJsonArray(rollingThirtyDayOverviewReport.ReportData, rollingThirtyDayOverviewReportColumns),
                ReportName = rollingThirtyDayOverviewReport.ReportName
            });
        }

        // Report 3 - Daily Top Items Report
        if (latestReports.TryGetValue("DailyTopItemsReport", out GameReport? dailyTopItemsReport))
        {
            string ParseItemName(string str) => str.Replace("[\"", "").Replace("\"]", "");
            PlayFabReportColumn[] dailyTopItemsReportColumns = new[]
            {
                new PlayFabReportColumn { Name = "ItemName", SourceParser=ParseItemName, Description = "The name of the product, representing a distinct item available for purchase." },
                new PlayFabReportColumn { Name = "TotalSales", Description = "The cumulative count of sales for the specific item, indicating its popularity and market demand." },
                new PlayFabReportColumn { Name = "TotalRevenue", Description = "The total monetary value of revenue generated from sales of the item in US dollars." },
            };

            playFabReports.Add(new()
            {
                Columns = dailyTopItemsReportColumns,
                Description = "The dataset provides an of a sales reports for last day, delivering total sales and total revenue for individual products.",
                CsvData = PlayFabReport.CreateCsvReportFromJsonArray(dailyTopItemsReport.ReportData, dailyTopItemsReportColumns),
                ReportName = dailyTopItemsReport.ReportName
            });
        }

        // Report 4 - Rolling 30 Day Retention Report
        if (latestReports.TryGetValue("ThirtyDayRetentionReport", out GameReport? thirtyDayRetentionReport))
        {
            PlayFabReportColumn[] thirtyDayRetentionReportColumns = new[]
            {
                new PlayFabReportColumn { Name = "CohortDate", SourceName="Ts", SourceParser=ParseDailyReportDate, Description = "The date indicating when the retention data was collected." },
                new PlayFabReportColumn { Name = "CohortSize", Description = "The initial size of the cohort, representing the number of players at the beginning of the retention period." },
                new PlayFabReportColumn { Name = "DaysLater", SourceName="PeriodsLater", Description = "The number of days later at which the retention is being measured." },
                new PlayFabReportColumn { Name = "TotalRetained", Description = "The total number of players retained in the specified cohort after the specified number of days." },
                new PlayFabReportColumn { Name = "PercentRetained", Description = "The percentage of players retained in the cohort after the specified number of days." },
            };

            playFabReports.Add(new()
            {
                Columns = thirtyDayRetentionReportColumns,
                Description = "Retention report for daily cohorts of players in the last 30 days.",
                CsvData = PlayFabReport.CreateCsvReportFromJsonArray(thirtyDayRetentionReport.ReportData, thirtyDayRetentionReportColumns),
                ReportName = thirtyDayRetentionReport.ReportName
            });
        }

        // Report 5 - Engagement Mertics Report
        if (latestReports.TryGetValue("EngagementMetricsRollupReportCSV", out GameReport? engagementMetricsRollupReport))
        {
            PlayFabReportColumn[] engagementMetricsRollupReportColumns = new[]
            {
                new PlayFabReportColumn { Name = "ReportDate", Description = "The date for the week for which the data is recorded." },
                new PlayFabReportColumn { Name = "Region", Description = "The geographic region to which the data pertains. Examples include Greater China, France, Japan, United Kingdom, United States, Latin America, India, Middle East & Africa, Germany, Canada, Western Europe, Asia Pacific, and Central & Eastern Europe. 'All' is a special region which means this rows aggregates data across all the other regions" },
                new PlayFabReportColumn { Name = "MonthlyActiveUsers", Description = "The total number of unique users who engaged with the game at least once during the month." },
                new PlayFabReportColumn { Name = "DailyActiveUsers", Description = "The total number of unique users who engaged with the game on that week." },
                new PlayFabReportColumn { Name = "NewPlayers", Description = "The number of new users who joined and engaged with the game on that week." },
                new PlayFabReportColumn { Name = "Retention1Day", Description = "The percentage of users who returned to the game on the day after their first engagement." },
                new PlayFabReportColumn { Name = "Retention7Day", Description = "The percentage of users who returned to the game seven days after their first engagement." },
            };

            playFabReports.Add(new()
            {
                Columns = engagementMetricsRollupReportColumns,
                Description = """
Weekly aggregated data related to the user activity and retention for the last 30 days.
Data is broken down by different geographic regions, including France, Greater China, Japan, United Kingdom, United States, Latin America, India, Middle East & Africa, Germany, Canada, Western Europe, Asia Pacific, and Central & Eastern Europe.
There is a special row for each week with the Region set to 'All', which means this row aggregates data across all the regions for that week.
""",
                CsvData = string.Join(
                Environment.NewLine,
                engagementMetricsRollupReport.ReportData
                    .Split("\"", StringSplitOptions.RemoveEmptyEntries)
                    .Where(line => line != ",")
                    .Select(line => line.Split(",", StringSplitOptions.RemoveEmptyEntries))
                    .Where(row => row[2] == "All" && row[4] == "All") // Platform and Segment
                    .Select(row => $"{ParseDailyReportDate(row[1])},{row[3]},{row[5]},{row[6]},{row[7]},{row[11]},{row[12]}")
                    .ToList()),
                ReportName = "EngagementMetricsRollupReport"
            });
        }

        return playFabReports.ToArray();
    }
}

public class KqlReportDataManager : IReportDataManager
{
    #region Data Members
    /// <summary>
    /// Caches the reports metadata
    /// </summary>
    private readonly AsyncPolicyWrap<PlayFabReport[]> _cachingPolicy;

    /// <summary>
    /// The Kusto cluster endpoint where the reports are
    /// </summary>
    private readonly string _adxClusterEndpoint;

    /// <summary>
    /// The Kusto database name where the reports are
    /// </summary>
    private readonly string _adxDatabaseName;
    #endregion

    #region Constructor
    /// <summary>
    /// Initializes a new instance of the <see cref="KqlReportDataManager"/> class.
    /// </summary>
    /// <param name="adxClusterEndpoint">The azure data explorer cluster endpoint</param>
    /// <param name="adxDatabaseName">The azure data explorer database name</param>
    public KqlReportDataManager(string adxClusterEndpoint, string adxDatabaseName)
    {
        this._adxClusterEndpoint = adxClusterEndpoint ?? throw new ArgumentNullException(nameof(adxClusterEndpoint));
        this._adxDatabaseName = adxDatabaseName ?? throw new ArgumentNullException(nameof(adxDatabaseName));

        // Configure Memory Cache
        var reportsMemoryCache = new MemoryCache(new MemoryCacheOptions());

        // Configure the MemoryCacheProvider with the MemoryCache instance
        var cacheProvider = new MemoryCacheProvider(reportsMemoryCache);

        // Configure the cache policy with a MemoryCacheProvider
        AsyncCachePolicy<PlayFabReport[]> cachePolicy = Policy.CacheAsync<PlayFabReport[]>(cacheProvider, TimeSpan.FromHours(1));
        AsyncFallbackPolicy<PlayFabReport[]> fallbackPolicy = Policy<PlayFabReport[]>.Handle<Exception>().FallbackAsync(new PlayFabReport[0]);

        this._cachingPolicy = Policy.WrapAsync(cachePolicy, fallbackPolicy);
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Get the reports for the specified title Id
    /// </summary>
    /// <param name="titleId">The title identifier</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>The reports for the specified title Id</returns>
    public async Task<IList<PlayFabReport>> GetPlayFabReportsAsync(string titleId, CancellationToken cancellationToken)
    {
        var context = new Context(titleId);
        context["TitleId"] = titleId;

        PlayFabReport[] playFabReports = await
            this._cachingPolicy
            .ExecuteAsync(
                (Context context, CancellationToken ct) => this.LoadPlayFabReportsMetadataAsync(context["TitleId"]?.ToString(), ct),
                context,
                cancellationToken);

        return playFabReports;
    }
    #endregion

    #region Private Methods
    /// <summary>
    /// Loads the playfab reports metadata
    /// </summary>
    /// <param name="titleId">The title identifier for which the reports are needed</param>
    /// <param name="cancellationToken">A cancellation token</param>
    /// <returns>The reports metadata</returns>
    private async Task<PlayFabReport[]> LoadPlayFabReportsMetadataAsync(string titleId, CancellationToken cancellationToken)
    {

        var playFabReports = new List<PlayFabReport>();

        // Report 1 - Daily Overview Report
        PlayFabReportColumn[] dailyOverviewReportColumns = new[]
        {
            new PlayFabReportColumn { Name = "ReportDate", Description = "The date for the day when the report was compiled, presented in Coordinated Universal Time (UTC)." },
            new PlayFabReportColumn { Name = "Ts", Description = "(Timestamp) The date and time of a one-hour window when the report was compiled, presented in Coordinated Universal Time (UTC)." },
            new PlayFabReportColumn { Name = "TotalLogins", Description = "The aggregate count of player logins during the specified hour, revealing the volume of player interactions." },
            new PlayFabReportColumn { Name = "UniqueLogins", Description = "The distinct number of players who logged into the game within the same hour, indicating individual engagement." },
            new PlayFabReportColumn { Name = "UniquePayers", Description = "The count of unique players who conducted in-game purchases, reflecting the game's monetization reach." },
            new PlayFabReportColumn { Name = "Revenue", Description = "The cumulative revenue in dollars generated from in-game purchases throughout the hour, demonstrating financial performance." },
            new PlayFabReportColumn { Name = "Purchases", Description = "The total number of in-game transactions carried out by players in the specified hour." },
            // new PlayFabReportColumn { Name = "TotalCalls", Description = "The collective sum of player-initiated interactions, encompassing gameplay actions, API requests, and more." },
            // new PlayFabReportColumn { Name = "TotalSuccessfulCalls", Description = "The count of interactions that succeeded without encountering errors, highlighting player satisfaction." },
            // new PlayFabReportColumn { Name = "TotalErrors", Description = "The overall number of errors encountered during interactions, potential indicators of player experience challenges." },
            new PlayFabReportColumn { Name = "Arpu", Description = "Average Revenue Per User, The average revenue generated per unique player, calculated as Revenue / UniquePayers." },
            new PlayFabReportColumn { Name = "Arppu", Description = "Average Revenue Per Paying User. The average revenue generated per player who made purchases, calculated as Revenue / UniquePayers." },
            new PlayFabReportColumn { Name = "AvgPurchasePrice", Description = "The average price of in-game purchases made by players, calculated as Revenue / Purchases." },
            new PlayFabReportColumn { Name = "NewUsers", Description = "The count of new players who started engaging with the game during the specified hour period." },
        };

        playFabReports.Add(new()
        {
            Columns = dailyOverviewReportColumns,
            Description = "Granular single day data capturing game reports for each hour. The report has 24 rows where every row reprsents one hour of the day.",
            KqlSafeData = $"{this.BuildTableReference("Report_Overview_Daily")} | where TitleId == '{titleId}'",
            ReportName = "Report_Overview_Daily"
        });


        // Report 2 - Rolling 30 Day Overview Report
        PlayFabReportColumn[] rollingThirtyDayOverviewReportColumns = new[]
        {
            new PlayFabReportColumn { Name = "ReportDate", Description = "The date of a one-day window when the report was compiled, presented in Coordinated Universal Time (UTC)." },
            new PlayFabReportColumn { Name = "TotalLogins", Description = "The aggregate count of player logins during the specified hour, revealing the volume of player interactions." },
            new PlayFabReportColumn { Name = "UniqueLogins", Description = "The distinct number of players who logged into the game within the same hour, indicating individual engagement." },
            new PlayFabReportColumn { Name = "UniquePayers", Description = "The count of unique players who conducted in-game purchases, reflecting the game's monetization reach." },
            new PlayFabReportColumn { Name = "Revenue", Description = "The cumulative revenue in dollars generated from in-game purchases throughout the hour, demonstrating financial performance." },
            new PlayFabReportColumn { Name = "Purchases", Description = "The total number of in-game transactions carried out by players in the specified hour." },
            // new PlayFabReportColumn { Name = "TotalCalls", Description = "The collective sum of player-initiated interactions, encompassing gameplay actions, API requests, and more." },
            // new PlayFabReportColumn { Name = "TotalSuccessfulCalls", Description = "The count of interactions that succeeded without encountering errors, highlighting player satisfaction." },
            // new PlayFabReportColumn { Name = "TotalErrors", Description = "The overall number of errors encountered during interactions, potential indicators of player experience challenges." },
            new PlayFabReportColumn { Name = "Arpu", Description = "Average Revenue Per User. The average revenue generated per unique player, calculated as Revenue / UniquePayers." },
            new PlayFabReportColumn { Name = "Arppu", Description = "Average Revenue Per Paying User. The average revenue generated per player who made purchases, calculated as Revenue / UniquePayers." },
            new PlayFabReportColumn { Name = "AvgPurchasePrice", Description = "The average price of in-game purchases made by players, calculated as Revenue / Purchases." },
            new PlayFabReportColumn { Name = "NewUsers", Description = "The count of new players who started engaging with the game during the specified hour period." },
        };

        playFabReports.Add(new()
        {
            Columns = rollingThirtyDayOverviewReportColumns,
            Description = "Daily data for the last 30 days capturing game reports for each day. The report has 30 rows where every row reprsents one the day of the last 30 days.",
            KqlSafeData = $"{this.BuildTableReference("Report_Rolling_Thirty_Day_Overview_Daily")} | where TitleId == '{titleId}'",
            ReportName = "Report_Rolling_Thirty_Day_Overview_Daily"
        });


        // Report 3 - Daily Top Items Report
        PlayFabReportColumn[] dailyTopItemsReportColumns = new[]
        {
            new PlayFabReportColumn { Name = "ReportDate", Description = "The date for the day for which the data is recorded" },
            new PlayFabReportColumn { Name = "ItemName", Description = "The name of the product, representing a distinct item available for purchase." },
            new PlayFabReportColumn { Name = "TotalSales", Description = "The cumulative count of sales for the specific item, indicating its popularity and market demand." },
            new PlayFabReportColumn { Name = "TotalRevenue", Description = "The total monetary value of revenue generated from sales of the item in US dollars." },
        };

        playFabReports.Add(new()
        {
            Columns = dailyTopItemsReportColumns,
            Description = "The dataset provides an of a sales reports for last day, delivering total sales and total revenue for individual products.",
            KqlSafeData = $"{this.BuildTableReference("Report_Top_Items_Daily")} | where TitleId == '{titleId}'",
            ReportName = "Report_Top_Items_Daily"
        });

        // Report 4 - Rolling 30 Day Retention Report
        PlayFabReportColumn[] thirtyDayRetentionReportColumns = new[]
        {
            new PlayFabReportColumn { Name = "ReportDate", SourceName="Ts", Description = "This is the CohortDate: The date indicating when the retention data was collected." },
            new PlayFabReportColumn { Name = "CohortSize", Description = "The initial size of the cohort, representing the number of players at the beginning of the retention period." },
            new PlayFabReportColumn { Name = "PeriodsLater", Description = "The number of days later at which the retention is being measured." },
            new PlayFabReportColumn { Name = "TotalRetained", Description = "The total number of players retained in the specified cohort after the specified number of days." },
            new PlayFabReportColumn { Name = "PercentRetained", Description = "The percentage of players retained in the cohort after the specified number of days." },
        };

        playFabReports.Add(new()
        {
            Columns = thirtyDayRetentionReportColumns,
            Description = "Retention report for daily cohorts of players in the last 30 days.",
            KqlSafeData = $"{this.BuildTableReference("Report_Thirty_Day_Retention_Daily")} | where PeriodsLater == 1 | where TitleId == '{titleId}'",
            ReportName = "Report_Thirty_Day_Retention_Daily"
        });

        // Report 5 - Engagement Mertics Report
        PlayFabReportColumn[] engagementMetricsRollupReportColumns = new[]
        {
            new PlayFabReportColumn { Name = "ReportDate", Description = "The date for the week for which the data is recorded." },
            new PlayFabReportColumn { Name = "Region", Description = "The geographic region to which the data pertains. Examples include Greater China, France, Japan, United Kingdom, United States, Latin America, India, Middle East & Africa, Germany, Canada, Western Europe, Asia Pacific, and Central & Eastern Europe. 'All' is a special region which means this rows aggregates data across all the other regions" },
            new PlayFabReportColumn { Name = "MAU", Description = "(MonthlyActiveUsers) The total number of unique users who engaged with the game at least once during the month." },
            new PlayFabReportColumn { Name = "DAU", Description = "(DailyActiveUsers) The total number of unique users who engaged with the game on that week." },
            new PlayFabReportColumn { Name = "NewPlayers", Description = "The number of new users who joined and engaged with the game on that week." },
            new PlayFabReportColumn { Name = "Retention1Day", Description = "The percentage of users who returned to the game on the day after their first engagement." },
            new PlayFabReportColumn { Name = "Retention7Day", Description = "The percentage of users who returned to the game seven days after their first engagement." },
        };

        playFabReports.Add(new()
        {
            Columns = engagementMetricsRollupReportColumns,
            Description = """
Weekly aggregated data related to the user activity and retention for the last 30 days.
Data is broken down by different geographic regions, including France, Greater China, Japan, United Kingdom, United States, Latin America, India, Middle East & Africa, Germany, Canada, Western Europe, Asia Pacific, and Central & Eastern Europe.
There is a special row for each week with the Region set to 'All', which means this row aggregates data across all the regions for that week.
""",
            KqlSafeData = $"{this.BuildTableReference("Report_Engagement_Metrics_Daily")} | where Platform == 'All' | where Segment == 'All' | where Region != 'Unknown' | where TitleId == '{titleId}'",
            ReportName = "Report_Engagement_Metrics_Daily"
        });

        return playFabReports.ToArray();
    }

    /// <summary>
    /// Gets the full table reference for the given table name.
    /// </summary>
    /// <param name="tableName">The name of the kql table</param>
    /// <returns>The full table reference for the given table name.</returns>
    private string BuildTableReference(string tableName)
    {
        string tableReference = $"cluster('{this._adxClusterEndpoint}').database('{this._adxDatabaseName}').{tableName}";
        return tableReference;
    } 
    #endregion
}
