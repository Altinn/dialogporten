using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Domain;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions.Contents;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Transmissions.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class CreateTransmissionTests : ApplicationCollectionFixture
{
    public CreateTransmissionTests(DialogApplication application) : base(application) { }

    [Fact]
    public async Task Can_Create_Simple_Transmission()
    {
        // Arrange
        var dialogId = IdentifiableExtensions.CreateVersion7();
        var createCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: dialogId);

        var transmission = DialogGenerator.GenerateFakeDialogTransmissions(1)[0];
        createCommand.Dto.Transmissions = [transmission];

        // Act
        var response = await Application.Send(createCommand);

        // Assert
        response.TryPickT0(out var success, out _).Should().BeTrue();
        success.DialogId.Should().Be(dialogId);
        var transmissionEntities = await Application.GetDbEntities<DialogTransmission>();
        transmissionEntities.Should().HaveCount(1);
        transmissionEntities.First().DialogId.Should().Be(dialogId);
        transmissionEntities.First().Id.Should().Be(transmission.Id!.Value);
    }

    [Fact]
    public async Task Can_Create_Transmission_With_Embeddable_Content()
    {
        // Arrange
        var dialogId = IdentifiableExtensions.CreateVersion7();
        var createCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: dialogId);

        var transmissionId = IdentifiableExtensions.CreateVersion7();
        var transmission = DialogGenerator.GenerateFakeDialogTransmissions(1)[0];

        const string contentUrl = "https://example.com/transmission";
        transmission.Id = transmissionId;
        transmission.Content!.ContentReference = new ContentValueDto
        {
            MediaType = MediaTypes.EmbeddableMarkdown,
            Value = [new LocalizationDto { LanguageCode = "nb", Value = contentUrl }]
        };

        createCommand.Dto.Transmissions = [transmission];

        // Act
        var response = await Application.Send(createCommand);

        // Assert
        response.TryPickT0(out var success, out _).Should().BeTrue();
        success.DialogId.Should().Be(dialogId);

        var transmissionEntities = await Application.GetDbEntities<DialogTransmission>();
        transmissionEntities.Should().HaveCount(1);

        var transmissionEntity = transmissionEntities.First();
        transmissionEntity.DialogId.Should().Be(dialogId);
        transmissionEntity.Id.Should().Be(transmissionId);

        var contentEntities = await Application.GetDbEntities<DialogTransmissionContent>();
        contentEntities.First(x => x.MediaType == MediaTypes.EmbeddableMarkdown).TransmissionId.Should().Be(transmissionId);
    }

    [Fact]
    public async Task Cannot_Create_Transmission_Embeddable_Content_With_Http_Url()
    {
        // Arrange
        var createCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();

        var transmission = DialogGenerator.GenerateFakeDialogTransmissions(1)[0];

        transmission.Content!.ContentReference = new ContentValueDto
        {
            MediaType = MediaTypes.EmbeddableMarkdown,
            Value = [new LocalizationDto { LanguageCode = "nb", Value = "http://example.com/transmission" }]
        };

        createCommand.Dto.Transmissions = [transmission];

        // Act
        var response = await Application.Send(createCommand);

        // Assert
        response.TryPickT2(out var validationError, out _).Should().BeTrue();
        validationError.Errors.Should().HaveCount(1);
        validationError.Errors.First().ErrorMessage.Should().Contain("HTTPS");

    }

    [Fact]
    public async Task Can_Create_Related_Transmission_With_Null_Id()
    {
        // Arrange
        var createCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        var transmissions = DialogGenerator.GenerateFakeDialogTransmissions(2);

        transmissions[0].RelatedTransmissionId = transmissions[1].Id;

        // This test assures that the Create-handler will use CreateVersion7IfDefault
        // on all transmissions before validating the hierarchy.
        transmissions[0].Id = null;

        createCommand.Dto.Transmissions = transmissions;

        // Act
        var response = await Application.Send(createCommand);

        // Assert
        response.TryPickT0(out var success, out _).Should().BeTrue();
        success.Should().NotBeNull();
    }

    [Fact]
    public async Task Cannot_Create_Transmission_With_Empty_Content()
    {
        // Arrange
        var createCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();

        var transmission = DialogGenerator.GenerateFakeDialogTransmissions(1)[0];

        // Omitting the property the payload to the WebAPI will set this to null
        transmission.Content = null!;

        createCommand.Dto.Transmissions = [transmission];

        // Act
        var response = await Application.Send(createCommand);

        // Assert
        response.TryPickT2(out var validationError, out _).Should().BeTrue();
        validationError.Errors.Should().HaveCount(1);
        validationError.Errors.Should()
            .ContainSingle(e => e.ErrorMessage
                .Contains(nameof(transmission.Content)));
    }
}
