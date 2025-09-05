namespace Digdir.Domain.Dialogporten.WebApi.Common.CostManagement;

/// <summary>
/// Record representing a transaction to be processed by the background metrics service
/// </summary>
public readonly record struct TransactionRecord(
    TransactionType TransactionType,
    int HttpStatusCode,
    string? TokenOrg = null,
    string? ServiceOrg = null,
    string? ServiceResource = null);
