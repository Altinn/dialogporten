using Altinn.ApiClients.Dialogporten;
using Altinn.ApiClients.Dialogporten.Features.V1;
using Altinn.ApiClients.Maskinporten.Config;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using Xunit.Abstractions;
using Xunit.Microsoft.DependencyInjection;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace Digdir.Domain.Dialogporten.WebApiClient.E2E.Tests;

public class E2EFixture : TestBedFixture
{
    protected override void AddServices(IServiceCollection services, IConfiguration? configuration)
        => services.AddDialogportenClient(new DialogportenSettings
        {
            BaseUri = "https://localhost:7214",
            Maskinporten = new MaskinportenSettings
            {
                ClientId = "",
                EncodedJwk = "",
                Environment = "test",
                Scope = "digdir:dialogporten.serviceprovider",
            }
        });

    protected override ValueTask DisposeAsyncCore()
        => new();

    protected override IEnumerable<TestAppSettings> GetTestAppSettings()
    {
        yield return new() { Filename = "appsettings.json", IsOptional = false };
    }

    protected override void AddUserSecrets(IConfigurationBuilder configurationBuilder)
        => configurationBuilder.AddUserSecrets<TestProjectFixture>();
}

public class TestTests : TestBed<E2EFixture>
{
    private new readonly ITestOutputHelper _testOutputHelper;
    private readonly IServiceownerApi _serviceownerApi;

    public TestTests(ITestOutputHelper testOutputHelper, E2EFixture fixture) : base(testOutputHelper, fixture)
    {
        _testOutputHelper = testOutputHelper;
        _serviceownerApi = fixture.GetService<IServiceownerApi>(_testOutputHelper)!;
    }

    // [ConditionalSkipFact]
    [Fact]
    public async Task Test1()
    {
        var guid = Guid.NewGuid();
        var getDialogResponse = await _serviceownerApi.V1ServiceOwnerDialogsGetGetDialog(guid, null!, CancellationToken.None);
        _testOutputHelper.WriteLine(getDialogResponse.ToString());
        Assert.True(true);
    }

    // [ConditionalSkipFact]
    // public void Test2()
    // {
    //     Assert.True(false);
    // }
}
