using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Dialogs.Queries.Get;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class EnduserContextRevisionTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task Get_Should_Populate_EnduserContextRevision()
    {
        var createCmd = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var createRes = await Application.Send(createCmd);

        var response = await Application.Send(new GetDialogQuery { DialogId = createRes.AsT0.DialogId });

        response.TryPickT0(out var result, out _).Should().BeTrue();
        result.EnduserContextRevision.Should().NotBeEmpty();
    }
}
