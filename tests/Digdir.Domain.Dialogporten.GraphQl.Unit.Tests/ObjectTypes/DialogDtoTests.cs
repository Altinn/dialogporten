using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Digdir.Domain.Dialogporten.GraphQl.Unit.Tests.ObjectTypes;

public class DialogDtoTests
{
    [Fact]
    public void Dialog_Object_Type_Should_Match_Property_Names_On_DialogDto()
    {
        // Arrange
        var dialogProperties = typeof(DialogDto)
            .GetProperties()
            .Select(p => p.Name)
            // Delete when dialogDto.SystemLabel is removed
            .Where(name => !name.Contains(nameof(DialogDto.SystemLabel)))
            .ToList();

        var domainDialogProperties = typeof(Dialog)
            .GetProperties()
            .Select(p => p.Name)
            .ToList();

        // Act
        var missingProperties = dialogProperties.Except(domainDialogProperties, StringComparer.OrdinalIgnoreCase).ToList();

        // Assert
        Assert.True(missingProperties.Count == 0, $"Properties missing in graphql dialog: {string.Join(", ", missingProperties)}");
    }
}
