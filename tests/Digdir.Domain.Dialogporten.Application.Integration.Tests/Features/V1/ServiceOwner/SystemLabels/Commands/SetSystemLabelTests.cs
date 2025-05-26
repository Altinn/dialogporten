using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.DialogSystemLabels.Commands.Set;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.SystemLabels.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SetSystemLabelTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task Set_Updates_System_Label()
    {
        var createCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var create = await Application.Send(createCommand);

        var command = new SetSystemLabelCommand
        {
            DialogId = create.AsT0.DialogId,
            EnduserId = createCommand.Dto.Party,
            SystemLabels = new[] { SystemLabel.Values.Bin }
        };

        var result = await Application.Send(command);
        result.TryPickT0(out _, out _).Should().BeTrue();

        var get = await Application.Send(new GetDialogQuery { DialogId = create.AsT0.DialogId });
        get.AsT0.SystemLabel.Should().Be(SystemLabel.Values.Bin);
    }

    [Fact]
    public async Task Set_Returns_ConcurrencyError_On_Revision_Mismatch()
    {
        var createCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var create = await Application.Send(createCommand);

        var command = new SetSystemLabelCommand
        {
            DialogId = create.AsT0.DialogId,
            EnduserId = createCommand.Dto.Party,
            IfMatchEnduserContextRevision = Guid.NewGuid(),
            SystemLabels = new[] { SystemLabel.Values.Bin }
        };

        var result = await Application.Send(command);
        result.TryPickT5(out _, out _).Should().BeTrue();
    }

    [Fact]
    public async Task Set_Succeeds_On_Revision_Match()
    {
        var createCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var create = await Application.Send(createCommand);

        var search = await Application.Send(new SearchDialogQuery
        {
            Party = [createCommand.Dto.Party]
        });
        search.TryPickT0(out var result, out _).Should().BeTrue();
        var revision = result.Items.Single(x => x.Id == create.AsT0.DialogId).EnduserContextRevision;

        var command = new SetSystemLabelCommand
        {
            DialogId = create.AsT0.DialogId,
            EnduserId = createCommand.Dto.Party,
            IfMatchEnduserContextRevision = revision,
            SystemLabels = new[] { SystemLabel.Values.Bin }
        };

        var set = await Application.Send(command);
        set.TryPickT0(out _, out _).Should().BeTrue();

        var get = await Application.Send(new GetDialogQuery { DialogId = create.AsT0.DialogId });
        get.AsT0.SystemLabel.Should().Be(SystemLabel.Values.Bin);
    }
}
