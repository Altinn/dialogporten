using Digdir.Domain.Dialogporten.Application.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using AwesomeAssertions;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
using GetDialogLookupQuery = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.DialogLookup.Queries.Get.GetDialogLookupQuery;
using ServiceOwnerIdentifierLookupDto = Digdir.Domain.Dialogporten.Application.Features.V1.Common.IdentifierLookup.ServiceOwnerIdentifierLookupDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.DialogLookup.Queries.Get;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class GetDialogLookupTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Get_Should_Return_Deleted_Dialog_For_ServiceOwner()
    {
        var instanceUrn = $"urn:altinn:app-instance-id:{Guid.NewGuid()}";

        return FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) => x.AddServiceOwnerLabels(instanceUrn))
            .DeleteDialog()
            .SendCommand(_ => new GetDialogLookupQuery
            {
                InstanceUrn = instanceUrn
            })
            .ExecuteAndAssert<ServiceOwnerIdentifierLookupDto>(result =>
                result.InstanceUrn.Should().Be(instanceUrn.ToLowerInvariant()));
    }

    [Fact]
    public Task Get_By_Label_Should_Pick_Newest_Dialog_Even_When_Deleted_For_ServiceOwner()
    {
        var instanceUrn = $"urn:altinn:app-instance-id:{Guid.NewGuid()}";
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
            .ExecuteAndAssert<ServiceOwnerIdentifierLookupDto>(result =>
                result.DialogId.Should().Be(newerDeletedDialogId));
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
                InstanceUrn = $"urn:altinn:dialog-id:{ctx.GetDialogId()}",
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
                InstanceUrn = $"urn:altinn:dialog-id:{ctx.GetDialogId()}"
            })
            .ExecuteAndAssert<ServiceOwnerIdentifierLookupDto>(result =>
                result.NonSensitiveTitle.Should().BeNull());

    [Fact]
    public Task Get_Should_Return_Forbidden_When_Org_Does_Not_Match() =>
        FlowBuilder.For(Application)
            .AsIntegrationTestUser(x => x.WithClaim(ClaimsPrincipalExtensions.AltinnOrgClaim, "other-org"))
            .CreateSimpleDialog()
            .SendCommand((_, ctx) => new GetDialogLookupQuery
            {
                InstanceUrn = $"urn:altinn:dialog-id:{ctx.GetDialogId()}"
            })
            .ExecuteAndAssert<Digdir.Domain.Dialogporten.Application.Common.ReturnTypes.Forbidden>(_ => { });

    [Fact]
    public Task Get_Should_Allow_AdminScope_When_Org_Does_Not_Match() =>
        FlowBuilder.For(Application)
            .AsAdminUser(x => x.WithClaim(ClaimsPrincipalExtensions.AltinnOrgClaim, "other-org"))
            .CreateSimpleDialog()
            .SendCommand((_, ctx) => new GetDialogLookupQuery
            {
                InstanceUrn = $"urn:altinn:dialog-id:{ctx.GetDialogId()}"
            })
            .ExecuteAndAssert<ServiceOwnerIdentifierLookupDto>(result =>
                result.DialogId.Should().NotBeEmpty());

    [Fact]
    public Task Get_Should_Return_ValidationError_For_Invalid_Urn() =>
        FlowBuilder.For(Application)
            .SendCommand(_ => new GetDialogLookupQuery
            {
                InstanceUrn = "urn:altinn:unsupported:abc"
            })
            .ExecuteAndAssert<Digdir.Domain.Dialogporten.Application.Common.ReturnTypes.ValidationError>(result =>
                result.Errors.Should().ContainSingle());
}
