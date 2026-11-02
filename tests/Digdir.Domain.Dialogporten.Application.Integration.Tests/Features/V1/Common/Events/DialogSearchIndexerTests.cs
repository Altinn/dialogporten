using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Events.DialogSearch;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Configurations.Dialogs.Search;
using AwesomeAssertions;
using Microsoft.EntityFrameworkCore;
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

    [Fact]
    public async Task DialogSearchIndexer_Should_Upsert_SearchVector_When_Related_CreatedAt_Is_Infinity()
    {
        // Regression test for the DLQ error "0A000: cannot convert infinity to bigint".
        // Npgsql maps DateTimeOffset.MaxValue to the PostgreSQL special timestamp value 'infinity',
        // which the search document view would feed into EXTRACT(EPOCH ...)::bigint and fail on.
        var result = await FlowBuilder.For(Application)
            .CreateComplexDialog()
            .ExecuteAndAssert<CreateDialogSuccess>();

        using (var scope = Application.GetServiceProvider().CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<DialogDbContext>();
            foreach (var table in new[] { "DialogTransmission", "DialogActivity", "Attachment" })
            {
                // Table name is a controlled constant; DialogId is passed as a bound parameter.
                var sql = "UPDATE \"" + table + "\" SET \"CreatedAt\" = 'infinity' WHERE \"DialogId\" = {0}";
                await db.Database.ExecuteSqlRawAsync(sql, [result.DialogId]);
            }
        }

        var sut = ActivatorUtilities.CreateInstance<DialogSearchIndexer>(Application.GetServiceProvider());

        var act = async () => await sut.Handle(
            new DialogUpdatedDomainEvent(result.DialogId, null!, null!, null, null), CancellationToken.None);

        await act.Should().NotThrowAsync();

        var searchEntities = await Application.GetDbEntities<DialogSearch>();
        searchEntities.Should().ContainSingle(x => x.DialogId == result.DialogId);
    }
}
