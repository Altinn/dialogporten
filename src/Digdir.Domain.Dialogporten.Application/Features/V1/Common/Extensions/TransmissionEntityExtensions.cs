using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Extensions;

internal static class TransmissionEntityExtensions
{
    internal static bool ContainsTransmissionByEndUser(this IEnumerable<DialogTransmission> transmissions) =>
        transmissions.Any(x => IsEndUserTransmissionType(x.TypeId));

    internal static (int FromParty, int FromServiceOwner) GetTransmissionCounts(this IEnumerable<DialogTransmission> transmissions)
    {
        var fromParty = 0;
        var fromServiceOwner = 0;

        foreach (var transmission in transmissions)
        {
            if (IsEndUserTransmissionType(transmission.TypeId))
            {
                fromParty++;
            }
            else
            {
                fromServiceOwner++;
            }
        }

        return (fromParty, fromServiceOwner);
    }

    private static bool IsEndUserTransmissionType(DialogTransmissionType.Values typeId)
        => typeId is DialogTransmissionType.Values.Submission or DialogTransmissionType.Values.Correction;
}
