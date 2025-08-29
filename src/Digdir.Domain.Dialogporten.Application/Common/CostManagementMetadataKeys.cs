namespace Digdir.Domain.Dialogporten.Application.Common;

/// <summary>
/// Constants for cost management metadata keys used in IApplicationContext
/// </summary>
public static class CostManagementMetadataKeys
{
    /// <summary>
    /// Metadata key for service organization
    /// </summary>
    public const string ServiceOrg = "serviceOrg";

    /// <summary>
    /// Metadata key for service resource
    /// </summary>
    public const string ServiceResource = "serviceResource";

    /// <summary>
    /// Sentinel value for search operations (cannot attribute to specific org/resource)
    /// </summary>
    public const string SearchOperation = "search_operation";

    /// <summary>
    /// Sentinel value for bulk operations with mixed entities
    /// </summary>
    public const string BulkOperation = "bulk_operation";

    /// <summary>
    /// Sentinel value for operations where org/resource attribution is not applicable
    /// </summary>
    public const string NotApplicable = "not_applicable";

}
