using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Parties;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.SystemLabelAdder;

internal static class SystemLabelAdder
{
    public static void AddSystemLabel(
        IUser user,
        IDomainContext domainContext,
        DialogEntity dialog,
        SystemLabel.Values labelToAdd
    )
    {
        if (!user.GetPrincipal().TryGetConsumerOrgNumber(out var organizationNumber))
        {
            domainContext.AddError(
                new DomainFailure(nameof(organizationNumber), "Cannot find organization number for current user.")
            );
            return;
        }

        var performedBy = LabelAssignmentLogActorFactory.Create(
            ActorType.Values.ServiceOwner,
            actorId: $"{NorwegianOrganizationIdentifier.PrefixWithSeparator}{organizationNumber}",
            actorName: null);

        dialog.EndUserContext.UpdateSystemLabels(
            addLabels: [labelToAdd],
            removeLabels: [],
            performedBy);
    }
}
