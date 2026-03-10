using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Features.V1.ResourceRegistry.Commands.SyncPolicy;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using OneOf.Types;
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
            .CreateSimpleDialog((x, _) => x.AddServiceOwnerLabels($"urn:altinn:integration:storage:1337/{Guid.NewGuid()}"))
            .DeleteDialog()
            .SendCommand((_, ctx) => new GetDialogLookupQuery
            {
                InstanceRef = $"urn:altinn:dialog-id:{ctx.GetDialogId()}"
            })
            .ExecuteAndAssert<Digdir.Domain.Dialogporten.Application.Common.ReturnTypes.EntityNotFound>(_ => { });

    [Fact]
    public Task Get_By_Label_Should_Pick_Newest_NonDeleted_Dialog_For_EndUser()
    {
        var instanceId = Guid.NewGuid();
        var instanceRef = $"urn:altinn:instance-id:1337/{instanceId}";
        var storageLabel = $"urn:altinn:integration:storage:1337/{instanceId}";
        var olderDialogId = NewUuidV7();
        var newerDeletedDialogId = NewUuidV7();

        return FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.Id = olderDialogId;
                x.AddServiceOwnerLabels(storageLabel);
            })
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.Id = newerDeletedDialogId;
                x.AddServiceOwnerLabels(storageLabel);
            })
            .DeleteDialog()
            .SendCommand(_ => new GetDialogLookupQuery
            {
                InstanceRef = instanceRef
            })
            .ExecuteAndAssert<EndUserIdentifierLookupDto>(result =>
            {
                result.DialogId.Should().Be(olderDialogId);
                result.InstanceRef.Should().Be(instanceRef.ToLowerInvariant());
            });
    }

    [Fact]
    public Task Get_By_AppInstanceRef_Should_Return_Newest_Dialog_When_Multiple_Labels_Match()
    {
        var instanceId = Guid.NewGuid();
        var instanceRef = $"urn:altinn:instance-id:1337/{instanceId}";
        var storageLabel = $"urn:altinn:integration:storage:1337/{instanceId}";
        var firstDialogId = NewUuidV7();
        var secondDialogId = NewUuidV7();
        var expectedDialogId = firstDialogId.CompareTo(secondDialogId) > 0 ? firstDialogId : secondDialogId;

        return FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.Id = firstDialogId;
                x.AddServiceOwnerLabels(storageLabel);
            })
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.Id = secondDialogId;
                x.AddServiceOwnerLabels(storageLabel);
            })
            .SendCommand(_ => new GetDialogLookupQuery
            {
                InstanceRef = instanceRef
            })
            .ExecuteAndAssert<EndUserIdentifierLookupDto>(result =>
            {
                result.DialogId.Should().Be(expectedDialogId);
                result.InstanceRef.Should().Be(instanceRef.ToLowerInvariant());
            });
    }

    [Fact]
    public Task Get_By_CorrespondenceRef_Should_Return_Matching_Dialog()
    {
        var correspondenceId = Guid.NewGuid();
        var correspondenceRef = $"urn:altinn:correspondence-id:{correspondenceId}";

        return FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.AddServiceOwnerLabels(correspondenceRef))
            .SendCommand((_, ctx) => new GetDialogLookupQuery
            {
                InstanceRef = correspondenceRef
            })
            .ExecuteAndAssert<EndUserIdentifierLookupDto>((result, ctx) =>
            {
                result.DialogId.Should().Be(ctx.GetDialogId());
                result.InstanceRef.Should().Be(correspondenceRef.ToLowerInvariant());
            });
    }

    [Fact]
    public Task Get_By_DialogRef_Should_Prefer_AppInstanceRef_Then_CorrespondenceRef()
    {
        var instanceId = Guid.NewGuid();
        var appInstanceRef = $"urn:altinn:instance-id:1337/{instanceId}";
        var storageLabel = $"urn:altinn:integration:storage:1337/{instanceId}";
        var correspondenceRef = $"urn:altinn:correspondence-id:{Guid.NewGuid()}";

        return FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.AddServiceOwnerLabels(correspondenceRef, storageLabel))
            .SendCommand((_, ctx) => new GetDialogLookupQuery
            {
                InstanceRef = $"urn:altinn:dialog-id:{ctx.GetDialogId()}"
            })
            .ExecuteAndAssert<EndUserIdentifierLookupDto>(result =>
                result.InstanceRef.Should().Be(appInstanceRef.ToLowerInvariant()));
    }

    [Fact]
    public Task Get_Should_Return_Forbidden_When_EndUser_Has_No_Access() =>
        FlowBuilder.For(Application)
            .ConfigureAltinnAuthorization(altinnAuthorization =>
            {
                altinnAuthorization.GetAuthorizedPartiesForLookup(
                        null!,
                        Arg.Any<List<string>>(),
                        Arg.Any<CancellationToken>())
                    .ReturnsForAnyArgs(new AuthorizedPartiesResult { AuthorizedParties = [] });

                altinnAuthorization.UserHasRequiredAuthLevel(
                        Arg.Any<string>(),
                        Arg.Any<CancellationToken>())
                    .Returns(true);
            })
            .CreateSimpleDialog((x, _) => x.AddServiceOwnerLabels($"urn:altinn:integration:storage:1337/{Guid.NewGuid()}"))
            .SendCommand((_, ctx) => new GetDialogLookupQuery
            {
                InstanceRef = $"urn:altinn:dialog-id:{ctx.GetDialogId()}"
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
        var instanceRef = $"urn:altinn:instance-id:1337/{instanceId}";
        var storageLabel = $"urn:altinn:integration:storage:1337/{instanceId}";

        return FlowBuilder.For(Application)
            .ConfigureAltinnAuthorization(altinnAuthorization =>
            {
                altinnAuthorization.GetAuthorizedPartiesForLookup(
                        null!,
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
                                        ResourceId = otherServiceResource[Domain.Common.Constants.ServiceResourcePrefix.Length..],
                                        InstanceId = instanceId.ToString(),
                                        InstanceRef = instanceRef
                                    },
                                    new AuthorizedResource
                                    {
                                        ResourceId = serviceResource[Domain.Common.Constants.ServiceResourcePrefix.Length..],
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
            })
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.Id = dialogId;
                x.Dto.Party = party;
                x.Dto.ServiceResource = serviceResource;
                x.AddServiceOwnerLabels(storageLabel);
            })
            .SendCommand(_ => new GetDialogLookupQuery
            {
                InstanceRef = $"urn:altinn:dialog-id:{dialogId}"
            })
            .ExecuteAndAssert<EndUserIdentifierLookupDto>(result =>
            {
                result.AuthorizationEvidence.ViaInstanceDelegation.Should().BeTrue();
                result.AuthorizationEvidence.Evidence
                    .Should()
                    .ContainSingle(x =>
                        x.GrantType == IdentifierLookupGrantType.InstanceDelegation
                        && x.Subject == instanceRef);
            });
    }

    [Fact]
    public Task Get_Should_Return_ValidationError_For_Unsupported_InstanceRef() =>
        FlowBuilder.For(Application)
            .SendCommand(_ => new GetDialogLookupQuery
            {
                InstanceRef = $"urn:altinn:unsupported:{Guid.NewGuid()}"
            })
            .ExecuteAndAssert<Digdir.Domain.Dialogporten.Application.Common.ReturnTypes.ValidationError>(result =>
                result.Errors.Should().ContainSingle());
}
