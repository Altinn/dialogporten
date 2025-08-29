using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder.Common;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record Dialog(
    Guid Id,
    Guid Revision,
    string? IdempotentKey,
    DateTimeOffset CreatedAt,
    DateTimeOffset UpdatedAt,
    bool Deleted,
    DateTimeOffset? DeletedAt,
    string Org,
    string ServiceResource,
    string ServiceResourceType,
    string Party,
    int Progress,
    string? ExtendedStatus,
    string? ExternalReference,
    DateTimeOffset? VisibleFrom,
    DateTimeOffset? DueAt,
    DateTimeOffset? ExpiresAt,
    DialogStatus.Values StatusId,
    string? Process,
    string? PrecedingProcess,
    bool IsApiOnly,
    short FromServiceOwnerTransmissionsCount,
    short FromPartyTransmissionsCount,
    bool HasUnopenedContent,
    DateTimeOffset ContentUpdatedAt
) : IEntityGenerator<Dialog>
{
    public static IEnumerable<Dialog> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        foreach (var timestamp in timestamps)
        {
            var rng = timestamp.GetRng();
            var party = rng.GetParty();

            var transmissions = DialogTransmission.GenerateEntities([timestamp]).ToList();

            yield return new Dialog(
                Id: timestamp.DialogId,
                Revision: Guid.NewGuid(),
                IdempotentKey: null,
                CreatedAt: timestamp.Timestamp,
                UpdatedAt: timestamp.Timestamp,
                Deleted: false,
                DeletedAt: null,
                Org: "ttd", // TODO: fancy pick from Dagfinns list?
                ServiceResource: "service/resource", // TODO: fancy pick from service_resources file?
                ServiceResourceType: "GenericAccessResource", // based on picked service resource?
                Party: party,
                Progress: rng.Next(0, 101),
                ExtendedStatus: null,
                ExternalReference: null,
                VisibleFrom: null,
                DueAt: null,
                ExpiresAt: null,
                StatusId: DialogStatus.Values.NotApplicable,
                Process: null,
                PrecedingProcess: null,
                IsApiOnly: false,
                FromServiceOwnerTransmissionsCount: (short)transmissions.Count(x => x.TypeId
                    is not DialogTransmissionType.Values.Submission
                    and not DialogTransmissionType.Values.Correction),
                FromPartyTransmissionsCount: (short)transmissions.Count(x => x.TypeId
                    is DialogTransmissionType.Values.Submission
                    or DialogTransmissionType.Values.Correction),
                HasUnopenedContent: false, // TODO: ask Magnus about this
                ContentUpdatedAt: timestamp.Timestamp
            );
        }
    }
}
