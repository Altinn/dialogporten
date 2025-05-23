using Digdir.Domain.Dialogporten.Application.Features.V1.Common.SystemLabels;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.DialogSystemLabels.Commands.Set;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.DialogSystemLabels;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SetDialogSystemLabelTests(DialogApplication application) : ApplicationCollectionFixture(application)
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
}
