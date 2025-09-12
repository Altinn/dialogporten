namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

public interface IFeatureMetricDeliveryContext
{
    void Ack(string presentationTag);
    void Nack(string presentationTag);
    void Abandon();
}

internal interface IFeatureMetricRecorder
{
    void Record(FeatureMetricRecord record);
}

internal sealed record FeatureMetricRecord(
    string TransactionName,
    string Environment,
    string? PerformerOrg = null,
    string? OwnerOrg = null,
    string? ServiceResource = null);
