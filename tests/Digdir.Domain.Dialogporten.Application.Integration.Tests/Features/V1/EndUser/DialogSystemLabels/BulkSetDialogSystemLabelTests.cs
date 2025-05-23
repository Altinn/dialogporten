using Digdir.Domain.Dialogporten.Application.Features.V1.Common.SystemLabels;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.DialogSystemLabels.Commands.BulkSet;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.DialogSystemLabels;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class BulkSetDialogSystemLabelTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task BulkSet_Updates_System_Labels()
    {
        var cmd1 = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var cmd2 = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var res1 = await Application.Send(cmd1);
        var res2 = await Application.Send(cmd2);

        var command = new BulkSetSystemLabelCommand
        {
            Dto = new BulkSetSystemLabelDto
            {
                Dialogs = new[]
                {
                    new DialogRevisionDto { DialogId = res1.AsT0.DialogId },
                    new DialogRevisionDto { DialogId = res2.AsT0.DialogId }
                },
                SystemLabels = new[] { SystemLabel.Values.Bin }
            }
        };

        var result = await Application.Send(command);
        result.TryPickT0(out _, out _).Should().BeTrue();

        var get1 = await Application.Send(new GetDialogQuery { DialogId = res1.AsT0.DialogId });
        get1.AsT0.SystemLabel.Should().Be(SystemLabel.Values.Bin);
        var get2 = await Application.Send(new GetDialogQuery { DialogId = res2.AsT0.DialogId });
        get2.AsT0.SystemLabel.Should().Be(SystemLabel.Values.Bin);
    }

    [Fact]
    public async Task BulkSet_Updates_System_Labels_With_Revisions()
    {
        var cmd1 = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var cmd2 = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var res1 = await Application.Send(cmd1);
        var res2 = await Application.Send(cmd2);

        var contexts = await Application.GetDbEntities<DialogEndUserContext>();
        var ctx1 = contexts.Single(x => x.DialogId == res1.AsT0.DialogId);
        var ctx2 = contexts.Single(x => x.DialogId == res2.AsT0.DialogId);

        var command = new BulkSetSystemLabelCommand
        {
            Dto = new BulkSetSystemLabelDto
            {
                Dialogs = new[]
                {
                    new DialogRevisionDto { DialogId = res1.AsT0.DialogId, EnduserContextRevision = ctx1.Revision },
                    new DialogRevisionDto { DialogId = res2.AsT0.DialogId, EnduserContextRevision = ctx2.Revision }
                },
                SystemLabels = new[] { SystemLabel.Values.Bin }
            }
        };

        var result = await Application.Send(command);
        result.TryPickT0(out _, out _).Should().BeTrue();

        var get1 = await Application.Send(new GetDialogQuery { DialogId = res1.AsT0.DialogId });
        get1.AsT0.SystemLabel.Should().Be(SystemLabel.Values.Bin);
        var get2 = await Application.Send(new GetDialogQuery { DialogId = res2.AsT0.DialogId });
        get2.AsT0.SystemLabel.Should().Be(SystemLabel.Values.Bin);
    }

    [Fact]
    public async Task BulkSet_Returns_Forbidden_For_Invalid_Id()
    {
        var cmd = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var res = await Application.Send(cmd);

        var command = new BulkSetSystemLabelCommand
        {
            Dto = new BulkSetSystemLabelDto
            {
                Dialogs = new[]
                {
                    new DialogRevisionDto { DialogId = res.AsT0.DialogId },
                    new DialogRevisionDto { DialogId = Guid.NewGuid() }
                },
                SystemLabels = new[] { SystemLabel.Values.Bin }
            }
        };

        var result = await Application.Send(command);
        result.TryPickT1(out var forbidden, out _).Should().BeTrue();
        forbidden.Reasons.Should().NotBeEmpty();
    }

    [Fact]
    public async Task BulkSet_Returns_ConcurrencyError_On_Revision_Mismatch()
    {
        var cmd = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var res = await Application.Send(cmd);

        var command = new BulkSetSystemLabelCommand
        {
            Dto = new BulkSetSystemLabelDto
            {
                Dialogs = new[]
                {
                    new DialogRevisionDto { DialogId = res.AsT0.DialogId, EnduserContextRevision = Guid.NewGuid() }
                },
                SystemLabels = new[] { SystemLabel.Values.Bin }
            }
        };

        var result = await Application.Send(command);
        result.TryPickT4(out _, out _).Should().BeTrue();
    }
}
