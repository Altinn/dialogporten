using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Queries.Search;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class ServiceOwnerContextRevisionTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task Search_Should_Populate_ServiceOwnerContextRevision()
    {
        var createCmd = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var createRes = await Application.Send(createCmd);

        var response = await Application.Send(new SearchDialogQuery
        {
            ServiceResource = [createCmd.Dto.ServiceResource]
        });

        response.TryPickT0(out var result, out _).Should().BeTrue();
        var dialog = result.Items.Single(x => x.Id == createRes.AsT0.DialogId);
        dialog.ServiceOwnerContextRevision.Should().NotBeEmpty();
    }
}
