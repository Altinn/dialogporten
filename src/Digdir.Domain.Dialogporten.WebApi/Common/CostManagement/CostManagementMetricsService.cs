using System.Diagnostics;
using Microsoft.Extensions.Hosting;

namespace Digdir.Domain.Dialogporten.WebApi.Common.CostManagement;

/// <summary>
/// Internal service for recording cost management metrics to OpenTelemetry.
/// Used exclusively by CostManagementBackgroundService to process queued transactions.
/// </summary>
internal sealed class CostManagementMetricsService : ICostManagementTransactionRecorder
{
    private readonly IMetricsRecorder _metricsRecorder;
    private readonly string _environment;

    public CostManagementMetricsService(IMetricsRecorder metricsRecorder, IHostEnvironment hostEnvironment)
    {
        _metricsRecorder = metricsRecorder ?? throw new ArgumentNullException(nameof(metricsRecorder));
        _environment = (hostEnvironment ?? throw new ArgumentNullException(nameof(hostEnvironment))).EnvironmentName;
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

        _metricsRecorder.RecordTransactionCounter(1, tags);
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

}
