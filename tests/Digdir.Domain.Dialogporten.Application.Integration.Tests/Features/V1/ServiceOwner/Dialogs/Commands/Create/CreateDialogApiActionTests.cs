using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain.Http;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Commands.Create;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class CreateDialogApiActionTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task Can_Create_Api_Action_Endpoint_With_Supplied_Id()
    {
        var apiActionId = NewUuidV7();
        var apiActionEndpointId = NewUuidV7();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.ApiActions =
                [
                    new()
                    {
                        Id = apiActionId,
                        Action = "read",
                        Endpoints =
                        [
                            new()
                            {
                                Id = apiActionEndpointId,
                                HttpMethod = HttpVerb.Values.GET,
                                Url = new Uri("https://example.com/api/read")
                            }
                        ]
                    }
                ];
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
}
