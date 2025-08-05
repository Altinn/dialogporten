using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Extensions;

internal static class TransmissionEntityExtensions
{
    internal static bool ContainsTransmissionByEndUser(this List<DialogTransmission> transmissions) =>
        transmissions.Any(x => x.TypeId
            is DialogTransmissionType.Values.Submission
            or DialogTransmissionType.Values.Correction);
}
