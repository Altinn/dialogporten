using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.SearchDialogs;

namespace Digdir.Domain.Dialogporten.GraphQl.Unit.Tests.ObjectTypes.SearchDialogs;

public class ContentTypeTests
{
    [Fact]
    public void OutPutInList_DialogContentType_Names_Should_Match_Props_On_SearchContent()
    {
        // Arrange
        var dialogContentTypeNames = DialogContentType.GetValues()
            .Where(x => x.Id is not DialogContentType.Values.NonSensitiveSummary
                and not DialogContentType.Values.NonSensitiveTitle)
            .Where(x => x.OutputInList)
            .Select(x => x.Name)
            .ToList();

        var dtoPropertyNames = typeof(SearchContent)
            .GetProperties()
            .Select(p => p.Name)
            .ToList();

        // Assert
        Assert.Equal(dialogContentTypeNames.Count, dtoPropertyNames.Count);
        foreach (var contentTypeName in dialogContentTypeNames)
        {
            Assert.Contains(contentTypeName, dtoPropertyNames, StringComparer.OrdinalIgnoreCase);
        }
    }
}
