using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Queries.SearchLabelAssignmentLog;

internal static class LabelAssignmentLogMapExtensions
{
    extension(LabelAssignmentLog source)
    {
        internal LabelAssignmentLogDto ToDto() => new()
        {
            CreatedAt = source.CreatedAt,
            Name = source.Name,
            Action = source.Action,
            /*
             * PerformedBy should not be null, but for some cases the database do not have this data.
             * See issue: https://github.com/Altinn/dialogporten/issues/4340
             * Code should be removed if we add missing data to the database for all environments
             */
            PerformedBy = source.PerformedBy?.ToDto() ?? new ActorDto
            {
                ActorType = ActorType.Values.PartyRepresentative,
                ActorId = "",
                ActorName = ""
            }
        };
    }
}
