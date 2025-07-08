using DialogStatus = Digdir.Domain.Dialogporten.GraphQL.EndUser.Common.DialogStatus;
using DialogStatusValues = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.DialogStatus.Values;
#pragma warning disable CS0618 // Type or member is obsolete

namespace Digdir.Domain.Dialogporten.GraphQl.Unit.Tests.ObjectTypes;

public class DialogStatusTests
{
    [Fact]
    public void DialogStatus_Object_Type_Should_Match_Property_Names_On_DialogStatusValues()
    {
        // Arrange
        var dialogStatusValues = Enum.GetNames(typeof(DialogStatus)).ToList();
        var domainDialogStatusValues = Enum.GetNames(typeof(DialogStatusValues)).ToList();

        var missingProperties = domainDialogStatusValues.Except(dialogStatusValues, StringComparer.OrdinalIgnoreCase).ToList();

        // Assert
        Assert.True(missingProperties.Count == 0, $"Properties missing in graphql dialog status: {string.Join(", ", missingProperties)}");
    }
}
