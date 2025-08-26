using System.Diagnostics.Metrics;
using Microsoft.Extensions.Hosting;

namespace Digdir.Domain.Dialogporten.WebApi.Common.CostManagement;

/// <summary>
/// Implementation of cost management metrics service using OpenTelemetry
/// </summary>
public sealed class CostManagementMetricsService : ICostManagementMetricsService, IDisposable
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

        var tags = new KeyValuePair<string, object?>[]
        {
            new(CostManagementConstants.TransactionTypeTag, transactionType.ToString()),
            new(CostManagementConstants.StatusTag, status),
            new(CostManagementConstants.HttpStatusCodeTag, httpStatusCode),
            new(CostManagementConstants.EnvironmentTag, _environment),
            new(CostManagementConstants.TokenOrgTag, tokenOrg ?? CostManagementConstants.UnknownValue),
            new(CostManagementConstants.ServiceOrgTag, serviceOrg ?? CostManagementConstants.UnknownValue),
            new(CostManagementConstants.ServiceResourceTag, serviceResource ?? CostManagementConstants.UnknownValue)
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
