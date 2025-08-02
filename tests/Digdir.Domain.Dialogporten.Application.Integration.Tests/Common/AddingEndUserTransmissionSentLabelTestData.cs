using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;

internal sealed class AddingEndUserTransmissionSentLabelTestData : TheoryData<DialogTransmissionType.Values, bool>
{
    public AddingEndUserTransmissionSentLabelTestData()
    {
        foreach (var type in Enum.GetValues<DialogTransmissionType.Values>())
        {
            // (transmissionType, shouldAddSentLabel)
            Add(type, type
                is DialogTransmissionType.Values.Submission
                or DialogTransmissionType.Values.Correction);
        }
    }
}
