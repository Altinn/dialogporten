using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Janitor.CostManagementAggregation;

public sealed class CostCoefficients
{
    private readonly CostCoefficientsOptions _options;
    private readonly Dictionary<TransactionType, decimal> _coefficients;

    public CostCoefficients(IOptions<CostCoefficientsOptions> options)
    {
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _coefficients = new Dictionary<TransactionType, decimal>
        {
            { TransactionType.CreateDialog, _options.CreateDialog },
            { TransactionType.UpdateDialog, _options.UpdateDialog },
            { TransactionType.SoftDeleteDialog, _options.SoftDeleteDialog },
            { TransactionType.HardDeleteDialog, _options.HardDeleteDialog },
            { TransactionType.GetDialogServiceOwner, _options.GetDialogServiceOwner },
            { TransactionType.GetDialogEndUser, _options.GetDialogEndUser },
            { TransactionType.SearchDialogsServiceOwner, _options.SearchDialogsServiceOwner },
            { TransactionType.SearchDialogsServiceOwnerWithEndUser, _options.SearchDialogsServiceOwnerWithEndUser },
            { TransactionType.SearchDialogsEndUser, _options.SearchDialogsEndUser },
            { TransactionType.SetDialogLabel, _options.SetDialogLabel },
            { TransactionType.BulkSetLabelsServiceOwnerWithEndUser, _options.BulkSetLabelsServiceOwnerWithEndUser },
            { TransactionType.BulkSetLabelsEndUser, _options.BulkSetLabelsEndUser }
        };
    }

    public decimal GetCoefficient(TransactionType transactionType)
    {
        return _coefficients.TryGetValue(transactionType, out var coefficient) ? coefficient : 1.0m;
    }

    public static string GetNorwegianName(TransactionType transactionType)
    {
        return transactionType switch
        {
            TransactionType.CreateDialog => "Opprette dialog",
            TransactionType.UpdateDialog => "Oppdatere dialog",
            TransactionType.SoftDeleteDialog => "Slette dialog",
            TransactionType.HardDeleteDialog => "Slette dialog",
            TransactionType.GetDialogServiceOwner => "Hente dialog (tjenesteeier)",
            TransactionType.SearchDialogsServiceOwner => "Tjenesteeiersøk (hente dialoger knyttet til én tjenesteeier)",
            TransactionType.SearchDialogsServiceOwnerWithEndUser => "Tjenesteeiersøk m/sluttbruker-id (hente dialoger knyttet til én tjenesteeier basert på en oppgitt sluttbrukers tilganger)",
            TransactionType.GetDialogEndUser => "Hente dialog (sluttbruker)",
            TransactionType.SetDialogLabel => "Sette label på enkeltdialog (samme for tjenesteeier og sluttbruker)",
            TransactionType.BulkSetLabelsServiceOwnerWithEndUser => "Sluttbruker som setter labels (aka flytter til arkiv/papirkurv) i bulk gjennom tjenesteeier-API m/sluttbruker-id",
            TransactionType.SearchDialogsEndUser => "Sluttbrukersøk",
            TransactionType.BulkSetLabelsEndUser => "Sluttbruker som setter labels (aka flytter til arkiv/papirkurv) i bulk gjennom sluttbruker-API",
            _ => transactionType.ToString()
        };
    }
}
