using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
using GetDialogLookupQuery = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.DialogLookup.Queries.Get.GetDialogLookupQuery;
using EndUserIdentifierLookupDto = Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup.EndUserIdentifierLookupDto;
using IdentifierLookupGrantType = Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup.IdentifierLookupGrantType;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.DialogLookup.Queries.Get;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class GetDialogLookupTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Get_Should_Return_NotFound_For_Deleted_Dialog() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.AddServiceOwnerLabels($"urn:altinn:app-instance-id:{Guid.NewGuid()}"))
            .DeleteDialog()
            .SendCommand((_, ctx) => new GetDialogLookupQuery
            {
                InstanceUrn = $"urn:altinn:dialog-id:{ctx.GetDialogId()}"
            })
            .ExecuteAndAssert<Digdir.Domain.Dialogporten.Application.Common.ReturnTypes.EntityNotFound>(_ => { });

    [Fact]
    public Task Get_By_Label_Should_Pick_Newest_NonDeleted_Dialog_For_EndUser()
    {
        var instanceId = Guid.NewGuid();
        var instanceUrn = $"urn:altinn:app-instance-id:{instanceId}";
        var olderDialogId = NewUuidV7();
        var newerDeletedDialogId = NewUuidV7();

        return FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.Id = olderDialogId;
                x.AddServiceOwnerLabels(instanceUrn);
            })
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.Id = newerDeletedDialogId;
                x.AddServiceOwnerLabels(instanceUrn);
            })
            .DeleteDialog()
            .SendCommand(_ => new GetDialogLookupQuery
            {
                InstanceUrn = instanceUrn
            })
            .ExecuteAndAssert<EndUserIdentifierLookupDto>(result =>
            {
                result.DialogId.Should().Be(olderDialogId);
                result.InstanceUrn.Should().Be(instanceUrn.ToLowerInvariant());
            });
    }

    [Fact]
    public Task Get_By_AppInstanceUrn_Should_Return_Newest_Dialog_When_Multiple_Labels_Match()
    {
        var instanceId = Guid.NewGuid();
        var instanceUrn = $"urn:altinn:app-instance-id:{instanceId}";
        var firstDialogId = NewUuidV7();
        var secondDialogId = NewUuidV7();
        var expectedDialogId = firstDialogId.CompareTo(secondDialogId) > 0 ? firstDialogId : secondDialogId;

        return FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.Id = firstDialogId;
                x.AddServiceOwnerLabels(instanceUrn);
            })
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.Id = secondDialogId;
                x.AddServiceOwnerLabels(instanceUrn);
            })
            .SendCommand(_ => new GetDialogLookupQuery
            {
                InstanceUrn = instanceUrn
            })
            .ExecuteAndAssert<EndUserIdentifierLookupDto>(result =>
            {
                result.DialogId.Should().Be(expectedDialogId);
                result.InstanceUrn.Should().Be(instanceUrn.ToLowerInvariant());
            });
    }

    [Fact]
    public Task Get_By_CorrespondenceUrn_Should_Return_Matching_Dialog()
    {
        var correspondenceId = Guid.NewGuid();
        var correspondenceUrn = $"urn:altinn:correspondence-id:{correspondenceId}";

        return FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.AddServiceOwnerLabels(correspondenceUrn))
            .SendCommand((_, ctx) => new GetDialogLookupQuery
            {
                InstanceUrn = correspondenceUrn
            })
            .ExecuteAndAssert<EndUserIdentifierLookupDto>((result, ctx) =>
            {
                result.DialogId.Should().Be(ctx.GetDialogId());
                result.InstanceUrn.Should().Be(correspondenceUrn.ToLowerInvariant());
            });
    }

    [Fact]
    public Task Get_By_DialogUrn_Should_Prefer_AppInstanceUrn_Then_CorrespondenceUrn()
    {
        var appInstanceUrn = $"urn:altinn:app-instance-id:{Guid.NewGuid()}";
        var correspondenceUrn = $"urn:altinn:correspondence-id:{Guid.NewGuid()}";

        return FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.AddServiceOwnerLabels(correspondenceUrn, appInstanceUrn))
            .SendCommand((_, ctx) => new GetDialogLookupQuery
            {
                InstanceUrn = $"urn:altinn:dialog-id:{ctx.GetDialogId()}"
            })
            .ExecuteAndAssert<EndUserIdentifierLookupDto>(result =>
                result.InstanceUrn.Should().Be(appInstanceUrn.ToLowerInvariant()));
    }

    [Fact]
    public Task Get_Should_Return_Forbidden_When_EndUser_Has_No_Access() =>
        FlowBuilder.For(Application, services =>
            {
                services.ConfigureAltinnAuthorization(altinnAuthorization =>
                {
                    altinnAuthorization.GetAuthorizedPartiesForLookup(
                            default!,
                            Arg.Any<List<string>>(),
                            Arg.Any<CancellationToken>())
                        .ReturnsForAnyArgs(new AuthorizedPartiesResult { AuthorizedParties = [] });

                    altinnAuthorization.UserHasRequiredAuthLevel(
                            Arg.Any<string>(),
                            Arg.Any<CancellationToken>())
                        .Returns(true);
                });
            })
            .CreateSimpleDialog((x, _) => x.AddServiceOwnerLabels($"urn:altinn:app-instance-id:{Guid.NewGuid()}"))
            .SendCommand((_, ctx) => new GetDialogLookupQuery
            {
                InstanceUrn = $"urn:altinn:dialog-id:{ctx.GetDialogId()}"
            })
            .ExecuteAndAssert<Digdir.Domain.Dialogporten.Application.Common.ReturnTypes.Forbidden>(_ => { });

    [Fact]
    public Task Get_Should_Set_ViaInstanceDelegation_From_AuthorizedPartiesInstances()
    {
        var dialogId = NewUuidV7();
        var party = Party;
        var serviceResource = "urn:altinn:resource:test-service-a";
        var otherServiceResource = "urn:altinn:resource:test-service-b";
        var instanceId = Guid.NewGuid();
        var instanceUrn = $"urn:altinn:app-instance-id:{instanceId}";
        var instanceRef = $"urn:altinn:instance-id:1337/{instanceId}";

        return FlowBuilder.For(Application, services =>
            {
                services.ConfigureAltinnAuthorization(altinnAuthorization =>
                {
                    altinnAuthorization.GetAuthorizedPartiesForLookup(
                            default!,
                            Arg.Any<List<string>>(),
                            Arg.Any<CancellationToken>())
                        .ReturnsForAnyArgs(new AuthorizedPartiesResult
                        {
                            AuthorizedParties =
                            [
                                new AuthorizedParty
                                {
                                    Party = party,
                                    PartyUuid = Guid.NewGuid(),
                                    Name = "Party",
                                    AuthorizedInstances =
                                    [
                                        new AuthorizedResource
                                        {
                                            ResourceId = otherServiceResource[Digdir.Domain.Dialogporten.Domain.Common.Constants.ServiceResourcePrefix.Length..],
                                            InstanceId = instanceId.ToString(),
                                            InstanceRef = instanceRef
                                        },
                                        new AuthorizedResource
                                        {
                                            ResourceId = serviceResource[Digdir.Domain.Dialogporten.Domain.Common.Constants.ServiceResourcePrefix.Length..],
                                            InstanceId = instanceId.ToString(),
                                            InstanceRef = instanceRef
                                        }
                                    ]
                                }
                            ]
                        });

                    altinnAuthorization.UserHasRequiredAuthLevel(
                            Arg.Any<string>(),
                            Arg.Any<CancellationToken>())
                        .Returns(true);
                });
            })
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.Id = dialogId;
                x.Dto.Party = party;
                x.Dto.ServiceResource = serviceResource;
                x.AddServiceOwnerLabels(instanceUrn);
            })
            .SendCommand(_ => new GetDialogLookupQuery
            {
                InstanceUrn = $"urn:altinn:dialog-id:{dialogId}"
            })
            .ExecuteAndAssert<EndUserIdentifierLookupDto>(result =>
            {
                result.AuthorizationEvidence.ViaInstanceDelegation.Should().BeTrue();
                result.AuthorizationEvidence.Evidence
                    .Should()
                    .ContainSingle(x =>
                        x.GrantType == IdentifierLookupGrantType.InstanceDelegation
                        && x.Subject == instanceUrn);
            });
    }

    [Fact]
    public Task Get_Should_Return_ValidationError_For_Unsupported_Urn() =>
        FlowBuilder.For(Application)
            .SendCommand(_ => new GetDialogLookupQuery
            {
                InstanceUrn = $"urn:altinn:unsupported:{Guid.NewGuid()}"
            })
            .ExecuteAndAssert<Digdir.Domain.Dialogporten.Application.Common.ReturnTypes.ValidationError>(result =>
                result.Errors.Should().ContainSingle());
}
