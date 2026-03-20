using System.Security.Claims;
using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Domain;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
using Constants = Digdir.Domain.Dialogporten.Domain.Common.Constants;
using TransmissionDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.TransmissionDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Commands.Create;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class CreateTransmissionTests : ApplicationCollectionFixture
{
    public CreateTransmissionTests(DialogApplication application) : base(application) { }

    [Fact]
    public Task Cannot_Create_Transmission_Url_With_Media_Type_Exceeding_Max_Length() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(x =>
                    x.AddAttachment(x =>
                        x.Urls.First().MediaType = new string('a', TestConstants.DefaultMaxStringLength + 1))))
            .ExecuteAndAssert<ValidationError>();

    [Fact]
    public Task Can_Create_Transmission_Without_Summary() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(x =>
                    x.Content!.Summary = null))
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>(x => x.Transmissions
                .First().Content.Summary.Should().BeNull());

    [Fact]
    public Task Can_Create_Transmission_With_Embeddable_Content() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
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
            .CreateSimpleDialog((x, _) =>
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
    public Task Cannot_Create_Transmission_NavigationalAction_With_Long_Title() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(transmission =>
                    transmission.AddNavigationalAction(action =>
                        action.Title =
                        [
                            new LocalizationDto
                            {
                                LanguageCode = "nb",
                                Value = new string('a', 256)
                            }
                        ])))
            .ExecuteAndAssert<ValidationError>(result =>
                result.ShouldHaveErrorWithText("256 characters"));

    [Fact]
    public Task Cannot_Create_Transmission_NavigationalAction_With_Http_Url() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(transmission =>
                    transmission.AddNavigationalAction(action =>
                        action.Url = new Uri("http://example.com/action"))))
            .ExecuteAndAssert<ValidationError>(result =>
                result.ShouldHaveErrorWithText("https"));

    [Fact]
    public Task Cannot_Create_Transmission_NavigationalAction_With_ExpiresAt_In_Past() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(transmission =>
                    transmission.AddNavigationalAction(action =>
                        action.ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1))))
            .ExecuteAndAssert<ValidationError>(result =>
                result.ShouldHaveErrorWithText("future"));

    [Fact]
    public Task Can_Create_Related_Transmission_With_Null_Id() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
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
            .CreateSimpleDialog((x, _) =>
            {
                var transmission = DialogGenerator.GenerateFakeDialogTransmissions(1)[0];
                transmission.Content = null;
                x.Dto.Transmissions = [transmission];
            })
            .ExecuteAndAssert<ValidationError>(result =>
                result.ShouldHaveErrorWithText(nameof(DialogTransmission.Content)));

    [Fact]
    public Task Can_Create_Dialog_With_Empty_Transmission_Content_If_IsApiOnly() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                var transmission = DialogGenerator.GenerateFakeDialogTransmissions(1)[0];
                transmission.Content = null;
                x.Dto.Transmissions = [transmission];
                x.Dto.IsApiOnly = true;
            })
            .ExecuteAndAssert<CreateDialogSuccess>();

    [Fact]
    public Task Can_Create_Transmission_With_ExternalReference() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
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
    public Task Cannot_Create_Transmission_With_ExternalReference_Over_Max_Length() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog((x, _) =>
            {
                var transmission = DialogGenerator.GenerateFakeDialogTransmissions(1)[0];
                transmission.ExternalReference = new string('a', Constants.DefaultMaxStringLength + 1);
                x.Dto.Transmissions = [transmission];
            })
            .ExecuteAndAssert<ValidationError>(result => result
                .ShouldHaveErrorWithText(nameof(DialogTransmission.ExternalReference)));

    public sealed record HtmlContentScenario(
        string DisplayName,
        ClaimsPrincipal User,
        Action<TransmissionDto> ModifyTransmission,
        Type ExpectedResultType) : IClassDataBase
    {
        public override string ToString() => DisplayName;
    }

    private sealed class HtmlContentTestData : TheoryData<HtmlContentScenario>
    {

        public HtmlContentTestData()
        {
            var legacyHtmlScopeUser = TestUsers.FromDefault()
                .WithScope(AuthorizationScope.LegacyHtmlScope)
                .Build();
            var defaultUser = TestUsers.FromDefault().Build();

            Add(new HtmlContentScenario(
                DisplayName: "Cannot create transmission with HTML content without valid html scope",
                User: defaultUser, // No change in user scopes
                ModifyTransmission: x => x.Content!.ContentReference = CreateHtmlContentValueDto(MediaTypes.LegacyHtml),
                ExpectedResultType: typeof(ValidationError)));

            Add(new HtmlContentScenario(
                DisplayName: "Cannot create transmission with embeddable HTML content (LegacyEmbeddableHtml) without valid html scope",
                User: defaultUser, // No change in user scopes
                ModifyTransmission: x => x.Content!.ContentReference = CreateEmbeddableHtmlContentValueDto(MediaTypes.LegacyEmbeddableHtml),
                ExpectedResultType: typeof(ValidationError)));

            Add(new HtmlContentScenario(
                DisplayName: "Cannot create transmission title content with HTML media type with valid html scope",
                User: legacyHtmlScopeUser,
                ModifyTransmission: x => x.Content!.Title = CreateHtmlContentValueDto(MediaTypes.LegacyHtml),
                ExpectedResultType: typeof(ValidationError)));

            Add(new HtmlContentScenario(
                DisplayName: "Cannot create transmission summary content with HTML media type with valid html scope",
                User: legacyHtmlScopeUser,
                ModifyTransmission: x => x.Content!.Summary = CreateHtmlContentValueDto(MediaTypes.LegacyHtml),
                ExpectedResultType: typeof(ValidationError)));

            Add(new HtmlContentScenario(
                DisplayName: "Cannot create title content with embeddable HTML media type with valid html scope",
                User: legacyHtmlScopeUser,
                ModifyTransmission: x => x.Content!.Title = CreateHtmlContentValueDto(MediaTypes.LegacyEmbeddableHtml),
                ExpectedResultType: typeof(ValidationError)));

            Add(new HtmlContentScenario(
                DisplayName: "Cannot create transmission with embeddable HTML content (LegacyEmbeddableHtmlDeprecated) without valid html scope",
                User: defaultUser, // No change in user scopes
                ModifyTransmission: x => x.Content!.ContentReference = CreateEmbeddableHtmlContentValueDto(MediaTypes.LegacyEmbeddableHtmlDeprecated),
                ExpectedResultType: typeof(ValidationError)));

            Add(new HtmlContentScenario(
                DisplayName: "Cannot create content with HTML media type with valid html scope",
                User: legacyHtmlScopeUser,
                ModifyTransmission: x => x.Content!.ContentReference = CreateHtmlContentValueDto(MediaTypes.LegacyHtml),
                ExpectedResultType: typeof(ValidationError)));

            Add(new HtmlContentScenario(
                DisplayName: "Can create contentRef content with embeddable HTML media type with valid html scope",
                User: legacyHtmlScopeUser,
                ModifyTransmission: x => x.Content!.ContentReference = CreateEmbeddableHtmlContentValueDto(MediaTypes.LegacyEmbeddableHtml),
                ExpectedResultType: typeof(CreateDialogSuccess)));
        }
    }

    [Theory, ClassData(typeof(HtmlContentTestData))]
    public Task Html_Content_Tests(HtmlContentScenario scenario) =>
        FlowBuilder.For(Application)
            .AsUser(scenario.User)
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(scenario.ModifyTransmission))
            .ExecuteAndAssert(x =>
            {
                x.Should().BeOfType(scenario.ExpectedResultType);

                if (scenario.ExpectedResultType != typeof(ValidationError))
                {
                    return;
                }

                var validationError = (ValidationError)x;
                validationError.ShouldHaveErrorWithText("Allowed media types");
            });

    [Fact]
    public Task Transmission_With_Legacy_Embeddable_HTML_Returns_New_Embeddable_MediaType() =>
        FlowBuilder.For(Application)
            .AsIntegrationTestUser(x => x.WithScope(AuthorizationScope.LegacyHtmlScope))
            .CreateSimpleDialog((x, _) => x
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
