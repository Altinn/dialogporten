using Digdir.Domain.Dialogporten.Application.Features.V1.Compliance.Queries.GetPartyGdprExtract.Activities;
using Digdir.Domain.Dialogporten.Application.Features.V1.Compliance.Queries.GetPartyGdprExtract.LabelAssignmentLogs;
using Digdir.Domain.Dialogporten.Application.Features.V1.Compliance.Queries.GetPartyGdprExtract.SeenLogs;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Compliance.Queries.GetPartyGdprExtract;

public sealed record PartyGdprExtractDto(
    List<DialogDto> Dialogs,
    List<SeenLogGdprExtractDto> SeenLogs,
    List<ActivityGdprExtractDto> Activities,
    List<LabelAssignmentGdprExtractDto> LabelAssignments
);
