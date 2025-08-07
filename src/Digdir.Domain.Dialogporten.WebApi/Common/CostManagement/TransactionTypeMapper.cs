using System.Text.RegularExpressions;

namespace Digdir.Domain.Dialogporten.WebApi.Common.CostManagement;

/// <summary>
/// Service to map HTTP requests to transaction types for cost management
/// </summary>
public interface ITransactionTypeMapper
{
    /// <summary>
    /// Determines the transaction type based on the HTTP request
    /// </summary>
    /// <param name="method">HTTP method</param>
    /// <param name="path">Request path</param>
    /// <param name="hasEndUserId">Whether the request has an end user ID parameter</param>
    /// <returns>Transaction type if mappable, null otherwise</returns>
    TransactionType? GetTransactionType(string method, string path, bool hasEndUserId = false);
}

/// <summary>
/// Implementation of transaction type mapper
/// </summary>
public sealed class TransactionTypeMapper : ITransactionTypeMapper
{
    private readonly List<RoutePattern> _patterns;

    public TransactionTypeMapper()
    {
        _patterns = new List<RoutePattern>
        {
            // Service Owner operations
            new("POST", @"^/api/v1/serviceowner/dialogs$", TransactionType.CreateDialog),
            new("PUT", @"^/api/v1/serviceowner/dialogs/[^/]+$", TransactionType.UpdateDialog),
            new("PATCH", @"^/api/v1/serviceowner/dialogs/[^/]+$", TransactionType.UpdateDialog),
            new("DELETE", @"^/api/v1/serviceowner/dialogs/[^/]+$", TransactionType.SoftDeleteDialog),
            new("DELETE", @"^/api/v1/serviceowner/dialogs/[^/]+/purge$", TransactionType.HardDeleteDialog),
            new("POST", @"^/api/v1/serviceowner/dialogs/[^/]+/restore$", TransactionType.UpdateDialog), // Restore is an update operation
            new("GET", @"^/api/v1/serviceowner/dialogs/[^/]+$", TransactionType.GetDialogServiceOwner),
            new("GET", @"^/api/v1/serviceowner/dialogs$", TransactionType.SearchDialogsServiceOwner, true, TransactionType.SearchDialogsServiceOwnerWithEndUser),
            
            // Service Owner label operations (considered as set label)
            new("POST", @"^/api/v1/serviceowner/dialogs/[^/]+/context/labels$", TransactionType.SetDialogLabel),
            new("DELETE", @"^/api/v1/serviceowner/dialogs/[^/]+/context/labels/[^/]+$", TransactionType.SetDialogLabel),
            new("PUT", @"^/api/v1/serviceowner/context/bulkupdate$", TransactionType.BulkSetLabelsServiceOwnerWithEndUser),
            
            // End User operations  
            new("GET", @"^/api/v1/enduser/dialogs/[^/]+$", TransactionType.GetDialogEndUser),
            new("GET", @"^/api/v1/enduser/dialogs$", TransactionType.SearchDialogsEndUser),
            
            // End User label operations
            new("PUT", @"^/api/v1/enduser/dialogs/[^/]+/context/systemlabels$", TransactionType.SetDialogLabel),
            new("PUT", @"^/api/v1/enduser/context/bulkupdate$", TransactionType.BulkSetLabelsEndUser),
        };
    }

    public TransactionType? GetTransactionType(string method, string path, bool hasEndUserId = false)
    {
        foreach (var pattern in _patterns)
        {
            if (pattern.Method.Equals(method, StringComparison.OrdinalIgnoreCase) &&
                Regex.IsMatch(path, pattern.PathPattern, RegexOptions.IgnoreCase))
            {
                // Check if this pattern has special handling for endUserId parameter
                if (pattern.HasEndUserIdVariant && hasEndUserId && pattern.EndUserIdTransactionType.HasValue)
                {
                    return pattern.EndUserIdTransactionType.Value;
                }

                return pattern.TransactionType;
            }
        }

        return null;
    }

    private sealed record RoutePattern(
        string Method,
        string PathPattern,
        TransactionType TransactionType,
        bool HasEndUserIdVariant = false,
        TransactionType? EndUserIdTransactionType = null);
}
