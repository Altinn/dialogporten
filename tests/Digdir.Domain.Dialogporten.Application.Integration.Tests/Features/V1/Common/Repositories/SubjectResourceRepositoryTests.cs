using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Repositories;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SubjectResourceRepositoryTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task GetSubjectsForReferencedPartyResources_ShouldReturnOnlySubjectsForPartyResourceSchemaResources()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var referencedResource = $"urn:altinn:resource:referenced-{suffix}";
        var unprefixedReferencedResource = $"referenced-{suffix}";
        var unreferencedResource = $"urn:altinn:resource:unreferenced-{suffix}";
        const string subject = "urn:altinn:rolecode:DIALOG_READ";

        using (var scope = Application.GetServiceProvider().CreateScope())
        {
            var dataSource = scope.ServiceProvider.GetRequiredService<NpgsqlDataSource>();
            await using var connection = await dataSource.OpenConnectionAsync(TestContext.Current.CancellationToken);
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                INSERT INTO partyresource."Resource" ("UnprefixedResourceIdentifier")
                VALUES (@resource);
                """;
            command.Parameters.AddWithValue("resource", unprefixedReferencedResource);
            await command.ExecuteNonQueryAsync(TestContext.Current.CancellationToken);

            var db = scope.ServiceProvider.GetRequiredService<DialogDbContext>();
            db.SubjectResources.AddRange(
                new()
                {
                    Id = Guid.NewGuid(),
                    Resource = referencedResource,
                    Subject = subject,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                },
                new()
                {
                    Id = Guid.NewGuid(),
                    Resource = unreferencedResource,
                    Subject = subject,
                    CreatedAt = DateTimeOffset.UtcNow,
                    UpdatedAt = DateTimeOffset.UtcNow
                });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken);
        }

        using var queryScope = Application.GetServiceProvider().CreateScope();
        var sut = queryScope.ServiceProvider.GetRequiredService<ISubjectResourceRepository>();

        var result = await sut.GetSubjectsForReferencedPartyResources(TestContext.Current.CancellationToken);

        Assert.True(result.TryGetValue(referencedResource, out var subjects));
        Assert.Equal([subject], subjects);
        Assert.False(result.ContainsKey(unreferencedResource));
    }
}
