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
            nameof(DialogEntity.DialogEndUserContext)
        };

        var dialogEntityPropertyNames = typeof(DialogEntity)
            .GetProperties()
            .Select(p => p.Name)
            .Where(name => !ignoreList.Contains(name))
            .ToList();

        var dialogPropertyNames = typeof(Dialog)
            .GetProperties()
            .Select(p => p.Name)
            .ToList();

        var missingProperties = dialogEntityPropertyNames.Except(dialogPropertyNames, StringComparer.OrdinalIgnoreCase).ToList();

        // Assert
        Assert.True(missingProperties.Count == 0, $"Properties missing in graphql dialog: {string.Join(", ", missingProperties)}");
    }

    [Fact]
    public void DialogStatus_Object_Type_Should_Match_Property_Names_On_DialogStatusValues()
    {
        // Arrange
        var dialogStatusValues = typeof(DialogStatus)
            .GetProperties()
            .Select(p => p.Name)
            .ToList();

        var domainDialogStatusValues = typeof(DialogStatusValues)
            .GetProperties()
            .Select(p => p.Name)
            .ToList();

        var missingProperties = domainDialogStatusValues.Except(dialogStatusValues, StringComparer.OrdinalIgnoreCase).ToList();

        // Assert
        Assert.True(missingProperties.Count == 0, $"Properties missing in graphql dialog status: {string.Join(", ", missingProperties)}");
    }
}
