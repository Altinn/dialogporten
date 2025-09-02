namespace Digdir.Domain.Dialogporten.Janitor.Services;

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
