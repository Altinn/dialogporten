using Digdir.Domain.Dialogporten.Application.Features.V1.Common.SystemLabels;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.DialogSystemLabels.Commands.BulkSet;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.SystemLabels.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class BulkSetSystemLabelTests(DialogApplication application) : ApplicationCollectionFixture(application)
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
            EnduserId = cmd2.Dto.Party,
            Dto = new BulkSetSystemLabelDto
            {
                Dialogs =
                [
                    new DialogRevisionDto { DialogId = res1.AsT0.DialogId },
                    new DialogRevisionDto { DialogId = res2.AsT0.DialogId }
                ],
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
            EnduserId = cmd.Dto.Party,
            Dto = new BulkSetSystemLabelDto
            {
                Dialogs =
                [
                    new DialogRevisionDto { DialogId = res.AsT0.DialogId },
                    new DialogRevisionDto { DialogId = Guid.NewGuid() }
                ],
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
            EnduserId = cmd.Dto.Party,
            Dto = new BulkSetSystemLabelDto
            {
                Dialogs =
                [
                    new DialogRevisionDto { DialogId = res.AsT0.DialogId, EnduserContextRevision = Guid.NewGuid() }
                ],
                SystemLabels = [SystemLabel.Values.Bin]
            }
        };

        var result = await Application.Send(command);
        result.TryPickT4(out _, out _).Should().BeTrue();
    }

    [Fact]
    public async Task BulkSet_Updates_System_Labels_With_Revisions()
    {
        var cmd1 = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var cmd2 = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var res1 = await Application.Send(cmd1);
        var res2 = await Application.Send(cmd2);

        var dialog1 = await Application.Send(new SearchDialogQuery
        {
            Party = [cmd1.Dto.Party]
        });
        dialog1.TryPickT0(out var result1, out _).Should().BeTrue();
        var rev1 = result1.Items.Single(x => x.Id == res1.AsT0.DialogId).EnduserContextRevision;

        var dialog2 = await Application.Send(new SearchDialogQuery
        {
            Party = [cmd2.Dto.Party]
        });
        dialog2.TryPickT0(out var result2, out _).Should().BeTrue();
        var rev2 = result2.Items.Single(x => x.Id == res2.AsT0.DialogId).EnduserContextRevision;

        var command = new BulkSetSystemLabelCommand
        {
            EnduserId = cmd1.Dto.Party,
            Dto = new BulkSetSystemLabelDto
            {
                Dialogs =
                [
                    new DialogRevisionDto { DialogId = res1.AsT0.DialogId, EnduserContextRevision = rev1 },
                    new DialogRevisionDto { DialogId = res2.AsT0.DialogId, EnduserContextRevision = rev2 }
                ],
                SystemLabels = [SystemLabel.Values.Bin]
            }
        };

        var result = await Application.Send(command);
        result.TryPickT0(out _, out _).Should().BeTrue();

        var get1 = await Application.Send(new GetDialogQuery { DialogId = res1.AsT0.DialogId });
        get1.AsT0.SystemLabel.Should().Be(SystemLabel.Values.Bin);
        var get2 = await Application.Send(new GetDialogQuery { DialogId = res2.AsT0.DialogId });
        get2.AsT0.SystemLabel.Should().Be(SystemLabel.Values.Bin);
    }
}
