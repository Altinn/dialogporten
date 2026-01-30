using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateTransmission;

namespace Digdir.Domain.Dialogporten.WebApi.Unit.Tests.Features.V1.ServiceOwner.Dialogs;

public class TransmissionDtoPropertyEquivalenceTests
{
    [Fact]
    public void Create_Transmission_DTOs_Should_Have_Identical_Property_Names()
    {
        var createDialogTransmissionProperties = GetNames(typeof(TransmissionDto)).ToList();
        var createTransmissionProperties = GetNames(typeof(CreateTransmissionDto)).ToList();

        var missingFromCreateDialogTransmission = createTransmissionProperties
            .Except(createDialogTransmissionProperties, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var missingFromCreateTransmission = createDialogTransmissionProperties
            .Except(createTransmissionProperties, StringComparer.OrdinalIgnoreCase)
            .ToList();

        missingFromCreateDialogTransmission.Should().BeEmpty(
            $"Properties missing in {nameof(TransmissionDto)}: " +
            $"{string.Join(", ", missingFromCreateDialogTransmission)}");

        missingFromCreateTransmission.Should().BeEmpty(
            $"Properties missing in {nameof(CreateTransmissionDto)}: " +
            $"{string.Join(", ", missingFromCreateTransmission)}");
    }

    private static IEnumerable<string> GetNames(Type type) =>
        type.GetProperties().Select(p => p.Name);
}
