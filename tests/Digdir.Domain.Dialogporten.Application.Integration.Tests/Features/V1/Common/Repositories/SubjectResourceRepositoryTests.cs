using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Repositories;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SubjectResourceRepositoryTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task GetSubjectsForReferencedPartyResources_ShouldReturnOnlySubjectsForPartyResourceSchemaResources()
    {
        var suffix = Guid.NewGuid().ToString("N");
        var referencedResource = $"urn:altinn:resource:referenced-{suffix}";
        var unreferencedResource = $"urn:altinn:resource:unreferenced-{suffix}";
        const string subject = "urn:altinn:rolecode:DIALOG_READ";

        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.Dto.ServiceResource = referencedResource)
            .ExecuteAsync();

        using (var scope = Application.GetServiceProvider().CreateScope())
        {
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
