using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.GetTransmission;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;
using Digdir.Domain.Dialogporten.Domain;
using FluentAssertions;
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
                x.ExternalReference.Should().Be("ext"));
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
            .ExecuteAndAssert<TransmissionDto>(x => x
                .Attachments.Should().ContainSingle(t => t.ExpiresAt != null)
                .Which.Urls.Should().ContainSingle()
                .Which.Url.Should().Be(Constants.ExpiredUri));
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
                x.IsAuthorized.Should().BeFalse();
                x.Content.ContentReference.Should().NotBeNull();
                x.Content.ContentReference!.Value.Should().NotBeEmpty()
                    .And.AllSatisfy(localization =>
                        localization.Value.Should().Be(Constants.UnauthorizedUri.ToString()));
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
