namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

public interface IFeatureMetricDeliveryContext
{
    void Ack(string presentationTag);
    void Nack(string presentationTag);
}
