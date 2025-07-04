using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Dialogs.Queries.Search;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SearchDialogTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task Search_Should_Populate_EnduserContextRevision()
    {
        string? party = null;
        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x => party = x.Dto.Party)
            .SearchEndUserDialogs(x => x.Party = [party!])
            .ExecuteAndAssert<PaginatedList<DialogDto>>(x =>
                x.Items.Should().ContainSingle(x =>
                    x.EndUserContext.Revision != Guid.Empty));
    }

    [Fact]
    [Obsolete("Testing obsolete SystemLabel, will be removed in future versions.")]
    public async Task Search_Should_Populate_Obsolete_SystemLabel()
    {
        string? party = null;
        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x => party = x.Dto.Party)
            .SearchEndUserDialogs(x => x.Party = [party!])
            .ExecuteAndAssert<PaginatedList<DialogDto>>(x =>
                x.Items.Should().ContainSingle(x =>
                    x.SystemLabel == SystemLabel.Values.Default));
    }

    [Fact]
    public Task Search_Should_Return_HasUnopenedContent_False_For_New_Simple_Dialogs() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SearchEndUserDialogs((x, ctx) =>
                x.Party = [ctx.GetParty()])
            .ExecuteAndAssert<PaginatedList<DialogDto>>(x =>
                x.Items.Single().HasUnopenedContent.Should().BeFalse());

    [Fact]
    public Task Search_Should_Return_HasUnopenedContent_True_For_Dialogs_With_Unopened_Transmission() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
                // Unopened content
                x.AddTransmission(x =>
                    x.Type = DialogTransmissionType.Values.Information))
            .SearchEndUserDialogs((x, ctx) =>
                x.Party = [ctx.GetParty()])
            .ExecuteAndAssert<PaginatedList<DialogDto>>(x =>
                x.Items.Single().HasUnopenedContent.Should().BeTrue());
}
