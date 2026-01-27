using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.SearchTransmissions;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;
using Digdir.Domain.Dialogporten.Domain;
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
            {
                var transmission = Assert.Single(x);
                Assert.Equal("ext", transmission.ExternalReference);
            });

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
            {
                Assert.NotEmpty(x);
                Assert.All(x, transmission =>
                {
                    Assert.NotEmpty(transmission.Attachments);
                    Assert.All(transmission.Attachments, attachment =>
                    {
                        Assert.NotNull(attachment.ExpiresAt);
                        Assert.NotEmpty(attachment.Urls);
                        Assert.All(attachment.Urls, url =>
                        {
                            var urlValue = url.Url;
                            Assert.NotNull(urlValue);
                            Assert.Equal(Constants.ExpiredUri, urlValue);
                        });
                    });
                });
            });

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
                Assert.False(transmission.IsAuthorized);
                Assert.NotNull(transmission.Content.ContentReference);
                var localizations = transmission.Content.ContentReference!.Value;
                Assert.NotEmpty(localizations);
                Assert.All(localizations, localization =>
                    Assert.Equal(Constants.UnauthorizedUri.ToString(), localization.Value));
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
