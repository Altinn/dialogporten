using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Delete;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Purge;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;

using GetDialogQueryEU = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get.GetDialogQuery;
using SearchDialogsQueryEU = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search.SearchDialogQuery;
using SearchDialogsQueryEUOld = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchOld.SearchDialogQuery;
using SetSystemLabelCommandEU = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Commands.SetSystemLabel.SetSystemLabelCommand;
using BulkSetSystemLabelCommandEU = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Commands.BulkSetSystemLabels.BulkSetSystemLabelCommand;

using GetDialogQuerySO = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get.GetDialogQuery;
using SearchDialogsQuerySO = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search.SearchDialogQuery;
using SetSystemLabelCommandSO = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.EndUserContext.Commands.SetSystemLabels.SetSystemLabelCommand;
using BulkSetSystemLabelCommandSO = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.EndUserContext.Commands.BulkSetSystemLabels.BulkSetSystemLabelCommand;

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
        [typeof(CreateDialogCommand).FullName!] = TransactionType.CreateDialog,
        [typeof(UpdateDialogCommand).FullName!] = TransactionType.UpdateDialog,
        [typeof(DeleteDialogCommand).FullName!] = TransactionType.SoftDeleteDialog,
        [typeof(PurgeDialogCommand).FullName!] = TransactionType.HardDeleteDialog,

        // ServiceOwner Get/Search
        [typeof(GetDialogQuerySO).FullName!] = TransactionType.GetDialogServiceOwner,
        [typeof(SearchDialogsQuerySO).FullName!] = TransactionType.SearchDialogsServiceOwner,

        // EndUser Get/Search
        [typeof(GetDialogQueryEU).FullName!] = TransactionType.GetDialogEndUser,
        [typeof(SearchDialogsQueryEU).FullName!] = TransactionType.SearchDialogsEndUser,
        [typeof(SearchDialogsQueryEUOld).FullName!] = TransactionType.SearchDialogsEndUser,

        // Labels - exact matches needed for these
        [typeof(SetSystemLabelCommandEU).FullName!] = TransactionType.SetDialogLabel,
        [typeof(BulkSetSystemLabelCommandEU).FullName!] = TransactionType.BulkSetLabelsEndUser,
        [typeof(SetSystemLabelCommandSO).FullName!] = TransactionType.SetDialogLabel,
        [typeof(BulkSetSystemLabelCommandSO).FullName!] = TransactionType.BulkSetLabelsServiceOwnerWithEndUser,
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

    private static bool HasEndUserParameter(string presentationTag) =>
        presentationTag.Contains(nameof(SearchDialogsQuerySO.EndUserId), StringComparison.OrdinalIgnoreCase);
}
