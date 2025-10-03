using Altinn.ApiClients.Dialogporten.Features.V1;
using FluentAssertions;
using Xunit.Abstractions;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace Digdir.Domain.Dialogporten.WebApiClient.E2E.Tests;

public class CreateTests : TestBed<AuthorizedE2EFixture>
{
    private readonly IServiceownerApi _serviceownerApi;
    // private readonly Guid _sentinelId = Guid.NewGuid();

    public CreateTests(ITestOutputHelper testOutputHelper, AuthorizedE2EFixture fixture) : base(testOutputHelper,
        fixture)
    {
        _serviceownerApi = fixture.GetService<IServiceownerApi>(_testOutputHelper)!;
    }

    //
    // public new async ValueTask DisposeAsync()
    // {
    //     Console.WriteLine("[CLEANUP] Disposing E2EFixture 🧹");
    //
    //     Console.WriteLine(_sentinelId.ToString());
    //     // 🔥 Do any teardown work here
    //     await Task.Delay(500); // Simulate async cleanup
    //
    //     await base.DisposeAsync(); // Ensure base cleanup runs
    // }

    [ConditionalSkipFact]
    public async Task Can_Create_Simple_Dialog()
    {
        var createDialogResponse = await _serviceownerApi
            .V1ServiceOwnerDialogsCommandsCreateDialog(
                new V1ServiceOwnerDialogsCommandsCreate_Dialog(),
                CancellationToken.None);

        createDialogResponse.IsSuccessful.Should().BeTrue();
    }
}
