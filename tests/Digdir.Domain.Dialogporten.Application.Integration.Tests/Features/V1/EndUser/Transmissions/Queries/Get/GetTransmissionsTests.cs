using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.GetTransmission;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;
using Digdir.Domain.Dialogporten.Domain;
using Shouldly;
using Microsoft.Extensions.DependencyInjection;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Transmissions.Queries.Get;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class GetTransmissionsTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public async Task Get_Transmission_Should_Include_ExternalReference()
    {
        var transmissionId = NewUuidV7();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
                x.AddTransmission(x =>
                {
                    x.Id = transmissionId;
                    x.ExternalReference = "ext";
                }))
            .SendCommand((_, ctx) => new GetTransmissionQuery
            {
                DialogId = ctx.GetDialogId(),
                TransmissionId = transmissionId
            })
            .ExecuteAndAssert<TransmissionDto>(x =>
                x.ExternalReference.ShouldBe("ext"));
    }

    [Fact]
    public async Task Get_Transmission_Should_Mask_Expired_Attachment_Urls()
    {
        var transmissionId = NewUuidV7();

        await FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
                x.AddTransmission(x =>
                {
                    x.Id = transmissionId;
                    x.AddAttachment(x => x.ExpiresAt = DateTimeOffset.UtcNow.AddDays(1));
                }))
            .OverrideUtc(TimeSpan.FromDays(2))
            .SendCommand((_, ctx) => new GetTransmissionQuery
            {
                DialogId = ctx.GetDialogId(),
                TransmissionId = transmissionId
            })
            .ExecuteAndAssert<TransmissionDto>(x =>
            {
                var attachment = x.Attachments.Single(t => t.ExpiresAt is not null);
                var url = attachment.Urls.Single();
                url.Url.ShouldBe(Constants.ExpiredUri);
            });
    }

    [Fact]
    public async Task Get_Transmission_Should_Mask_Unauthorized_ContentReference()
    {
        var transmissionId = NewUuidV7();

        await FlowBuilder.For(Application, ConfigureReadOnlyAuthorization)
            .CreateSimpleDialog(x =>
                x.AddTransmission(transmission =>
                {
                    transmission.Id = transmissionId;
                    transmission.AuthorizationAttribute = "urn:altinn:resource:restricted";
                    transmission.Content!.ContentReference = new ContentValueDto
                    {
                        MediaType = MediaTypes.EmbeddableMarkdown,
                        Value = [new LocalizationDto
                        {
                            LanguageCode = "nb",
                            Value = "https://example.com/secret"
                        }]
                    };
                }))
            .SendCommand((_, ctx) => new GetTransmissionQuery
            {
                DialogId = ctx.GetDialogId(),
                TransmissionId = transmissionId
            })
            .ExecuteAndAssert<TransmissionDto>(x =>
            {
                x.IsAuthorized.ShouldBeFalse();
                x.Content.ContentReference.ShouldNotBeNull();
                x.Content.ContentReference!.Value.ShouldNotBeEmpty();
                x.Content.ContentReference!.Value
                    .All(localization => localization.Value == Constants.UnauthorizedUri.ToString())
                    .ShouldBeTrue();
            });
    }

    private static void ConfigureReadOnlyAuthorization(IServiceCollection services)
    {
        var authorizationResult = new DialogDetailsAuthorizationResult
        {
            AuthorizedAltinnActions = [new AltinnAction(Constants.ReadAction)]
        };
        services.ConfigureDialogDetailsAuthorizationResult(authorizationResult);
    }
}
