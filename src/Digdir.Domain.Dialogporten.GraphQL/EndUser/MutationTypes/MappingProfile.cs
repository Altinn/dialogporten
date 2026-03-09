using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Commands.BulkSetSystemLabels;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Commands.SetSystemLabel;
using ApplicationSystemLabel = Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities.SystemLabel;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.MutationTypes;

internal static class GraphQlMapper
{
    extension(SetSystemLabelInput source)
    {
        public SetSystemLabelCommand ToCommand() => new()
        {
            DialogId = source.DialogId,
            AddLabels = source.AddLabels.Select(label => (ApplicationSystemLabel.Values)label).ToList(),
            RemoveLabels = source.RemoveLabels.Select(label => (ApplicationSystemLabel.Values)label).ToList()
        };
    }

    extension(DialogRevisionInput source)
    {
        public DialogRevisionDto ToDto() => new()
        {
            DialogId = source.DialogId,
            EndUserContextRevision = source.EnduserContextRevision
        };
    }

    extension(BulkSetSystemLabelInput source)
    {
        public BulkSetSystemLabelDto ToDto() => new()
        {
            Dialogs = source.Dialogs.Select(dialog => dialog.ToDto()).ToList(),
            AddLabels = source.AddLabels.Select(label => (ApplicationSystemLabel.Values)label).ToList(),
            RemoveLabels = source.RemoveLabels.Select(label => (ApplicationSystemLabel.Values)label).ToList()
        };

        public BulkSetSystemLabelCommand ToCommand() => new()
        {
            Dto = source.ToDto()
        };
    }
}
