using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateTransmission;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common;
using Digdir.Domain.Dialogporten.Domain;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
using Constants = Digdir.Domain.Dialogporten.Domain.Common.Constants;
using UpdateTransmissionDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.TransmissionDto;
using UpdateTransmissionContentDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update.TransmissionContentDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Transmissions.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class CreateTransmissionTests : ApplicationCollectionFixture
{
    private const string ParentTransmissionIdKey = nameof(ParentTransmissionIdKey);

    public CreateTransmissionTests(DialogApplication application) : base(application) { }

    [Fact]
    public Task Can_Create_Transmission_Without_Summary() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
                x.AddTransmission(x =>
                    x.Content!.Summary = null))
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x => x.Transmissions
                .First().Content.Summary.Should().BeNull());

    [Fact]
    public Task Can_Create_Transmission_With_Embeddable_Content() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                var transmission = DialogGenerator.GenerateFakeDialogTransmissions(1)[0];
                transmission.Content!.ContentReference = new ContentValueDto
                {
                    MediaType = MediaTypes.EmbeddableMarkdown,
                    Value = [new LocalizationDto
                    {
                        LanguageCode = "nb",
                        Value = "https://example.com/transmission"
                    }]
                };

                x.Dto.Transmissions = [transmission];
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(result =>
            {
                var transmission = result.Transmissions.Single();
                transmission.Content.ContentReference!.MediaType.Should().Be(MediaTypes.EmbeddableMarkdown);
                transmission.Content.ContentReference!.Value.Should().HaveCount(1);
                transmission.Content.ContentReference!.Value.First().Value.Should().StartWith("https://");
            });

    [Fact]
    public Task Cannot_Create_Transmission_Embeddable_Content_With_Http_Url() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                var transmission = DialogGenerator.GenerateFakeDialogTransmissions(1)[0];
                transmission.Content!.ContentReference = new ContentValueDto
                {
                    MediaType = MediaTypes.EmbeddableMarkdown,
                    Value = [new LocalizationDto
                    {
                        LanguageCode = "nb",
                        Value = "http://example.com/transmission"
                    }]
                };

                x.Dto.Transmissions = [transmission];
            })
            .ExecuteAndAssert<ValidationError>(result => result.ShouldHaveErrorWithText("https"));

    [Fact]
    public Task Can_Create_Related_Transmission_With_Null_Id() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                var transmissions = DialogGenerator.GenerateFakeDialogTransmissions(2);

                // Set the first transmission to be related to the second one
                transmissions[0].RelatedTransmissionId = transmissions[1].Id;

                // This test assures that the Create-handler will use CreateVersion7IfDefault
                // on all transmissions before validating the hierarchy.
                transmissions[0].Id = null;

                x.Dto.Transmissions = transmissions;
            })
            .ExecuteAndAssert<CreateDialogSuccess>();

    [Fact]
    public Task Cannot_Create_Transmission_With_Empty_Content() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                var transmission = DialogGenerator.GenerateFakeDialogTransmissions(1)[0];
                transmission.Content = null!;
                x.Dto.Transmissions = [transmission];
            })
            .ExecuteAndAssert<ValidationError>(result =>
                result.ShouldHaveErrorWithText(nameof(DialogTransmission.Content)));

    [Fact]
    public Task Can_Create_Dialog_With_Empty_Transmission_Content_If_IsApiOnly() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                var transmission = DialogGenerator.GenerateFakeDialogTransmissions(1)[0];
                transmission.Content = null!;
                x.Dto.Transmissions = [transmission];
                x.Dto.IsApiOnly = true;
            })
            .ExecuteAndAssert<CreateDialogSuccess>();

    [Fact]
    public Task Can_Create_Transmission_With_ExternalReference() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                var transmission = DialogGenerator.GenerateFakeDialogTransmissions(1)[0];
                transmission.ExternalReference = "unique-key";
                x.Dto.Transmissions = [transmission];
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(result =>
                result.Transmissions
                    .Single()
                    .ExternalReference
                    .Should()
                    .Be("unique-key"));

    [Fact]
    public Task Can_Create_Transmission_Related_To_Existing_Transmission() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((command, ctx) =>
            {
                var parentTransmission = DialogGenerator.GenerateFakeDialogTransmissions(1)[0];
                parentTransmission.Id = parentTransmission.Id.CreateVersion7IfDefault();
                var parentId = parentTransmission.Id!.Value;
                ctx.Bag[ParentTransmissionIdKey] = parentId;
                command.Dto.Transmissions = [parentTransmission];
            })
            .AssertResult<CreateDialogSuccess>()
            .SendCommand((_, ctx) =>
            {
                var parentId = (Guid)ctx.Bag[ParentTransmissionIdKey]!;
                var transmission = new UpdateTransmissionDto
                {
                    Id = Guid.CreateVersion7(),
                    CreatedAt = DateTimeOffset.UtcNow,
                    Type = DialogTransmissionType.Values.Information,
                    RelatedTransmissionId = parentId,
                    Sender = new ActorDto
                    {
                        ActorType = Digdir.Domain.Dialogporten.Domain.Actors.ActorType.Values.ServiceOwner
                    },
                    Content = new UpdateTransmissionContentDto
                    {
                        Title = new ContentValueDto
                        {
                            Value = [new LocalizationDto
                            {
                                LanguageCode = "nb",
                                Value = "Ny melding"
                            }]
                        }
                    }
                };

                return new CreateTransmissionCommand
                {
                    DialogId = ctx.GetDialogId(),
                    Transmissions = [transmission]
                };
            })
            .AssertResult<CreateTransmissionSuccess>()
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(result =>
            {
                result.Transmissions.Should().HaveCount(2);
                result.Transmissions.Last().RelatedTransmissionId.Should()
                    .Be(result.Transmissions.First().Id);
            });

    [Fact]
    public Task Cannot_Create_Transmission_With_ExternalReference_Over_Max_Length() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog(x =>
            {
                var transmission = DialogGenerator.GenerateFakeDialogTransmissions(1)[0];
                transmission.ExternalReference = new string('a', Constants.DefaultMaxStringLength + 1);
                x.Dto.Transmissions = [transmission];
            })
            .ExecuteAndAssert<ValidationError>(result => result
                .ShouldHaveErrorWithText(nameof(DialogTransmission.ExternalReference)));

    private sealed class HtmlContentTestData : TheoryData<string, Action<IServiceCollection>, Action<TransmissionDto>, Type>
    {
        public HtmlContentTestData()
        {
            Add("Cannot create transmission with HTML content without valid html scope",
                _ => { }, // No change in user scopes
                x => x.Content!.ContentReference = CreateHtmlContentValueDto(MediaTypes.LegacyHtml),
                typeof(ValidationError));

            Add("Cannot create transmission with embeddable HTML content without valid html scope",
                _ => { }, // No change in user scopes
                x => x.Content!.ContentReference = CreateEmbeddableHtmlContentValueDto(MediaTypes.LegacyEmbeddableHtml),
                typeof(ValidationError));

            Add("Cannot create transmission title content with HTML media type with valid html scope",
                ConfigureUserWithScope(AuthorizationScope.LegacyHtmlScope),
                x => x.Content!.Title = CreateHtmlContentValueDto(MediaTypes.LegacyHtml),
                typeof(ValidationError));

            Add("Cannot create transmission summary content with HTML media type with valid html scope",
                ConfigureUserWithScope(AuthorizationScope.LegacyHtmlScope),
                x => x.Content!.Summary = CreateHtmlContentValueDto(MediaTypes.LegacyHtml),
                typeof(ValidationError));

            Add("Cannot create title content with embeddable HTML media type with valid html scope",
                ConfigureUserWithScope(AuthorizationScope.LegacyHtmlScope),
                x => x.Content!.Title = CreateHtmlContentValueDto(MediaTypes.LegacyEmbeddableHtml),
                typeof(ValidationError));

            Add("Cannot create transmission with embeddable HTML content without valid html scope",
                _ => { }, // No change in user scopes
                x => x.Content!.ContentReference = CreateEmbeddableHtmlContentValueDto(MediaTypes.LegacyEmbeddableHtmlDeprecated),
                typeof(ValidationError));

            Add("Cannot create content with HTML media type with valid html scope",
                ConfigureUserWithScope(AuthorizationScope.LegacyHtmlScope),
                x => x.Content!.ContentReference = CreateHtmlContentValueDto(MediaTypes.LegacyHtml),
                typeof(ValidationError));

            Add("Can create contentRef content with embeddable HTML media type with valid html scope",
                ConfigureUserWithScope(AuthorizationScope.LegacyHtmlScope),
                x => x.Content!.ContentReference = CreateEmbeddableHtmlContentValueDto(MediaTypes.LegacyEmbeddableHtml),
                typeof(CreateDialogSuccess));
        }
    }

    [Theory, ClassData(typeof(HtmlContentTestData))]
    public Task Html_Content_Tests(string _, Action<IServiceCollection> appConfig,
        Action<TransmissionDto> createTransmission, Type expectedType) =>
        FlowBuilder.For(Application, appConfig)
            .CreateSimpleDialog(x =>
                x.AddTransmission(createTransmission))
            .ExecuteAndAssert(x =>
            {
                x.Should().BeOfType(expectedType);

                if (expectedType != typeof(ValidationError))
                {
                    return;
                }

                var validationError = (ValidationError)x;
                validationError.ShouldHaveErrorWithText("Allowed media types");
            });

    [Fact]
    public Task Transmission_With_Legacy_Embeddable_HTML_Returns_New_Embeddable_MediaType() =>
        FlowBuilder.For(Application,
                ConfigureUserWithScope(AuthorizationScope.LegacyHtmlScope))
            .CreateSimpleDialog(x => x
                .AddTransmission(SetLegacyEmbeddableHtmlDeprecated))
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x =>
            {
                var transmission = x.Transmissions.Single();

                // Deprecated media type should be converted
                // to the new embeddable media type
                transmission.Content.ContentReference!
                    .MediaType.Should().Be(MediaTypes.LegacyEmbeddableHtml);
            });

    private static void SetLegacyEmbeddableHtmlDeprecated(TransmissionDto x) =>
        x.Content!.ContentReference = new()
        {
            MediaType = MediaTypes.LegacyEmbeddableHtmlDeprecated,
            Value = [new()
            {
                LanguageCode = "nb", Value = "https://external.html"
            }]
        };
}
