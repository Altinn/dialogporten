namespace Digdir.Domain.Dialogporten.WebApi.Common.CostManagement;

/// <summary>
/// Represents the different transaction types for cost management metrics
/// </summary>
public enum TransactionType
{
    /// <summary>
    /// Create dialog operation (Opprette dialog)
    /// </summary>
    CreateDialog,

    /// <summary>
    /// Update dialog operation (Oppdatere dialog) 
    /// </summary>
    UpdateDialog,

    /// <summary>
    /// Soft delete dialog operation (Softslette dialog)
    /// </summary>
    SoftDeleteDialog,

    /// <summary>
    /// Hard delete/purge dialog operation (Hardslette dialog)
    /// </summary>
    HardDeleteDialog,

    /// <summary>
    /// Get dialog by service owner (Hente dialog tjenesteeier)
    /// </summary>
    GetDialogServiceOwner,

    /// <summary>
    /// Service owner search dialogs (Tjenesteeiersøk)
    /// </summary>
    SearchDialogsServiceOwner,

    /// <summary>
    /// Service owner search dialogs with end user ID (Tjenesteeiersøk m/sluttbruker-id)
    /// </summary>
    SearchDialogsServiceOwnerWithEndUser,

    /// <summary>
    /// Get dialog by end user (Hente dialog sluttbruker)
    /// </summary>
    GetDialogEndUser,

    /// <summary>
    /// Set label on single dialog (Sette label på enkeltdialog)
    /// </summary>
    SetDialogLabel,

    /// <summary>
    /// Bulk set labels via service owner API with end user ID
    /// </summary>
    BulkSetLabelsServiceOwnerWithEndUser,

    /// <summary>
    /// End user search dialogs (Sluttbrukersøk) - no service code
    /// </summary>
    SearchDialogsEndUser,

    /// <summary>
    /// Bulk set labels via end user API - no service code
    /// </summary>
    BulkSetLabelsEndUser
}

/// <summary>
/// Constants for cost management metrics
/// </summary>
public static class CostManagementConstants
{
    /// <summary>
    /// The name of the counter metric for transactions
    /// </summary>
    public const string TransactionCounterName = "dialogporten_transactions_total";

    /// <summary>
    /// Description of the transaction counter metric
    /// </summary>
    public const string TransactionCounterDescription = "Total number of dialog transactions for cost management";

    /// <summary>
    /// Tag name for transaction type
    /// </summary>
    public const string TransactionTypeTag = "transaction_type";

    /// <summary>
    /// Tag name for organization short name from token
    /// </summary>
    public const string TokenOrgTag = "token_org";

    /// <summary>
    /// Tag name for organization short name from dialog entity
    /// </summary>
    public const string ServiceOrgTag = "service_org";

    /// <summary>
    /// Tag name for service resource type from dialog entity
    /// </summary>
    public const string ServiceResourceTag = "service_resource";

    /// <summary>
    /// Tag name for success/failure status
    /// </summary>
    public const string StatusTag = "status";

    /// <summary>
    /// Tag name for HTTP status code
    /// </summary>
    public const string HttpStatusCodeTag = "http_status_code";

    /// <summary>
    /// Tag name for environment
    /// </summary>
    public const string EnvironmentTag = "environment";

    /// <summary>
    /// Status value for successful operations (2xx)
    /// </summary>
    public const string StatusSuccess = "success";

    /// <summary>
    /// Status value for failed operations (4xx)
    /// </summary>
    public const string StatusFailed = "failed";

    /// <summary>
    /// Value used when no organization can be determined
    /// </summary>
    public const string NoOrgValue = "null";
}
