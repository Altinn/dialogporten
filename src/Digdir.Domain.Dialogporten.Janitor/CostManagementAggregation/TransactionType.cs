namespace Digdir.Domain.Dialogporten.Janitor.CostManagementAggregation;

/// <summary>
/// Transaction types for cost management metrics aggregation
/// </summary>
public enum TransactionType
{
    CreateDialog,
    UpdateDialog,
    SoftDeleteDialog,
    HardDeleteDialog,
    GetDialogServiceOwner,
    SearchDialogsServiceOwner,
    SearchDialogsServiceOwnerWithEndUser,
    GetDialogEndUser,
    SetDialogLabel,
    BulkSetLabelsServiceOwnerWithEndUser,
    SearchDialogsEndUser,
    BulkSetLabelsEndUser
}

/// <summary>
/// Helper methods for mapping feature types to transaction types
/// </summary>
public static class TransactionTypeMapper
{
    public static TransactionType? MapFeatureTypeToTransactionType(string featureType, string presentationTag)
    {
        // Handle Commands
        if (featureType.Contains("Commands.Create.CreateDialogCommand", StringComparison.Ordinal))
        {
            return TransactionType.CreateDialog;
        }

        if (featureType.Contains("Commands.Update.UpdateDialogCommand", StringComparison.Ordinal))
        {
            return TransactionType.UpdateDialog;
        }

        if (featureType.Contains("Commands.Delete.DeleteDialogCommand", StringComparison.Ordinal))
        {
            return TransactionType.SoftDeleteDialog;
        }

        if (featureType.Contains("Commands.Purge.PurgeDialogCommand", StringComparison.Ordinal))
        {
            return TransactionType.HardDeleteDialog;
        }

        // Handle Get operations
        if (featureType.Contains("ServiceOwner.Dialogs.Queries.Get.GetDialogQuery", StringComparison.Ordinal))
        {
            return TransactionType.GetDialogServiceOwner;
        }

        if (featureType.Contains("EndUser.Dialogs.Queries.Get.GetDialogQuery", StringComparison.Ordinal))
        {
            return TransactionType.GetDialogEndUser;
        }

        // Handle Search operations
        if (featureType.Contains("ServiceOwner.Dialogs.Queries.Search.SearchDialogQuery", StringComparison.Ordinal))
        {
            // Check if endUserId parameter is present in presentation tag to distinguish
            // SearchDialogsServiceOwnerWithEndUser from SearchDialogsServiceOwner
            if (presentationTag.Contains("endUserId", StringComparison.OrdinalIgnoreCase) ||
                presentationTag.Contains("EndUser", StringComparison.OrdinalIgnoreCase))
            {
                return TransactionType.SearchDialogsServiceOwnerWithEndUser;
            }
            return TransactionType.SearchDialogsServiceOwner;
        }

        if (featureType.Contains("EndUser.Dialogs.Queries.Search.SearchDialogQuery", StringComparison.Ordinal))
        {
            return TransactionType.SearchDialogsEndUser;
        }

        // Handle Label operations
        if (featureType.Contains("EndUser.EndUserContext.Commands.SetSystemLabel.SetSystemLabelCommand", StringComparison.Ordinal) ||
            featureType.Contains("ServiceOwner.EndUserContext.Commands.SetSystemLabel.SetSystemLabelCommand", StringComparison.Ordinal))
        {
            return TransactionType.SetDialogLabel;
        }

        if (featureType.Contains("ServiceOwner.EndUserContext.Commands.BulkSetSystemLabels.BulkSetSystemLabelCommand", StringComparison.Ordinal))
        {
            return TransactionType.BulkSetLabelsServiceOwnerWithEndUser;
        }

        if (featureType.Contains("EndUser.EndUserContext.Commands.BulkSetSystemLabels.BulkSetSystemLabelCommand", StringComparison.Ordinal))
        {
            return TransactionType.BulkSetLabelsEndUser;
        }

        // No matching transaction type - this feature metric should not be included in cost aggregation
        return null;
    }
}
