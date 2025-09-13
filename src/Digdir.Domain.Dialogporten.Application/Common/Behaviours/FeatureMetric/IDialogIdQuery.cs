namespace Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;

/// <summary>
/// Marker interface for queries that operate on a specific dialog by ID
/// </summary>
public interface IDialogIdQuery
{
    /// <summary>
    /// The ID of the dialog being queried
    /// </summary>
    Guid DialogId { get; }
}