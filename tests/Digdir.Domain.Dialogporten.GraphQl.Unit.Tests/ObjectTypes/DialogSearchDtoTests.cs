using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.SearchDialogs;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Digdir.Domain.Dialogporten.GraphQl.Unit.Tests.ObjectTypes;

public class DialogSearchDtoTests
{
    [Fact]
    public void SearchDialog_Object_Type_Should_Match_Property_Names_On_Search_DialogDto()
    {
        // Arrange
        var dialogProperties = typeof(DialogDto)
            .GetProperties()
            .Select(p => p.Name)
            // Delete when dialogDto.SystemLabel is removed
            .Where(name => !name.Contains(nameof(DialogDto.SystemLabel)))
            .ToList();

        var domainDialogProperties = typeof(SearchDialog)
            .GetProperties()
            .Select(p => p.Name)
            .ToList();

        // Act
        var missingProperties = dialogProperties.Except(domainDialogProperties, StringComparer.OrdinalIgnoreCase).ToList();

        // Assert
        Assert.True(missingProperties.Count == 0, $"Properties missing in graphql dialog: {string.Join(", ", missingProperties)}");
    }
}
