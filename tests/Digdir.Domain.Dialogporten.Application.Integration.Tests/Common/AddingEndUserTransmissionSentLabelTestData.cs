using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;

public sealed record AddingEndUserTransmissionSentLabelScenario(
    string DisplayName,
    DialogTransmissionType.Values TransmissionType,
    bool ShouldAddSentLabel) : ClassDataBase(DisplayName);

internal sealed class AddingEndUserTransmissionSentLabelTestData : TheoryData<AddingEndUserTransmissionSentLabelScenario>
{
    public AddingEndUserTransmissionSentLabelTestData()
    {
        foreach (var type in Enum.GetValues<DialogTransmissionType.Values>())
        {
            var shouldAddSentLabel = type
                is DialogTransmissionType.Values.Submission
                or DialogTransmissionType.Values.Correction;

            var name = shouldAddSentLabel
                ? $"Should Add Sent label for type {type}"
                : $"Should not add Sent label for type {type}";

            Add(new AddingEndUserTransmissionSentLabelScenario(
                DisplayName: name,
                TransmissionType: type,
                ShouldAddSentLabel: shouldAddSentLabel));
        }
    }
}
