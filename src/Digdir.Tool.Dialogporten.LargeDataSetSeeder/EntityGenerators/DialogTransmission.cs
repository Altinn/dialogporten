using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Tool.Dialogporten.LargeDataSetSeeder.Common;

namespace Digdir.Tool.Dialogporten.LargeDataSetSeeder.EntityGenerators;

public sealed record DialogTransmission(
    Guid Id,
    DateTime CreatedAt,
    string? AuthorizationAttribute,
    string? ExtendedType,
    string? ExternalReference,
    DialogTransmissionType.Values TypeId,
    Guid DialogId,
    Guid? RelatedTransmissionId
) : IEntityGenerator<DialogTransmission>
{
    public static IEnumerable<DialogTransmission> GenerateEntities(IEnumerable<DialogTimestamp> timestamps)
    {
        foreach (var timestamp in timestamps)
        {
            var numTransmissions = timestamp.GetRng().Next(0, TransmissionTypes.Count + 1);

            if (numTransmissions == 0)
            {
                continue;
            }

            for (var i = 0; i < numTransmissions; i++)
            {
                var transmissionId = timestamp.ToUuidV7<DialogTransmission>(timestamp.DialogId, i);
                var typeId = TransmissionTypes[i % TransmissionTypes.Count];
                yield return new(
                    Id: transmissionId,
                    CreatedAt: timestamp.Timestamp.UtcDateTime,
                    AuthorizationAttribute: null,
                    ExtendedType: null,
                    ExternalReference: null,
                    TypeId: typeId,
                    DialogId: timestamp.DialogId,
                    RelatedTransmissionId: i == 0 ? null : timestamp.ToUuidV7<DialogTransmission>(timestamp.DialogId, i - 1)
                );
            }
        }
    }

    private static readonly List<DialogTransmissionType.Values> TransmissionTypes =
    [
        DialogTransmissionType.Values.Information,
        DialogTransmissionType.Values.Submission,
        DialogTransmissionType.Values.Rejection,
        DialogTransmissionType.Values.Correction,
        DialogTransmissionType.Values.Acceptance
    ];
}
