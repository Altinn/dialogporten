using System.Diagnostics;
using System.Diagnostics.Metrics;

namespace Digdir.Domain.Dialogporten.WebApi.Common.CostManagement;

/// <summary>
/// Implementation of IMetricsRecorder using .NET System.Diagnostics.Metrics
/// </summary>
internal sealed class DotNetMetricsRecorder : IMetricsRecorder
{
    private readonly Counter<long> _transactionCounter;

    public DotNetMetricsRecorder(Meter meter)
    {
        ArgumentNullException.ThrowIfNull(meter);
        _transactionCounter = meter.CreateCounter<long>(
            CostManagementConstants.TransactionCounterName,
            description: CostManagementConstants.TransactionCounterDescription);
    }

    public void RecordTransactionCounter(long value, in TagList tags)
    {
        _transactionCounter.Add(value, tags);
    }

}