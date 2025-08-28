using System.Diagnostics;
using System.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;

namespace Digdir.Domain.Dialogporten.WebApi.Common.CostManagement;

/// <summary>
/// Internal service for recording cost management metrics to OpenTelemetry.
/// Used exclusively by CostManagementBackgroundService to process queued transactions.
/// </summary>
public sealed class CostManagementMetricsService : IDisposable
{
    private readonly Counter<long> _transactionCounter;
    private readonly Meter _meter;
    private readonly string _environment;

    public CostManagementMetricsService(IHostEnvironment hostEnvironment)
    {
        _environment = hostEnvironment.EnvironmentName;
        _meter = new Meter("Dialogporten.CostManagement", "1.0.0");
        _transactionCounter = _meter.CreateCounter<long>(
            CostManagementConstants.TransactionCounterName,
            description: CostManagementConstants.TransactionCounterDescription);
    }

    public void RecordTransaction(TransactionType transactionType, int httpStatusCode, string? tokenOrg = null, string? serviceOrg = null, string? serviceResource = null)
    {
        var status = GetStatusFromHttpCode(httpStatusCode);

        // Only record metrics for 2xx (success) and 4xx (client errors)
        // 5xx errors should not incur costs according to requirements
        if (status == null)
        {
            return;
        }

        // Use TagList to avoid array allocation on each call
        var tags = new TagList
        {
            { CostManagementConstants.TransactionTypeTag, transactionType.ToString() },
            { CostManagementConstants.StatusTag, status },
            { CostManagementConstants.HttpStatusCodeTag, httpStatusCode },
            { CostManagementConstants.EnvironmentTag, _environment },
            { CostManagementConstants.TokenOrgTag, tokenOrg ?? CostManagementConstants.UnknownValue },
            { CostManagementConstants.ServiceOrgTag, serviceOrg ?? CostManagementConstants.UnknownValue },
            { CostManagementConstants.ServiceResourceTag, serviceResource ?? CostManagementConstants.UnknownValue }
        };

        _transactionCounter.Add(1, tags);
    }

    private static string? GetStatusFromHttpCode(int httpStatusCode)
    {
        return httpStatusCode switch
        {
            >= 200 and < 300 => CostManagementConstants.StatusSuccess,
            >= 400 and < 500 => CostManagementConstants.StatusFailed,
            _ => null // Don't record 5xx or other status codes
        };
    }

    public void Dispose()
    {
        _meter.Dispose();
    }
}
