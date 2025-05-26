using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.DialogSystemLabels.Commands.Set;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.SystemLabels.Commands;

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

        var contexts = await Application.GetDbEntities<DialogEndUserContext>();
        var ctx = contexts.Single(x => x.DialogId == create.AsT0.DialogId);
        var oldRevision = ctx.Revision;

        var command = new SetSystemLabelCommand
        {
            DialogId = create.AsT0.DialogId,
            IfMatchEnduserContextRevision = oldRevision,
            SystemLabels = new[] { SystemLabel.Values.Bin }
        };

        var result = await Application.Send(command);
        result.TryPickT0(out var success, out _).Should().BeTrue();
        success.Revision.Should().NotBeEmpty();
        success.Revision.Should().NotBe(oldRevision);

        var get = await Application.Send(new GetDialogQuery { DialogId = create.AsT0.DialogId });
        get.AsT0.SystemLabel.Should().Be(SystemLabel.Values.Bin);
    }
}
