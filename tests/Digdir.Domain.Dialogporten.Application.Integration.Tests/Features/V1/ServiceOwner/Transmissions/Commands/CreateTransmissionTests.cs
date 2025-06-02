using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Domain;
using Digdir.Domain.Dialogporten.Domain.Common;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Transmissions.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class CreateTransmissionTests : ApplicationCollectionFixture
{
    public CreateTransmissionTests(DialogApplication application) : base(application) { }

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
}
