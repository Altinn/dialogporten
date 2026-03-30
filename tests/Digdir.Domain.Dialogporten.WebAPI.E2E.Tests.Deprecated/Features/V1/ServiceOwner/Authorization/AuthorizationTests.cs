using System.Net;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Deprecated.Features.V1.Authentication;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using Refit;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Deprecated.Features.V1.ServiceOwner.Authorization;

[Collection(nameof(WebApiTestCollectionFixture))]
public class AuthorizationTests(WebApiE2EFixture fixture) : E2ETestBase<WebApiE2EFixture>(fixture)
{
    private sealed class AuthorizationScenarioTestData : TheoryData<AuthorizationScenario>
    {
        public AuthorizationScenarioTestData()
        {
            Add(new AuthorizationScenario(
                "valid serviceowner",
                ShouldSucceed: true));

            Add(new AuthorizationScenario(
                "invalid serviceowner",
                ShouldSucceed: false,
                "310778737",
                "other"));

            Add(new AuthorizationScenario(
                "valid serviceowner admin",
                ShouldSucceed: true,
                "310778737",
                "other",
                $"{AuthorizationScope.ServiceProvider} {AuthorizationScope.ServiceOwnerAdminScope}"));
        }
    }

    [E2ETheory]
    [ClassData(typeof(AuthorizationScenarioTestData))]
    public async Task Should_Authorize_Dialog_Create(AuthorizationScenario scenario)
    {
        var createResponse = await WithScenarioOverrides(
            scenario,
            () => Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsCreateDialog(
                CreateSeedDialog(),
                TestContext.Current.CancellationToken));

        AssertStatus(createResponse, HttpStatusCode.Created, HttpStatusCode.Forbidden, scenario.ShouldSucceed);

        if (!scenario.ShouldSucceed)
        {
            return;
        }

        createResponse.Content.ToGuid()
            .Should().NotBeEmpty();
    }

    [E2ETheory]
    [ClassData(typeof(AuthorizationScenarioTestData))]
    public Task Should_Authorize_Get_Dialog(AuthorizationScenario scenario) =>
        AssertScenarioStatusOnDialog(
            scenario,
            async (dialogId, _, _) =>
            {
                var response = await Fixture.ServiceownerApi.GetDialog(dialogId);
                return response;
            },
            HttpStatusCode.OK,
            HttpStatusCode.NotFound);

    [E2ETheory]
    [ClassData(typeof(AuthorizationScenarioTestData))]
    public Task Should_Authorize_Patch_Dialog(AuthorizationScenario scenario) =>
        AssertScenarioStatusOnDialog(
            scenario,
            async (dialogId, _, _) =>
            {
                var response = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsPatchDialog(
                    dialogId,
                    BuildPatchDocument(),
                    etag: null,
                    TestContext.Current.CancellationToken);
                return response;
            },
            HttpStatusCode.NoContent,
            HttpStatusCode.NotFound);

    [E2ETheory]
    [ClassData(typeof(AuthorizationScenarioTestData))]
    public Task Should_Authorize_Update_Dialog(AuthorizationScenario scenario) =>
        AssertScenarioStatusOnDialog(
            scenario,
            async (dialogId, _, _) =>
            {
                var response = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsUpdateDialog(
                    dialogId,
                    CreateUpdateDialogDto(),
                    if_Match: null,
                    TestContext.Current.CancellationToken);
                return response;
            },
            HttpStatusCode.NoContent,
            HttpStatusCode.NotFound);

    [E2ETheory]
    [ClassData(typeof(AuthorizationScenarioTestData))]
    public Task Should_Authorize_Delete_Dialog(AuthorizationScenario scenario) =>
        AssertScenarioStatusOnDialog(
            scenario,
            async (dialogId, _, _) =>
            {
                var response = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsDeleteDialog(
                    dialogId,
                    if_Match: null,
                    TestContext.Current.CancellationToken);
                return response;
            },
            HttpStatusCode.NoContent,
            HttpStatusCode.NotFound);

    [E2ETheory]
    [ClassData(typeof(AuthorizationScenarioTestData))]
    public Task Should_Authorize_Get_Transmission_List(AuthorizationScenario scenario) =>
        AssertScenarioStatusOnDialog(
            scenario,
            async (dialogId, _, _) =>
            {
                var response = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesSearchTransmissionsDialogTransmission(
                    dialogId,
                    TestContext.Current.CancellationToken);
                return response;
            },
            HttpStatusCode.OK,
            HttpStatusCode.NotFound);

    [E2ETheory]
    [ClassData(typeof(AuthorizationScenarioTestData))]
    public Task Should_Authorize_Get_Transmission(AuthorizationScenario scenario) =>
        AssertScenarioStatusOnDialog(
            scenario,
            async (dialogId, transmissionId, _) =>
            {
                var response = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesGetTransnissionDialogTransmission(
                    dialogId,
                    transmissionId,
                    TestContext.Current.CancellationToken);
                return response;
            },
            HttpStatusCode.OK,
            HttpStatusCode.NotFound);

    [E2ETheory]
    [ClassData(typeof(AuthorizationScenarioTestData))]
    public Task Should_Authorize_Get_Activities(AuthorizationScenario scenario) =>
        AssertScenarioStatusOnDialog(
            scenario,
            async (dialogId, _, _) =>
            {
                var response = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesSearchActivitiesDialogActivity(
                    dialogId,
                    TestContext.Current.CancellationToken);
                return response;
            },
            HttpStatusCode.OK,
            HttpStatusCode.NotFound);

    [E2ETheory]
    [ClassData(typeof(AuthorizationScenarioTestData))]
    public Task Should_Authorize_Get_Activity(AuthorizationScenario scenario) =>
        AssertScenarioStatusOnDialog(
            scenario,
            async (dialogId, _, activityId) =>
            {
                var response = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesGetActivityDialogActivity(
                    dialogId,
                    activityId,
                    TestContext.Current.CancellationToken);
                return response;
            },
            HttpStatusCode.OK,
            HttpStatusCode.NotFound);

    [E2ETheory]
    [ClassData(typeof(AuthorizationScenarioTestData))]
    public Task Should_Authorize_Post_Activity(AuthorizationScenario scenario) =>
        AssertScenarioStatusOnDialog(
            scenario,
            async (dialogId, _, _) =>
            {
                var response = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsCreateActivityDialogActivity(
                    dialogId,
                    CreateActivityRequest(),
                    if_Match: null,
                    TestContext.Current.CancellationToken);
                return response;
            },
            HttpStatusCode.Created,
            HttpStatusCode.NotFound);

    public static TheoryData<EndpointScenario> AllServiceOwnerEndpoints =>
        new(AuthenticationTestHelpers.GetEndpointScenarios<IServiceownerApi>());

    [E2ETheory]
    [MemberData(nameof(AllServiceOwnerEndpoints))]
    public async Task Should_Return_Forbidden_Without_ServiceProvider_Scope(EndpointScenario endpointScenario)
    {
        using var _ = Fixture.UseServiceOwnerTokenOverrides(scopes: "wrong-scope");

        var response = await AuthenticationTestHelpers.InvokeEndpointAsync(
            Fixture.ServiceownerApi, endpointScenario.Method, TestContext.Current.CancellationToken);

        response.ShouldHaveStatusCode(HttpStatusCode.Forbidden);
    }

    [E2EFact]
    public async Task Should_Deny_Search_Without_Search_Scope()
    {
        using var _ = Fixture.UseServiceOwnerTokenOverrides(scopes: AuthorizationScope.ServiceProvider);

        var searchWithoutScopeResponse = await Fixture.ServiceownerApi
            .V1ServiceOwnerDialogsQueriesSearchDialog(
            new(),
            TestContext.Current.CancellationToken);

        searchWithoutScopeResponse.ShouldHaveStatusCode(HttpStatusCode.Forbidden);
    }

    private async Task AssertScenarioStatusOnDialog(
        AuthorizationScenario scenario,
        Func<Guid, Guid, Guid, Task<IApiResponse>> operation,
        HttpStatusCode expectedSuccess,
        HttpStatusCode expectedFailure)
    {
        var (dialogId, transmissionId, activityId) = await CreateAuthorizedDialogWithIdentifiersAsync();

        var response = await WithScenarioOverrides(
            scenario,
            () => operation(dialogId, transmissionId, activityId));

        AssertStatus(response, expectedSuccess, expectedFailure, scenario.ShouldSucceed);
    }

    private async Task<(Guid dialogId, Guid transmissionId, Guid activityId)> CreateAuthorizedDialogWithIdentifiersAsync()
    {
        var createResponse = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsCreateDialog(
            CreateSeedDialog(),
            TestContext.Current.CancellationToken);

        createResponse.ShouldHaveStatusCode(HttpStatusCode.Created);
        var dialogId = createResponse.Content.ToGuid();

        var getDialogResponse = await Fixture.ServiceownerApi.GetDialog(dialogId);

        getDialogResponse.ShouldHaveStatusCode(HttpStatusCode.OK);

        var dialog = getDialogResponse.Content ?? throw new InvalidOperationException("Expected dialog content.");
        var transmissionId = dialog.Transmissions.Should().NotBeEmpty().And.Subject.First().Id;
        var activityId = dialog.Activities.Should().NotBeEmpty().And.Subject.First().Id;

        return (dialogId, transmissionId, activityId);
    }

    private static V1ServiceOwnerDialogsCommandsCreate_Dialog CreateSeedDialog() =>
        DialogTestData.CreateSimpleDialog()
            .AddTransmission()
            .AddApiAction()
            .AddActivity();


    private static V1ServiceOwnerDialogsCommandsUpdate_Dialog CreateUpdateDialogDto() =>
        new()
        {
            Status = V1ServiceOwnerCommonDialogStatuses_DialogStatusInput.NotApplicable,
            Content = new()
            {
                Title = DialogTestData.CreateContentValue(
                    value: "Updated title",
                    languageCode: "nb")
            }
        };

    private static List<JsonPatchOperations_Operation> BuildPatchDocument() =>
    [
        new()
        {
            OperationType = JsonPatchOperations_OperationType.Replace,
            Op = "replace",
            Path = "/apiActions/0/endpoints/1/url",
            Value = "https://vg.no"
        }
    ];

    private static V1ServiceOwnerDialogsCommandsCreateActivity_ActivityRequest CreateActivityRequest() =>
        new()
        {
            Type = Altinn.ApiClients.Dialogporten.Features.V1.DialogsEntitiesActivities_DialogActivityType.DialogCreated,
            PerformedBy = new()
            {
                ActorType = Altinn.ApiClients.Dialogporten.Features.V1.Actors_ActorType.PartyRepresentative,
                ActorName = "Some custom name"
            }
        };

    private async Task<T> WithScenarioOverrides<T>(AuthorizationScenario scenario, Func<Task<T>> action)
    {
        if (scenario.OrgNumber is null && scenario.OrgName is null && scenario.Scopes is null)
        {
            return await action();
        }

        using var _ = Fixture.UseServiceOwnerTokenOverrides(
            orgNumber: scenario.OrgNumber,
            orgName: scenario.OrgName,
            scopes: scenario.Scopes);

        return await action();
    }

    private static void AssertStatus(
        IApiResponse actual,
        HttpStatusCode success,
        HttpStatusCode failure,
        bool shouldSucceed) =>
        actual.ShouldHaveStatusCode(shouldSucceed ? success : failure);

}
