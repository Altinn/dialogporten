using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Domain.Http;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Commands.Update;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class UpdateDialogApiActionTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task Can_Create_Api_Action_Endpoint_With_Supplied_Id()
    {
        var apiActionId = NewUuidV7();
        var apiActionEndpointId = NewUuidV7();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .UpdateDialog(x =>
            {
                x.AddApiAction(apiAction =>
                {
                    apiAction.Id = apiActionId;
                    apiAction.Action = "read";
                    apiAction.Endpoints.Clear();
                    apiAction.AddEndpoint(apiActionEndpoint =>
                    {
                        apiActionEndpoint.Id = apiActionEndpointId;
                        apiActionEndpoint.HttpMethod = HttpVerb.Values.GET;
                        apiActionEndpoint.Url = new Uri("https://example.com/api/read");
                    });
                });
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                var apiAction = x.ApiActions.Single();
                apiAction.Id.Should().Be(apiActionId);
                apiAction.Endpoints.Should().ContainSingle()
                    .Which.Id.Should().Be(apiActionEndpointId);
            });
    }

    [Fact]
    public async Task Can_Update_Dialog_With_New_ApiActionEndpoint_Having_User_Defined_Id()
    {
        var apiActionId = NewUuidV7();
        var apiActionEndpointId = NewUuidV7();
        var newApiActionEndpointId = NewUuidV7();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
                x.AddApiAction(apiAction =>
                {
                    apiAction.Id = apiActionId;
                    apiAction.Endpoints.Clear();
                    apiAction.AddEndpoint(apiActionEndpoint =>
                    {
                        apiActionEndpoint.Id = apiActionEndpointId;
                    });
                }))
            .UpdateDialog(x =>
            {
                var apiAction = x.Dto.ApiActions.First();
                apiAction.Endpoints.Clear();
                apiAction.AddEndpoint(apiActionEndpoint =>
                    apiActionEndpoint.Id = newApiActionEndpointId);
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                var apiAction = x.ApiActions.Single();
                apiAction.Id.Should().Be(apiActionId);
                apiAction.Endpoints.Should().ContainSingle()
                    .Which.Id.Should().Be(newApiActionEndpointId);
            });
    }
}
