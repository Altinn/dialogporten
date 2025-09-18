namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

public interface IFeatureMetricDeliveryContext
{
    void ReportOutcome(string presentationTag, params IEnumerable<KeyValuePair<string, object>> additionalTags);
}
