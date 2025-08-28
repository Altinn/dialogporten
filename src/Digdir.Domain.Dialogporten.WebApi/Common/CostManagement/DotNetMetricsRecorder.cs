using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Digdir.Domain.Dialogporten.WebApi.Common.CostManagement;

/// <summary>
/// Implementation of IMetricsRecorder using .NET System.Diagnostics.Metrics
/// </summary>
public sealed class DotNetMetricsRecorder : IMetricsRecorder
{
    private readonly Meter _meter;
    private readonly Counter<long> _transactionCounter;

    public DotNetMetricsRecorder(Meter meter)
    {
        _meter = meter ?? throw new ArgumentNullException(nameof(meter));
        _transactionCounter = _meter.CreateCounter<long>(
            CostManagementConstants.TransactionCounterName,
            description: CostManagementConstants.TransactionCounterDescription);
    }

    public void RecordCounter(string name, long value, TagList tags)
    {
        // For now we only support the transaction counter, but this could be extended
        if (name == CostManagementConstants.TransactionCounterName)
        {
            _transactionCounter.Add(value, tags);
        }
    }

}