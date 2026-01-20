using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchTransmissions;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;
using Digdir.Domain.Dialogporten.Domain;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Transmissions.Queries.Search;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class SearchTransmissionsTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Search_Transmission_Should_Include_ExternalReference() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
                x.AddTransmission(x => x.ExternalReference = "ext"))
            .SendCommand((_, ctx) => new SearchTransmissionQuery
            {
                DialogId = ctx.GetDialogId()
            })
            .ExecuteAndAssert<List<TransmissionDto>>(x =>
                x.Should().ContainSingle().Which
                    .ExternalReference.Should().Be("ext"));

    [Fact]
    public Task Search_Transmission_Should_Mask_Expired_Attachment_Urls() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                x.AddTransmission(x => x.AddAttachment(x => x.ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)));
                x.AddTransmission(x => x.AddAttachment(x => x.ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)));
                x.AddTransmission(x => x.AddAttachment(x => x.ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)));
            })
            .OverrideUtc(TimeSpan.FromDays(2))
            .SendCommand((_, ctx) => new SearchTransmissionQuery
            {
                DialogId = ctx.GetDialogId(),
            })
            .ExecuteAndAssert<List<TransmissionDto>>(x =>
                x.Should().NotBeEmpty()
                    .And.AllSatisfy(x => x.Attachments.Should().NotBeEmpty()
                        .And.AllSatisfy(x => x.ExpiresAt.Should().NotBeNull())
                        .And.AllSatisfy(x => x.Urls.Should().NotBeEmpty()
                            .And.AllSatisfy(x => x.Url.Should().Be(Constants.ExpiredUri)))));

    [Fact]
    public Task Search_Transmission_Should_Mask_Unauthorized_ContentReference() =>
        FlowBuilder.For(Application, ConfigureReadOnlyAuthorization)
            .CreateSimpleDialog(x =>
                x.AddTransmission(transmission =>
                {
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
            .SendCommand((_, ctx) => new SearchTransmissionQuery
            {
                DialogId = ctx.GetDialogId(),
            })
            .ExecuteAndAssert<List<TransmissionDto>>(x =>
            {
                var transmission = x.Single();
                transmission.IsAuthorized.Should().BeFalse();
                transmission.Content.ContentReference.Should().NotBeNull();
                transmission.Content.ContentReference!.Value.Should().NotBeEmpty()
                    .And.AllSatisfy(localization =>
                        localization.Value.Should().Be(Constants.UnauthorizedUri.ToString()));
            });

    private static void ConfigureReadOnlyAuthorization(IServiceCollection services)
    {
        var authorizationResult = new DialogDetailsAuthorizationResult
        {
            AuthorizedAltinnActions = [new AltinnAction(Constants.ReadAction)]
        };
        services.ConfigureDialogDetailsAuthorizationResult(authorizationResult);
    }
}
