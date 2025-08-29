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
        // Middleware ensures only 2xx and 4xx reach this method
        var status = httpStatusCode is >= 200 and < 300
            ? CostManagementConstants.StatusSuccess
            : CostManagementConstants.StatusFailed;

        // Use TagList to avoid array allocation on each call
        var tags = new TagList
        {
            { CostManagementConstants.TransactionTypeTag, transactionType.ToString() },
            { CostManagementConstants.StatusTag, status },
            { CostManagementConstants.HttpStatusCodeTag, httpStatusCode },
            { CostManagementConstants.EnvironmentTag, _environment },
            { CostManagementConstants.TokenOrgTag, NormalizeTag(tokenOrg) },
            { CostManagementConstants.ServiceOrgTag, NormalizeTag(serviceOrg) },
            { CostManagementConstants.ServiceResourceTag, NormalizeTag(serviceResource) }
        };

        _metricsRecorder.RecordTransactionCounter(1, tags);
    }

    private static string NormalizeTag(string? value) => 
        string.IsNullOrWhiteSpace(value) ? CostManagementConstants.UnknownValue : value;
}
