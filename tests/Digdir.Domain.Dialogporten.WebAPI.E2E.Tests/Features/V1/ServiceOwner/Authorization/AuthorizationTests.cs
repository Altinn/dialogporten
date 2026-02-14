using System.Net;
using Altinn.ApiClients.Dialogporten.Features.V1;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Library.Dialogporten.E2E.Common;
using Digdir.Library.Dialogporten.E2E.Common.Extensions;
using Xunit;

namespace Digdir.Domain.Dialogporten.WebAPI.E2E.Tests.Features.V1.ServiceOwner.Authorization;

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
                CreateAuthorizationDialog(),
                TestContext.Current.CancellationToken));

        AssertStatus(createResponse.StatusCode, HttpStatusCode.Created, HttpStatusCode.Forbidden, scenario.ShouldSucceed);

        if (!scenario.ShouldSucceed)
        {
            return;
        }

        _ = createResponse.Content.ToGuid();
    }

    [E2ETheory]
    [ClassData(typeof(AuthorizationScenarioTestData))]
    public Task Should_Authorize_Get_Dialog(AuthorizationScenario scenario) =>
        AssertScenarioStatusOnDialog(
            scenario,
            async (dialogId, _, _) =>
            {
                var response = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesGetDialog(
                    dialogId,
                    endUserId: null!,
                    TestContext.Current.CancellationToken);
                return response.StatusCode;
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
                return response.StatusCode;
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
                return response.StatusCode;
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
                return response.StatusCode;
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
                return response.StatusCode;
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
                return response.StatusCode;
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
                return response.StatusCode;
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
                return response.StatusCode;
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
                return response.StatusCode;
            },
            HttpStatusCode.Created,
            HttpStatusCode.NotFound);

    [E2EFact]
    public async Task Should_Deny_Search_Without_Search_Scope()
    {
        using var _ = Fixture.UseServiceOwnerTokenOverrides(scopes: AuthorizationScope.ServiceProvider);

        var searchWithoutScopeResponse = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesSearchDialog(
            new V1ServiceOwnerDialogsQueriesSearchDialogQueryParams(),
            TestContext.Current.CancellationToken);

        searchWithoutScopeResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    private async Task AssertScenarioStatusOnDialog(
        AuthorizationScenario scenario,
        Func<Guid, Guid, Guid, Task<HttpStatusCode>> operation,
        HttpStatusCode expectedSuccess,
        HttpStatusCode expectedFailure)
    {
        var (dialogId, transmissionId, activityId) = await CreateAuthorizedDialogWithIdentifiersAsync();

        var statusCode = await WithScenarioOverrides(
            scenario,
            () => operation(dialogId, transmissionId, activityId));

        AssertStatus(statusCode, expectedSuccess, expectedFailure, scenario.ShouldSucceed);
    }

    private async Task<(Guid dialogId, Guid transmissionId, Guid activityId)> CreateAuthorizedDialogWithIdentifiersAsync()
    {
        var createResponse = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsCommandsCreateDialog(
            CreateAuthorizationDialog(),
            TestContext.Current.CancellationToken);

        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var dialogId = createResponse.Content.ToGuid();

        var getDialogResponse = await Fixture.ServiceownerApi.V1ServiceOwnerDialogsQueriesGetDialog(
            dialogId,
            endUserId: null!,
            TestContext.Current.CancellationToken);

        getDialogResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var dialog = getDialogResponse.Content ?? throw new InvalidOperationException("Expected dialog content.");
        var transmissionId = dialog.Transmissions.Should().NotBeEmpty().And.Subject.First().Id;
        var activityId = dialog.Activities.Should().NotBeEmpty().And.Subject.First().Id;

        return (dialogId, transmissionId, activityId);
    }

    private static V1ServiceOwnerDialogsCommandsCreate_Dialog CreateAuthorizationDialog()
    {
        var dialog = DialogTestData.CreateSimpleDialog();

        dialog.AddTransmission();

        dialog.ApiActions =
        [
            new V1ServiceOwnerDialogsCommandsCreate_ApiAction
            {
                Action = "some_unauthorized_action",
                Name = "confirm",
                Endpoints =
                [
                    new V1ServiceOwnerDialogsCommandsCreate_ApiActionEndpoint
                    {
                        Url = new Uri("https://digdir.no"),
                        HttpMethod = Http_HttpVerb.GET
                    },
                    new V1ServiceOwnerDialogsCommandsCreate_ApiActionEndpoint
                    {
                        Url = new Uri("https://digdir.no/deprecated"),
                        HttpMethod = Http_HttpVerb.GET,
                        Deprecated = true
                    }
                ]
            }
        ];

        dialog.Activities =
        [
            new V1ServiceOwnerDialogsCommandsCreate_Activity
            {
                Type = DialogsEntitiesActivities_DialogActivityType.DialogCreated,
                PerformedBy = new V1ServiceOwnerCommonActors_Actor
                {
                    ActorType = Actors_ActorType.PartyRepresentative,
                    ActorName = "Some custom name"
                }
            }
        ];

        return dialog;
    }

    private static V1ServiceOwnerDialogsCommandsUpdate_Dialog CreateUpdateDialogDto() =>
        new()
        {
            Status = V1ServiceOwnerCommonDialogStatuses_DialogStatusInput.NotApplicable,
            Content = new V1ServiceOwnerDialogsCommandsUpdate_Content
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
            Type = DialogsEntitiesActivities_DialogActivityType.DialogCreated,
            PerformedBy = new V1ServiceOwnerCommonActors_Actor
            {
                ActorType = Actors_ActorType.PartyRepresentative,
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
        HttpStatusCode actual,
        HttpStatusCode success,
        HttpStatusCode failure,
        bool shouldSucceed) =>
        actual.Should().Be(shouldSucceed ? success : failure);

    public sealed record AuthorizationScenario(
        string Name,
        bool ShouldSucceed,
        string? OrgNumber = null,
        string? OrgName = null,
        string? Scopes = null)
    {
        public override string ToString() => Name;
    }
}
