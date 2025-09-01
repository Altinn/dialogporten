using Bogus;
using Bogus.Extensions.Norway;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Parties;
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
        using var parties = Parties().GetEnumerator();
        foreach (var timestamp in timestamps)
        {
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
                Party: parties.GetNext(),
                Progress: Random.Shared.Next(0, 101),
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

    private static IEnumerable<string> Parties(Random? rng = null)
    {
        rng ??= Random.Shared;
        var expDistrDialogAmount = Settings.DialogAmount_S!.Value / 2;
        var minBase = Math.Min(150, expDistrDialogAmount / KnownParties.Values.Length);
        using var expDispEnumerator = ExponentialDistribution
            .NextExponentialIndicesWithBase(expDistrDialogAmount, minBase, KnownParties.Values.Length, 0.03, rng)
            .GetEnumerator();
        var count = 0;
        while (true)
        {
            if (count++ % 2 == 0 && expDispEnumerator.MoveNext())
            {
                yield return KnownParties.Values[expDispEnumerator.Current].GetRandomPartyUrn(rng);
                continue;
            }

            yield return $"{NorwegianPersonIdentifier.PrefixWithSeparator}{new Person("nb_NO", rng.Next()).Fodselsnummer()}";
        }
        // ReSharper disable once IteratorNeverReturns
    }
}
