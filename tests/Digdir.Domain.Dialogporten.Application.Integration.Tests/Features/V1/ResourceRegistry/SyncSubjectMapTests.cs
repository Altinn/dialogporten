using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Features.V1.ResourceRegistry.Commands.SyncSubjectMap;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using OneOf.Types;

#pragma warning disable CA1305

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ResourceRegistry;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SyncSubjectMapTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task SyncSubjectMapCommand_Should_Execute_Successfully_With_Whitespace_URIs() =>
        FlowBuilder
            .For(Application, ConfigureResourceRegistry())
            .SendCommand(_ => new SyncSubjectMapCommand { Since = DateTimeOffset.MinValue })
            .ExecuteAndAssert<Success>();

    private static async IAsyncEnumerable<List<UpdatedSubjectResource>> GetTestValues()
    {
        yield return
        [
            new UpdatedSubjectResource(new Uri("urn:altinn:rolecode:priv"),
                new Uri("urn:altinn:resource:some_app"),
                DateTimeOffset.Parse("2025-07-25T09:48:10.010819+00:00"),
                true),
            new UpdatedSubjectResource(new Uri("urn:altinn:rolecode:priv\n                        "),
                new Uri("urn:altinn:resource:some_app"),
                DateTimeOffset.Parse("2025-07-25T09:48:10.010819+00:00"),
                false),

            new UpdatedSubjectResource(new Uri("urn:altinn:rolecode:dagl"),
                new Uri("urn:altinn:resource:another_app"),
                DateTimeOffset.Parse("2025-07-25T10:32:37.827098+00:00"),
                false),

            new UpdatedSubjectResource(new Uri("urn:altinn:rolecode:priv"),
                new Uri("urn:altinn:resource:another_app"),
                DateTimeOffset.Parse("2025-07-25T10:32:37.827098+00:00"),
                false),
        ];

        await Task.CompletedTask;
    }

    private static Action<IServiceCollection>? ConfigureResourceRegistry() =>
        x =>
        {
            x.RemoveAll<IResourceRegistry>();

            var resourceRegistry = Substitute.For<IResourceRegistry>();

            resourceRegistry.GetUpdatedSubjectResources(
                Arg.Any<DateTimeOffset>(),
                Arg.Any<int>(),
                Arg.Any<CancellationToken>()
            ).Returns(GetTestValues());

            x.AddSingleton(resourceRegistry);
        };
}
