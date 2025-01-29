using Digdir.Library.Dialogporten.WebApiClient.Features.V1;
using Refit;

namespace Digdir.Library.Dialogporten.WebApiClient.Sample;

public sealed class Dialogs
{
    public static void PrintGetDialog(V1ServiceOwnerDialogsQueriesGet_Dialog dialog)
    {
        Console.WriteLine($"System Label: {dialog.SystemLabel}");
        Console.WriteLine($"Dialog Status: {dialog.Status}");
        Console.WriteLine($"Dialog Org: {dialog.Org}");
        Console.WriteLine($"Dialog Progress: {dialog.Progress}");
        Console.WriteLine($"Deleted at: {dialog.DeletedAt}");
    }
}
