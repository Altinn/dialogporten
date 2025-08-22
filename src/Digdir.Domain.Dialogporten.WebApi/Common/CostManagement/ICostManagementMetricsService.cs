namespace Digdir.Domain.Dialogporten.WebApi.Common.CostManagement;

/// <summary>
/// Service for recording cost management metrics
/// </summary>
public interface ICostManagementMetricsService
{
    /// <summary>
    /// Records a transaction for cost management metrics
    /// </summary>
    /// <param name="transactionType">The type of transaction</param>
    /// <param name="httpStatusCode">The HTTP status code of the response</param>
    /// <param name="orgIdentifier">Organization identifier from JWT token (optional)</param>
    /// <param name="orgName">Organization name from dialog entity (optional)</param>
    /// <param name="serviceResource">Service resource type from dialog entity (optional)</param>
    void RecordTransaction(TransactionType transactionType, int httpStatusCode, string? orgIdentifier = null, string? orgName = null, string? serviceResource = null);
}
