namespace Digdir.Domain.Dialogporten.Janitor.Services;

public static class CostCoefficients
{
    public static readonly Dictionary<TransactionType, decimal> Coefficients = new()
    {
        { TransactionType.CreateDialog, 1.5m },
        { TransactionType.UpdateDialog, 1.2m },
        { TransactionType.SoftDeleteDialog, 1.0m },
        { TransactionType.HardDeleteDialog, 1.0m },
        { TransactionType.GetDialogServiceOwner, 1.0m },
        { TransactionType.GetDialogEndUser, 1.0m },
        { TransactionType.SearchDialogsServiceOwner, 2.0m },
        { TransactionType.SearchDialogsServiceOwnerWithEndUser, 2.2m },
        { TransactionType.SearchDialogsEndUser, 2.5m },
        { TransactionType.SetDialogLabel, 1.0m },
        { TransactionType.BulkSetLabelsServiceOwnerWithEndUser, 3.0m },
        { TransactionType.BulkSetLabelsEndUser, 3.5m }
    };

    public static decimal GetCoefficient(TransactionType transactionType)
    {
        return Coefficients.TryGetValue(transactionType, out var coefficient) ? coefficient : 1.0m;
    }

    public static string GetNorwegianName(TransactionType transactionType)
    {
        return transactionType switch
        {
            TransactionType.CreateDialog => "Opprette dialog",
            TransactionType.UpdateDialog => "Oppdatere dialog",
            TransactionType.SoftDeleteDialog => "Softslette dialog",
            TransactionType.HardDeleteDialog => "Hardslette dialog",
            TransactionType.GetDialogServiceOwner => "Hente dialog tjenesteeier",
            TransactionType.SearchDialogsServiceOwner => "Tjenesteeiersøk",
            TransactionType.SearchDialogsServiceOwnerWithEndUser => "Tjenesteeiersøk m/sluttbruker-id",
            TransactionType.GetDialogEndUser => "Hente dialog sluttbruker",
            TransactionType.SetDialogLabel => "Sette label på enkeltdialog",
            TransactionType.BulkSetLabelsServiceOwnerWithEndUser => "Bulk label setting tjenesteeier m/sluttbruker-id",
            TransactionType.SearchDialogsEndUser => "Sluttbrukersøk",
            TransactionType.BulkSetLabelsEndUser => "Bulk label setting sluttbruker",
            _ => transactionType.ToString()
        };
    }
}
