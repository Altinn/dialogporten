namespace Digdir.Domain.Dialogporten.WebApi.Common.CostManagement;

/// <summary>
/// Service for recording cost management metrics
/// </summary>
public interface ICostManagementMetricsService
{
    /// <summary>
    /// Records a transaction metric for cost management
    /// </summary>
    /// <param name="transactionType">The type of transaction</param>
    /// <param name="httpStatusCode">The HTTP status code of the response</param>
    /// <param name="orgIdentifier">The organization identifier (null for end user searches)</param>
    void RecordTransaction(TransactionType transactionType, int httpStatusCode, string? orgIdentifier = null);
}
