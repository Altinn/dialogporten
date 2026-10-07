using AwesomeAssertions;
using Digdir.Domain.Dialogporten.WebApi.Endpoints.V1.Common.Problem.Mappers;
using NSwag;

namespace Digdir.Domain.Dialogporten.WebApi.Unit.Tests.Features.V1;

public class OpenApiSpecConflictTests
{
    [Fact]
    public void ShouldDocumentAllConflicts()
    {
        var openApiDocument = new OpenApiDocument();
        openApiDocument.AddConflictsSection();

        var actual = ProblemDetailsConflictCode.AllCodes
            .Where(x => openApiDocument.Info.Description.Contains(x.ToString()))
            .Select(x => x.ToString())
            .ToList();

        var expected = ProblemDetailsConflictCode.AllCodes.Select(x => x.ToString());

        actual.Should().BeEquivalentTo(expected);
    }
}
