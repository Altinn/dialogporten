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
    private static readonly Dictionary<string, TransactionType> FeatureTypeMap = new()
    {
        // Commands
        ["Commands.Create.CreateDialogCommand"] = TransactionType.CreateDialog,
        ["Commands.Update.UpdateDialogCommand"] = TransactionType.UpdateDialog,
        ["Commands.Delete.DeleteDialogCommand"] = TransactionType.SoftDeleteDialog,
        ["Commands.Purge.PurgeDialogCommand"] = TransactionType.HardDeleteDialog,

        // ServiceOwner Get/Search
        ["ServiceOwner.Dialogs.Queries.Get.GetDialogQuery"] = TransactionType.GetDialogServiceOwner,
        ["ServiceOwner.Dialogs.Queries.Search.SearchDialogQuery"] = TransactionType.SearchDialogsServiceOwner,

        // EndUser Get/Search
        ["EndUser.Dialogs.Queries.Get.GetDialogQuery"] = TransactionType.GetDialogEndUser,
        ["EndUser.Dialogs.Queries.Search.SearchDialogQuery"] = TransactionType.SearchDialogsEndUser,

        // Labels - exact matches needed for these
        ["EndUser.EndUserContext.Commands.SetSystemLabel.SetSystemLabelCommand"] = TransactionType.SetDialogLabel,
        ["ServiceOwner.EndUserContext.Commands.SetSystemLabel.SetSystemLabelCommand"] = TransactionType.SetDialogLabel,
        ["ServiceOwner.EndUserContext.Commands.BulkSetSystemLabels.BulkSetSystemLabelCommand"] = TransactionType.BulkSetLabelsServiceOwnerWithEndUser,
        ["EndUser.EndUserContext.Commands.BulkSetSystemLabels.BulkSetSystemLabelCommand"] = TransactionType.BulkSetLabelsEndUser,
    };

    public static TransactionType? MapFeatureTypeToTransactionType(string featureType, string presentationTag)
    {
        foreach (var (key, value) in FeatureTypeMap)
        {
            if (featureType.Contains(key, StringComparison.Ordinal))
            {
                // Special case: ServiceOwner Search with EndUser parameter
                if (value == TransactionType.SearchDialogsServiceOwner &&
                    HasEndUserParameter(presentationTag))
                {
                    return TransactionType.SearchDialogsServiceOwnerWithEndUser;
                }

                return value;
            }
        }

        // Unknown feature types should be silently ignored - they're not part of cost aggregation
        return null;
    }

    // TODO: Currently not working as expected, see issue #2871
    private static bool HasEndUserParameter(string presentationTag) =>
        presentationTag.Contains("endUserId", StringComparison.OrdinalIgnoreCase);
}
