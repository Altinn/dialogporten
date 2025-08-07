using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Commands.SetSystemLabel;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.ResourceRegistry;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using static Digdir.Domain.Dialogporten.Application.Common.ResourceRegistry.Constants;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

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

    [Theory]
    [InlineData(DialogActivityType.Values.CorrespondenceOpened, false)]
    [InlineData(DialogActivityType.Values.Information, true)]
    public Task Get_Correspondence_Sets_HasUnopenedContent_Correctly_Based_On_Activities(
        DialogActivityType.Values activityType, bool expectedHasUnOpenedContent) =>
        FlowBuilder.For(Application, x =>
            {
                x.RemoveAll<IUser>();
                x.AddSingleton<IUser>(CreateUserWithScope(AuthorizationScope.CorrespondenceScope));

                x.RemoveAll<IResourceRegistry>();
                x.AddScoped<IResourceRegistry, TestResourceRegistry>();
            })
            .CreateSimpleDialog(x => x.AddActivity(activityType))
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.HasUnopenedContent.Should().Be(expectedHasUnOpenedContent));
}


internal sealed class TestResourceRegistry(DialogDbContext db) : LocalDevelopmentResourceRegistry(db)
{
    public override Task<ServiceResourceInformation?> GetResourceInformation(string serviceResourceId,
        CancellationToken cancellationToken) =>
        Task.FromResult<ServiceResourceInformation?>(
            new ServiceResourceInformation(serviceResourceId, CorrespondenceService, "SomeOrg", "org"));
}
