using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Commands.BulkSetSystemLabels;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Commands.SetSystemLabel;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.MutationTypes;

internal static class MutationsMapExtensions
{
    public static SetSystemLabelCommand ToCommand(this SetSystemLabelInput source) => new()
    {
        DialogId = source.DialogId,
        AddLabels = source.AddLabels.Select(x => x.ToDomain()).ToList(),
        RemoveLabels = source.RemoveLabels.Select(x => x.ToDomain()).ToList()
    };

    public static BulkSetSystemLabelCommand ToCommand(this BulkSetSystemLabelInput source) => new()
    {
        Dto = source.ToDto()
    };

    private static BulkSetSystemLabelDto ToDto(this BulkSetSystemLabelInput source) => new()
    {
        Dialogs = source.Dialogs.Select(ToDialogRevisionDto).ToList(),
        AddLabels = source.AddLabels.Select(x => x.ToDomain()).ToList(),
        RemoveLabels = source.RemoveLabels.Select(x => x.ToDomain()).ToList()
    };

    private static DialogRevisionDto ToDialogRevisionDto(DialogRevisionInput source) => new()
    {
        DialogId = source.DialogId,
        EndUserContextRevision = source.EnduserContextRevision
    };
}
