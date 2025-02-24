using System.Net;
using Altinn.ApiClients.Dialogporten.Features.V1;
using FluentAssertions;
using Xunit.Abstractions;
using Xunit.Microsoft.DependencyInjection.Abstracts;

namespace Digdir.Domain.Dialogporten.WebApiClient.E2E.Tests;

public class NotFoundTests : TestBed<E2EFixture>
{
    private readonly IServiceownerApi _serviceownerApi;
    private readonly Guid _sentinelId = Guid.NewGuid();

    public NotFoundTests(ITestOutputHelper testOutputHelper, E2EFixture fixture) : base(testOutputHelper, fixture)
    {
        _serviceownerApi = fixture.GetService<IServiceownerApi>(_testOutputHelper)!;
    }

    // public override async ValueTask DisposeAsync()
    // {
        // var searchResult = await _serviceownerApi
            // .V1ServiceOwnerDialogsSearchSearchDialog(new V1ServiceOwnerDialogsSearchSearchDialogQueryParams()
            // {
                // Limit = 1000,
                // Search = _sentinelId.ToString()
            // });

        // Console.WriteLine(searchResult);
    // }

    [ConditionalSkipFact]
    public async Task Get_Dialog_With_Invalid_DialogId_Should_Return_NotFound()
    {
        var dialogId = Guid.NewGuid();

        var getDialogResponse = await _serviceownerApi
            .V1ServiceOwnerDialogsGetGetDialog(dialogId, null!, CancellationToken.None);

        getDialogResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [ConditionalSkipFact]
    public async Task Get_Transmission_With_Invalid_DialogId_Should_Return_NotFound()
    {
        var dialogId = Guid.NewGuid();
        var transmissionId = Guid.NewGuid();

        var getTransmissionResponse = await _serviceownerApi
            .V1ServiceOwnerDialogTransmissionsGetGetDialogTransmission(dialogId, transmissionId, CancellationToken.None);

        getTransmissionResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [ConditionalSkipFact]
    public async Task Search_Transmission_With_Invalid_TransmissionId_Should_Return_NotFound()
    {
        var dialogId = Guid.NewGuid();

        var getTransmissionResponse = await _serviceownerApi
            .V1ServiceOwnerDialogTransmissionsSearchSearchDialogTransmission(dialogId, CancellationToken.None);

        getTransmissionResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
