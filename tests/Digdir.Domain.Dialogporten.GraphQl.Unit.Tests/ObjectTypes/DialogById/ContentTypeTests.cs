using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Contents;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById;

namespace Digdir.Domain.Dialogporten.GraphQl.Unit.Tests.ObjectTypes.DialogById;

public class ContentTypeTests
{
    [Fact]
    public void DialogContentType_Names_Should_Match_Props_On_Content()
    {
        // Arrange
        var dialogContentTypeNames = DialogContentType.GetValues()
            .Where(x => x.Id is not DialogContentType.Values.NonSensitiveSummary
                and not DialogContentType.Values.NonSensitiveTitle)
            .Select(x => x.Name)
            .ToList();

        var dtoPropertyNames = typeof(Content)
            .GetProperties()
            .Select(p => p.Name)
            .ToList();

        Assert.Equal(dialogContentTypeNames.Count, dtoPropertyNames.Count);
        foreach (var contentTypeName in dialogContentTypeNames)
        {
            Assert.Contains(contentTypeName, dtoPropertyNames, StringComparer.OrdinalIgnoreCase);
        }
    }
}
