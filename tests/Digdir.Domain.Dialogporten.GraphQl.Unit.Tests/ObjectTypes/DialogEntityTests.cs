using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById;
using DialogStatus = Digdir.Domain.Dialogporten.GraphQL.EndUser.Common.DialogStatus;
using DialogStatusValues = Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.DialogStatus.Values;

namespace Digdir.Domain.Dialogporten.GraphQl.Unit.Tests.ObjectTypes;

public class DialogEntityTests
{
    [Fact]
    public void Dialog_Object_Type_Should_Match_Property_Names_On_DialogEntity()
    {
        // Arrange
        var ignoreList = new List<string>
        {
            nameof(DialogEntity.Deleted),
            nameof(DialogEntity.DeletedAt),
            nameof(DialogEntity.StatusId),
            nameof(DialogEntity.SearchTags),
            nameof(DialogEntity.SeenLog),
            nameof(DialogEntity.DialogEndUserContext),
            nameof(DialogEntity.IdempotentKey),
            nameof(DialogEntity.ServiceOwnerContext)
        };

        var dialogProperties = typeof(DialogEntity)
            .GetProperties()
            .Select(p => p.Name)
            .Where(name => !ignoreList.Contains(name))
            .ToList();

        var domainDialogProperties = typeof(Dialog)
            .GetProperties()
            .Select(p => p.Name)
            .ToList();

        var missingProperties = dialogProperties.Except(domainDialogProperties, StringComparer.OrdinalIgnoreCase).ToList();

        // Assert
        Assert.True(missingProperties.Count == 0, $"Properties missing in graphql dialog: {string.Join(", ", missingProperties)}");
    }

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
