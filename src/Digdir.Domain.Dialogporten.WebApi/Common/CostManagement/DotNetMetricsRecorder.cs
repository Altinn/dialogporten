using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Digdir.Domain.Dialogporten.WebApi.Common.CostManagement;

/// <summary>
/// Implementation of IMetricsRecorder using .NET System.Diagnostics.Metrics
/// </summary>
internal sealed class DotNetMetricsRecorder : IMetricsRecorder
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

    public void RecordTransactionCounter(long value, in TagList tags)
    {
        _transactionCounter.Add(value, tags);
    }

}