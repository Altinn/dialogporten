using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById;

namespace Digdir.Domain.Dialogporten.GraphQl.Unit.Tests.ObjectTypes;

public class DialogTransmissionTests
{
    [Fact]
    public void Transmission_Object_Type_Should_Match_Property_Names_On_DialogTransmission()
    {
        // Arrange
        var ignoreList = new List<string>
        {
            nameof(DialogTransmission.RelatedTransmission),
            nameof(DialogTransmission.RelatedTransmissions),
            nameof(DialogTransmission.Activities),
            nameof(DialogTransmission.Dialog),
            nameof(DialogTransmission.DialogId),
            nameof(DialogTransmission.TypeId),
            nameof(DialogTransmission.ExternalReference)
        };

        var domainTransmissionProperties = typeof(DialogTransmission)
            .GetProperties()
            .Select(p => p.Name)
            .Where(name => !ignoreList.Contains(name))
            .ToList();

        var transmissionProperties = typeof(Transmission)
            .GetProperties()
            .Select(p => p.Name)
            .ToList();

        var missingProperties = domainTransmissionProperties.Except(transmissionProperties, StringComparer.OrdinalIgnoreCase).ToList();

        // Assert
        Assert.True(missingProperties.Count == 0, $"Properties missing in graphql transmission: {string.Join(", ", missingProperties)}");
    }

    [Fact]
    public void TransmissionType_Object_Type_Should_Match_Property_Names_On_DialogTransmissionTypeValues()
    {
        // Arrange
        var transmissionTypes = typeof(TransmissionType)
            .GetFields()
            .Select(f => f.Name)
            .ToList();

        var domainTransmissionTypes = typeof(DialogTransmissionType.Values)
            .GetFields()
            .Select(f => f.Name)
            .ToList();

        var missingProperties = domainTransmissionTypes.Except(transmissionTypes, StringComparer.OrdinalIgnoreCase).ToList();

        // Assert
        Assert.True(missingProperties.Count == 0, $"Properties missing in graphql TransmissionType: {string.Join(", ", missingProperties)}");
    }
}
