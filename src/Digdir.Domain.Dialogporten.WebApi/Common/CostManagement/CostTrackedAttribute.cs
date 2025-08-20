namespace Digdir.Domain.Dialogporten.WebApi.Common.CostManagement;

/// <summary>
/// Attribute to mark endpoints that should be tracked for cost management metrics.
/// Applying this attribute makes the middleware record a transaction for the specified type.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class CostTrackedAttribute : Attribute
{
    /// <summary>
    /// The transaction type this endpoint represents for cost management.
    /// </summary>
    public TransactionType TransactionType { get; }

    /// <summary>
    /// Optional query parameter that, when present, changes the transaction type.
    /// Used for endpoints that behave differently based on query parameters.
    /// </summary>
    public string? QueryParameterVariant { get; }

    /// <summary>
    /// The transaction type to use when the query parameter variant is present.
    /// </summary>
    public TransactionType VariantTransactionType { get; }

    /// <summary>
    /// Whether this endpoint has a query parameter variant.
    /// </summary>
    public bool HasVariant => !string.IsNullOrEmpty(QueryParameterVariant);

    public CostTrackedAttribute(TransactionType transactionType)
    {
        TransactionType = transactionType;
        QueryParameterVariant = null;
        VariantTransactionType = transactionType; // Default to same type
    }

    public CostTrackedAttribute(TransactionType transactionType, string queryParameterVariant, TransactionType variantTransactionType)
    {
        TransactionType = transactionType;
        QueryParameterVariant = queryParameterVariant ?? throw new ArgumentNullException(nameof(queryParameterVariant));
        VariantTransactionType = variantTransactionType;
    }
}


