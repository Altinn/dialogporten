namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

public interface IFeatureMetricDeliveryContext
{
    void Ack(string presentationTag, params IEnumerable<KeyValuePair<string, string>> additionalTags);
    void Nack(string presentationTag, params IEnumerable<KeyValuePair<string, string>> additionalTags);
}
