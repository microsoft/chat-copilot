// Copyright (c) Microsoft. All rights reserved.

namespace PlayFab.Reports;
public class GameReport
{
    public DateTime ReportDate { get; set; }
    public required string ReportId { get; set; }
    public required string ReportName { get; set; }
    public required string ReportData { get; set; }
}

