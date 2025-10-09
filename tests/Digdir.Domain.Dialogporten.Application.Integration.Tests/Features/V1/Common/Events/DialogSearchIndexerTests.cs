using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Events.DialogSearch;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Configurations.Dialogs.Search;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Events;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class DialogSearchIndexerTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task DialogSearchIndexer_Upsert_Create_SearchVector_On_DialogCreatedDomainEvent()
    {
        var result = await FlowBuilder.For(Application)
            .CreateComplexDialog()
            .ExecuteAndAssert<CreateDialogSuccess>();
        var sut = ActivatorUtilities.CreateInstance<DialogSearchIndexer>(Application.GetServiceProvider());
        await sut.Handle(new DialogCreatedDomainEvent(result.DialogId, null!, null!, null, null), CancellationToken.None);
        var searchEntities = await Application.GetDbEntities<DialogSearch>();
        searchEntities.Should().HaveCount(1)
            .And.Subject.First().DialogId.Should().Be(result.DialogId);
    }

    [Fact]
    public async Task DialogSearchIndexer_Should_Upsert_SearchVector_On_DialogUpdatedDomainEvent()
    {
        var result = await FlowBuilder.For(Application)
            .CreateComplexDialog()
            .ExecuteAndAssert<CreateDialogSuccess>();
        var sut = ActivatorUtilities.CreateInstance<DialogSearchIndexer>(Application.GetServiceProvider());
        await sut.Handle(new DialogUpdatedDomainEvent(result.DialogId, null!, null!, null, null), CancellationToken.None);
        var searchEntities = await Application.GetDbEntities<DialogSearch>();
        searchEntities.Should().HaveCount(1)
            .And.Subject.First().DialogId.Should().Be(result.DialogId);
    }
}
