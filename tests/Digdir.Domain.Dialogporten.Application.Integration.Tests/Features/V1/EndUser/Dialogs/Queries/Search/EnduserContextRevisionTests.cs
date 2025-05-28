using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Dialogs.Queries.Search;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class EnduserContextRevisionTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task Search_Should_Populate_EnduserContextRevision()
    {
        var createCmd = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var createRes = await Application.Send(createCmd);

        var response = await Application.Send(new SearchDialogQuery
        {
            Party = [createCmd.Dto.Party]
        });

        response.TryPickT0(out var result, out _).Should().BeTrue();
        var dialog = result.Items.Single(x => x.Id == createRes.AsT0.DialogId);
        dialog.EnduserContextRevision.Should().NotBeEmpty();
    }
}
