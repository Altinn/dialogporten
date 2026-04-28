using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.ResourceRegistry;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using AwesomeAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using GetDialogLookupQuery = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.DialogLookup.Queries.Get.GetDialogLookupQuery;
using ServiceOwnerIdentifierLookupDto = Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup.ServiceOwnerIdentifierLookupDto;
using static Digdir.Domain.Dialogporten.Application.Common.ResourceRegistry.Constants;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.DialogLookup.Queries.Get;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class GetDialogLookupTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Get_Should_Return_Deleted_Dialog_For_ServiceOwner()
    {
        var instanceId = Guid.NewGuid();
        var instanceRef = $"urn:altinn:instance-id:1337/{instanceId}";
        var storageLabel = $"urn:altinn:integration:storage:1337/{instanceId}";

        return FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.AddServiceOwnerLabels(storageLabel))
            .DeleteDialog()
            .SendCommand(_ => new GetDialogLookupQuery
            {
                InstanceRef = instanceRef
            })
            .ExecuteAndAssert<ServiceOwnerIdentifierLookupDto>(result =>
                result.InstanceRef.Should().Be(instanceRef.ToLowerInvariant()));
    }

    [Fact]
    public Task Get_By_CorrespondenceRef_Should_Return_Matching_Dialog()
    {
        var correspondenceId = Guid.NewGuid();
        var correspondenceRef = $"urn:altinn:correspondence-id:{correspondenceId}";
        const string party = "urn:altinn:organization:identifier-no:991825827";

        return FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.Party = party;
                x.AddServiceOwnerLabels(correspondenceRef);
            })
            .SendCommand((_, ctx) => new GetDialogLookupQuery
            {
                InstanceRef = correspondenceRef
            })
            .ExecuteAndAssert<ServiceOwnerIdentifierLookupDto>((result, ctx) =>
            {
                result.DialogId.Should().Be(ctx.GetDialogId());
                result.InstanceRef.Should().Be(correspondenceRef.ToLowerInvariant());
                result.Party.Should().Be(party);
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
            .ExecuteAndAssert<ServiceOwnerIdentifierLookupDto>(result =>
                result.InstanceRef.Should().Be(appInstanceRef.ToLowerInvariant()));
    }

    [Fact]
    public Task Get_Should_Prune_Title_And_NonSensitiveTitle_When_AcceptLanguage_Is_Set() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                x.Dto.Content!.Title.Value =
                [
                    new LocalizationDto { LanguageCode = "nb", Value = "Tittel" },
                    new LocalizationDto { LanguageCode = "en", Value = "Title" }
                ];

                x.Dto.Content.NonSensitiveTitle = new ContentValueDto
                {
                    Value =
                    [
                        new LocalizationDto { LanguageCode = "nb", Value = "Ugradert tittel" },
                        new LocalizationDto { LanguageCode = "en", Value = "Non-sensitive title" }
                    ]
                };
            })
            .SendCommand((_, ctx) => new GetDialogLookupQuery
            {
                InstanceRef = $"urn:altinn:dialog-id:{ctx.GetDialogId()}",
                AcceptedLanguages = [new AcceptedLanguage("en", 100)]
            })
            .ExecuteAndAssert<ServiceOwnerIdentifierLookupDto>(result =>
            {
                result.Title.Should().ContainSingle();
                result.Title[0].LanguageCode.Should().Be("en");
                result.Title[0].Value.Should().Be("Title");

                result.NonSensitiveTitle.Should().ContainSingle();
                result.NonSensitiveTitle![0].LanguageCode.Should().Be("en");
                result.NonSensitiveTitle[0].Value.Should().Be("Non-sensitive title");
            });

    [Fact]
    public Task Get_Should_Return_Null_NonSensitiveTitle_When_Not_Set() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SendCommand((_, ctx) => new GetDialogLookupQuery
            {
                InstanceRef = $"urn:altinn:dialog-id:{ctx.GetDialogId()}"
            })
            .ExecuteAndAssert<ServiceOwnerIdentifierLookupDto>(result =>
                result.NonSensitiveTitle.Should().BeNull());

    [Fact]
    public Task Get_Should_Fall_Back_To_Default_MinimumAuthenticationLevel_On_ServiceResource() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SendCommand((_, ctx) => new GetDialogLookupQuery
            {
                InstanceRef = $"urn:altinn:dialog-id:{ctx.GetDialogId()}"
            })
            .ExecuteAndAssert<ServiceOwnerIdentifierLookupDto>(result =>
                result.ServiceResource.MinimumAuthenticationLevel.Should().Be(3));

    [Fact]
    public Task Get_Should_Return_Forbidden_When_Org_Does_Not_Match() =>
        FlowBuilder.For(Application)
            .AsIntegrationTestUser(x => x.WithClaim(ClaimsPrincipalExtensions.AltinnOrgClaim, "other-org"))
            .CreateSimpleDialog()
            .SendCommand((_, ctx) => new GetDialogLookupQuery
            {
                InstanceRef = $"urn:altinn:dialog-id:{ctx.GetDialogId()}"
            })
            .ExecuteAndAssert<Digdir.Domain.Dialogporten.Application.Common.ReturnTypes.Forbidden>(_ => { });

    [Fact]
    public Task Get_Should_Allow_AdminScope_When_Org_Does_Not_Match() =>
        FlowBuilder.For(Application)
            .AsAdminUser(x => x.WithClaim(ClaimsPrincipalExtensions.AltinnOrgClaim, "other-org"))
            .CreateSimpleDialog()
            .SendCommand((_, ctx) => new GetDialogLookupQuery
            {
                InstanceRef = $"urn:altinn:dialog-id:{ctx.GetDialogId()}"
            })
            .ExecuteAndAssert<ServiceOwnerIdentifierLookupDto>(result =>
                result.DialogId.Should().NotBeEmpty());

    [Fact]
    public Task Get_Should_Return_ValidationError_For_Invalid_InstanceRef() =>
        FlowBuilder.For(Application)
            .SendCommand(_ => new GetDialogLookupQuery
            {
                InstanceRef = "urn:altinn:unsupported:abc"
            })
            .ExecuteAndAssert<Digdir.Domain.Dialogporten.Application.Common.ReturnTypes.ValidationError>(result =>
                result.Errors.Should().ContainSingle());
}

internal sealed class CorrespondenceLookupResourceRegistry(DialogDbContext db) : LocalDevelopmentResourceRegistry(db)
{
    public override Task<ServiceResourceInformation?> GetResourceInformation(
        string serviceResourceId,
        CancellationToken cancellationToken) =>
        Task.FromResult<ServiceResourceInformation?>(
            new ServiceResourceInformation(serviceResourceId, CorrespondenceService, "991825827", "ttd", [], [], false));
}
