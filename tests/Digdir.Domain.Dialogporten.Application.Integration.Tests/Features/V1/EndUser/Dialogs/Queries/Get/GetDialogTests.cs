using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.EndUserContext.Commands.SetSystemLabel;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Domain;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Infrastructure.Altinn.ResourceRegistry;
using Digdir.Domain.Dialogporten.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using static Digdir.Domain.Dialogporten.Application.Common.ResourceRegistry.Constants;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
using Constants = Digdir.Domain.Dialogporten.Application.Common.Authorization.Constants;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.EndUser.Dialogs.Queries.Get;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class GetDialogTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task Get_Should_Return_Dialog_With_Correct_Id()
    {
        const string externalReference = "Bare for å være sikker...";
        var id = NewUuidV7();
        return FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .CreateSimpleDialog()
            .CreateSimpleDialog()
            .CreateSimpleDialog(x => (x.Dto.Id, x.Dto.ExternalReference) = (id, externalReference))
            .CreateSimpleDialog()
            .CreateSimpleDialog()
            .CreateSimpleDialog()
            .CreateSimpleDialog()
            .SendCommand(_ => new GetDialogQuery { DialogId = id })
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.Id.Should().Be(id);
                x.ExternalReference.Should().Be(externalReference);
            });
    }

    [Fact]
    public Task Get_Dialog_Should_Include_MainContentReference() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
                x.Dto.Content!.MainContentReference = new ContentValueDto
                {
                    MediaType = MediaTypes.EmbeddableMarkdown,
                    Value =
                    [
                        new LocalizationDto
                        {
                            LanguageCode = "nb",
                            Value = "https://localhost/nb"
                        },
                        new LocalizationDto
                        {
                            LanguageCode = "nn",
                            Value = "https://localhost/nn"
                        },
                        new LocalizationDto
                        {
                            LanguageCode = "en",
                            Value = "https://localhost/en"
                        }
                    ]
                })
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.Content.MainContentReference!.Should().BeEquivalentTo(
                    new AuthorizationContentValueDto
                    {
                        MediaType = MediaTypes.EmbeddableMarkdown,
                        IsAuthorized = true,
                        Value =
                        [
                            new LocalizationDto
                            {
                                LanguageCode = "nb",
                                Value = "https://localhost/nb"
                            },
                            new LocalizationDto
                            {
                                LanguageCode = "nn",
                                Value = "https://localhost/nn"
                            },
                            new LocalizationDto
                            {
                                LanguageCode = "en",
                                Value = "https://localhost/en"
                            }
                        ],
                    }
                );
            });

    [Fact]
    public Task Get_Dialog_Should_Mask_Unauthorized_MainContentReference() =>
        FlowBuilder.For(Application, ConfigureWriteOnlyAuthorization)
            .CreateSimpleDialog(x =>
                x.Dto.Content!.MainContentReference = new ContentValueDto
                {
                    MediaType = MediaTypes.EmbeddableMarkdown,
                    Value =
                    [
                        new LocalizationDto
                        {
                            LanguageCode = "nb",
                            Value = "https://localhost/nb"
                        },
                        new LocalizationDto
                        {
                            LanguageCode = "nn",
                            Value = "https://localhost/nn"
                        },
                        new LocalizationDto
                        {
                            LanguageCode = "en",
                            Value = "https://localhost/en"
                        }
                    ]
                })
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.Content.MainContentReference!.IsAuthorized.Should().BeFalse();
                x.Content.MainContentReference!.Value.Should().AllSatisfy(x =>
                    x.Value.Should().Be(Constants.UnauthorizedUri.ToString()));
            });

    [Fact]
    public Task Get_Dialog_Should_Include_Transmission_ExternalReference() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
                x.AddTransmission(x =>
                    x.ExternalReference = "ext"))
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.Transmissions.Should().ContainSingle()
                    .Which.ExternalReference.Should().Be("ext"));

    [Fact]
    public Task Get_Dialog_Should_Mask_Unauthorized_Transmission_ContentReference() =>
        FlowBuilder.For(Application, ConfigureReadOnlyAuthorization)
            .CreateSimpleDialog(x =>
                x.AddTransmission(transmission =>
                {
                    transmission.AuthorizationAttribute = "urn:altinn:resource:restricted";
                    transmission.Content!.ContentReference = new ContentValueDto
                    {
                        MediaType = MediaTypes.EmbeddableMarkdown,
                        Value =
                        [
                            new LocalizationDto
                            {
                                LanguageCode = "nb",
                                Value = "https://example.com/secret"
                            }
                        ]
                    };
                }))
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                var transmission = x.Transmissions.Single();
                transmission.IsAuthorized.Should().BeFalse();
                transmission.Content.ContentReference.Should().NotBeNull();
                transmission.Content.ContentReference!.Value.Should().NotBeEmpty()
                    .And.AllSatisfy(localization =>
                        localization.Value.Should().Be(Constants.UnauthorizedUri.ToString()));
            });

    [Fact]
    public Task Get_Should_Populate_EnduserContextRevision() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.EndUserContext.Revision.Should().NotBeEmpty());

    [Fact]
    public Task Get_Dialog_Should_Mask_Expired_Attachment_Urls() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                x.AddAttachment(x => x.ExpiresAt = DateTimeOffset.Now.AddDays(1));
                x.AddAttachment(x => x.ExpiresAt = DateTimeOffset.Now.AddDays(1));

                x.AddTransmission(x => x.AddAttachment(x => x.ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)));
                x.AddTransmission(x => x.AddAttachment(x => x.ExpiresAt = DateTimeOffset.UtcNow.AddDays(1)));
            })
            .OverrideUtc(TimeSpan.FromDays(2))
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.Transmissions.Should().NotBeEmpty()
                    .And.AllSatisfy(x => x.Attachments.Should().NotBeEmpty()
                        .And.AllSatisfy(x => x.Urls.Should().NotBeEmpty()
                            .And.AllSatisfy(url => url.Url.Should().NotBeNull()
                                .And.Be(Constants.ExpiredUri))));

                x.Attachments.Should().NotBeEmpty()
                    .And.AllSatisfy(a => a.Urls.Should().NotBeEmpty()
                        .And.AllSatisfy(url => url.Url.Should().NotBeNull()
                            .And.Be(Constants.ExpiredUri)));
            });

    private const string DialogAttachmentName = "dialog-attachment";
    private const string TransmissionAttachmentName = "transmission-attachment";

    [Fact]
    public Task Get_Dialog_Should_Return_Attachment_Names() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                x.AddAttachment(attachment =>
                    attachment.Name = DialogAttachmentName);
                x.AddTransmission(transmission =>
                    transmission.AddAttachment(attachment =>
                        attachment.Name = TransmissionAttachmentName));
            })
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                x.Attachments.Should()
                    .ContainSingle(attachment =>
                        attachment.Name == DialogAttachmentName);
                x.Transmissions.Should().ContainSingle()
                    .Which.Attachments.Should()
                    .ContainSingle(attachment =>
                        attachment.Name == TransmissionAttachmentName);
            });

    [Fact]
    public Task Get_Should_Remove_MarkedAsUnopened_SystemLabel() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .SendCommand((_, ctx) => new SetSystemLabelCommand
            {
                DialogId = ctx.GetDialogId(),
                AddLabels = [SystemLabel.Values.MarkedAsUnopened]
            })
            .SendCommand((_, ctx) => GetDialog(ctx.GetDialogId()))
            .ExecuteAndAssert<DialogDto>(x =>
                x.EndUserContext.SystemLabels.Should().NotContain(SystemLabel.Values.MarkedAsUnopened));

    [Fact]
    [Obsolete("Testing obsolete SystemLabel, will be removed in future versions.")]
    public Task Get_Should_Populate_Obsolete_SystemLabel() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.SystemLabel.Should()
                    .Be(SystemLabel.Values.Default));

    private static GetDialogQuery GetDialog(Guid? id) => new() { DialogId = id!.Value };

    private static void ConfigureReadOnlyAuthorization(IServiceCollection services)
    {
        var authorizationResult = new DialogDetailsAuthorizationResult
        {
            AuthorizedAltinnActions = [new AltinnAction(Constants.ReadAction)]
        };
        services.ConfigureDialogDetailsAuthorizationResult(authorizationResult);
    }

    private static void ConfigureWriteOnlyAuthorization(IServiceCollection services)
    {
        var authorizationResult = new DialogDetailsAuthorizationResult
        {
            AuthorizedAltinnActions = [new AltinnAction("write")]
        };
        services.ConfigureDialogDetailsAuthorizationResult(authorizationResult);
    }

    [Theory]
    [InlineData(DialogActivityType.Values.CorrespondenceOpened, false)]
    [InlineData(DialogActivityType.Values.Information, true)]
    public Task Get_Correspondence_Sets_HasUnopenedContent_Correctly_Based_On_Activities(
        DialogActivityType.Values activityType, bool expectedHasUnOpenedContent) =>
        FlowBuilder.For(Application, x =>
            {
                x.RemoveAll<IResourceRegistry>();
                x.AddScoped<IResourceRegistry, TestResourceRegistry>();
            })
            .AsIntegrationTestUser(x => x.WithScope(AuthorizationScope.CorrespondenceScope))
            .CreateSimpleDialog(x => x.AddActivity(activityType))
            .GetEndUserDialog()
            .ExecuteAndAssert<DialogDto>(x =>
                x.HasUnopenedContent.Should().Be(expectedHasUnOpenedContent));
}

internal sealed class TestResourceRegistry(DialogDbContext db) : LocalDevelopmentResourceRegistry(db)
{
    public override Task<ServiceResourceInformation?> GetResourceInformation(string serviceResourceId,
        CancellationToken cancellationToken) =>
        Task.FromResult<ServiceResourceInformation?>(
            new ServiceResourceInformation(serviceResourceId, CorrespondenceService, "SomeOrg", "org"));
}
