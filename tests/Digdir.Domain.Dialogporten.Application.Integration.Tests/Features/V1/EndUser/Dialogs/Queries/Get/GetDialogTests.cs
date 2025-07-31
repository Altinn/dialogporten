using Castle.Components.DictionaryAdapter.Xml;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Commands.SetSystemLabel;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Dialogs.Queries.Get;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class GetDialogTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Get_Should_Populate_EnduserContextRevision() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.EndUserContext.Revision.Should().NotBeEmpty());

    [Fact]
    public Task Get_Should_Remove_MarkedAsUnopened_SystemLabel() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SendCommand((_, ctx) => new SetSystemLabelCommand
            {
                DialogId = ctx.GetDialogId(),
                AddLabels = [SystemLabel.Values.MarkedAsUnopened]
            })
            .SendCommand((_, ctx) => GetDialog(ctx.GetDialogId()))
            .ExecuteAndAssert<DialogDto>(x =>
                x.EndUserContext.SystemLabels.Should().NotContain(SystemLabel.Values.MarkedAsUnopened));


    [Fact]
    [Obsolete("Testing obsolete SystemLabel, will be removed in future versions.")]
    public Task Get_Should_Populate_Obsolete_SystemLabel() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.SystemLabel.Should()
                    .Be(SystemLabel.Values.Default));

    private static GetDialogQuery GetDialog(Guid? id) => new() { DialogId = id!.Value };
}
