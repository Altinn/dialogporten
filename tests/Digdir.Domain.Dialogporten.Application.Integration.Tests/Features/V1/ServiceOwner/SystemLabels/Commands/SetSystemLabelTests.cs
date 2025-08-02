using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.EndUserContext.Commands.SetSystemLabels;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using FluentAssertions;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.SystemLabels.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SetSystemLabelTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Set_Updates_System_Label() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SetSystemLabelsServiceOwner(x => x.AddLabels = [SystemLabel.Values.Bin])
            .SendCommand((_, ctx) => GetDialog(ctx.GetDialogId()))
            .ExecuteAndAssert<DialogDto>(x =>
                x.EndUserContext.SystemLabels.FirstOrDefault().Should().Be(SystemLabel.Values.Bin));

    [Fact]
    public Task Set_Returns_ConcurrencyError_On_Revision_Mismatch() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SetSystemLabelsServiceOwner(x =>
            {
                x.IfMatchEndUserContextRevision = NewUuidV7();
                x.AddLabels = [SystemLabel.Values.Bin];
            })
            .ExecuteAndAssert<ConcurrencyError>();

    [Fact]
    public async Task Set_Succeeds_On_Revision_Match()
    {
        Guid? dialogId = NewUuidV7();
        string? party = null;
        Guid? revision = null;

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x => x.Dto.Id = dialogId)
            .GetServiceOwnerDialog()
            .AssertResult<DialogDto>(x =>
            {
                party = x.Party;
                revision = x.EndUserContext.Revision;
            })
            .SendCommand(_ => new SetSystemLabelCommand
            {
                EndUserId = party!,
                DialogId = dialogId.Value,
                IfMatchEndUserContextRevision = revision!.Value,
                AddLabels = [SystemLabel.Values.Bin]
            })
            .SendCommand(_ => GetDialog(dialogId))
            .ExecuteAndAssert<DialogDto>(x =>
                x.EndUserContext.SystemLabels.FirstOrDefault().Should().Be(SystemLabel.Values.Bin));
    }

    [Fact]
    public Task Can_Set_And_Remove_MarkedAsUnopened_Label() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SetSystemLabelsServiceOwner(x =>
                x.AddLabels = [SystemLabel.Values.MarkedAsUnopened])
            .SendCommand((_, ctx) => GetDialog(ctx.GetDialogId()))
            .AssertResult<DialogDto>(x =>
            {
                x.EndUserContext.SystemLabels.Should().ContainSingle(x => x == SystemLabel.Values.MarkedAsUnopened);
                x.EndUserContext.SystemLabels.Should().ContainSingle(x => x == SystemLabel.Values.Default);
            })
            .SendCommand((_, ctx) => new SetSystemLabelCommand
            {
                EndUserId = ctx.GetParty(),
                DialogId = ctx.GetDialogId(),
                RemoveLabels = [SystemLabel.Values.MarkedAsUnopened]
            })
            .SendCommand((_, ctx) => GetDialog(ctx.GetDialogId()))
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.EndUserContext.SystemLabels.Should().NotContain(x => x == SystemLabel.Values.MarkedAsUnopened);
                x.EndUserContext.SystemLabels.Should().ContainSingle(x => x == SystemLabel.Values.Default);
            });

    [Fact]
    public Task Cannot_Set_Sent_System_Label() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SetSystemLabelsServiceOwner(x =>
                x.AddLabels = [SystemLabel.Values.Sent])
            .ExecuteAndAssert<ValidationError>(x =>
                x.ShouldHaveErrorWithText(
                    ValidationErrorStrings.SentLabelNotAllowed));

    [Fact]
    public Task Cannot_Remove_Existing_Sent_System_Label() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
                x.AddTransmission(x =>
                    x.Type = DialogTransmissionType.Values.Submission))
            .SetSystemLabelsServiceOwner(x =>
                x.RemoveLabels = [SystemLabel.Values.Sent])
            .ExecuteAndAssert<ValidationError>(x =>
                x.ShouldHaveErrorWithText(
                    ValidationErrorStrings.SentLabelNotAllowed));

    private static GetDialogQuery GetDialog(Guid? id) => new() { DialogId = id!.Value };
}
