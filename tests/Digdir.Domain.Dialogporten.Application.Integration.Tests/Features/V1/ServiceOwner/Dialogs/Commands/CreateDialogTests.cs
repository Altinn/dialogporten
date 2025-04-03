using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Externals.Presentation;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Domain;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Library.Entity.Abstractions.Features.Identifiable;
using Digdir.Tool.Dialogporten.GenerateFakeData;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TransmissionDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create.TransmissionDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Dialogs.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class CreateDialogTests : ApplicationCollectionFixture
{
    public CreateDialogTests(DialogApplication application) : base(application) { }

    [Fact]
    public async Task Cant_Create_Dialog_With_UUIDv4_format()
    {
        // Arrange
        var invalidDialogId = Guid.NewGuid();

        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: invalidDialogId);

        // Act
        var response = await Application.Send(createDialogCommand);

        // Assert
        response.TryPickT2(out var validationError, out _).Should().BeTrue();
        validationError.Should().NotBeNull();
    }

    [Fact]
    public async Task Cant_Create_Dialog_With_UUIDv7_In_Little_Endian_Format()
    {
        // Arrange
        // Guid created with Medo, Uuid7.NewUuid7().ToGuid(bigEndian: true)
        var invalidDialogId = Guid.Parse("b2ca9301-c371-ab74-a87b-4ee1416b9655");

        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: invalidDialogId);

        // Act
        var response = await Application.Send(createDialogCommand);

        // Assert
        response.TryPickT2(out var validationError, out _).Should().BeTrue();
        validationError.Should().NotBeNull();
    }

    [Fact]
    public async Task Cant_Create_Dialog_With_ID_With_Timestamp_In_The_Future()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.AddSeconds(1);
        var invalidDialogId = IdentifiableExtensions.CreateVersion7(timestamp);

        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: invalidDialogId);

        // Act
        var response = await Application.Send(createDialogCommand);

        // Assert
        response.TryPickT2(out var validationError, out _).Should().BeTrue();
        validationError.Should().NotBeNull();
    }

    [Fact]
    public async Task Create_Dialog_With_ID_With_Timestamp_In_The_Past()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow.AddSeconds(-1);
        var validDialogId = IdentifiableExtensions.CreateVersion7(timestamp);

        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: validDialogId);

        // Act
        var response = await Application.Send(createDialogCommand);

        // Assert
        response.TryPickT0(out var success, out _).Should().BeTrue();
        success.Should().NotBeNull();
        success.DialogId.Should().Be(validDialogId);
    }

    [Fact]
    public async Task Create_CreatesDialog_WhenDialogIsSimple()
    {
        // Arrange
        var expectedDialogId = IdentifiableExtensions.CreateVersion7();
        var createCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: expectedDialogId);

        // Act
        var response = await Application.Send(createCommand);

        // Assert
        response.TryPickT0(out var success, out _).Should().BeTrue();
        success.DialogId.Should().Be(expectedDialogId);
    }

    [Fact]
    public async Task Create_CreateDialog_WhenDialogIsComplex()
    {
        // Arrange
        var expectedDialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogCommand = DialogGenerator.GenerateFakeCreateDialogCommand(id: expectedDialogId);

        // Act
        var result = await Application.Send(createDialogCommand);

        // Assert
        result.TryPickT0(out var success, out _).Should().BeTrue();
        success.DialogId.Should().Be(expectedDialogId);
    }

    [Fact]
    public async Task Can_Create_Dialog_With_UpdatedAt_Supplied()
    {
        // Arrange
        var dialogId = IdentifiableExtensions.CreateVersion7();
        var createdAt = DateTimeOffset.UtcNow.AddYears(-20);
        var updatedAt = DateTimeOffset.UtcNow.AddYears(-15);
        var createDialogCommand = DialogGenerator.GenerateFakeCreateDialogCommand(id: dialogId, updatedAt: updatedAt, createdAt: createdAt);

        // Act
        var createDialogResult = await Application.Send(createDialogCommand);
        var getDialogQuery = new GetDialogQuery
        {
            DialogId = dialogId
        };

        var getDialogResponse = await Application.Send(getDialogQuery);

        // Assert
        createDialogResult.TryPickT0(out var dialogCreatedSuccess, out _).Should().BeTrue();
        dialogCreatedSuccess.DialogId.Should().Be(dialogId);

        getDialogQuery.Should().NotBeNull();
        getDialogResponse.TryPickT0(out var dialog, out _).Should().BeTrue();
        dialog.Should().NotBeNull();
        dialog.CreatedAt.Should().BeCloseTo(createdAt, precision: TimeSpan.FromMicroseconds(10));
        dialog.UpdatedAt.Should().BeCloseTo(updatedAt, precision: TimeSpan.FromMicroseconds(10));
    }

    [Fact]
    public async Task Cant_Create_Dialog_With_UpdatedAt_Supplied_Without_CreatedAt_Supplied()
    {
        // Arrange
        var updatedAt = DateTimeOffset.UtcNow.AddYears(-15);
        var createDialogCommand = DialogGenerator.GenerateFakeCreateDialogCommand(updatedAt: updatedAt);

        // Act
        var response = await Application.Send(createDialogCommand);

        // Assert
        response.TryPickT2(out var validationError, out _).Should().BeTrue();
        validationError.Should().NotBeNull();
        validationError.Errors.Should().Contain(e => e.ErrorMessage.Contains(nameof(createDialogCommand.Dto.UpdatedAt)));
    }

    [Fact]
    public async Task Cant_Create_Dialog_With_UpdatedAt_Date_Earlier_Than_CreatedAt_Date()
    {
        // Arrange
        var createdAt = DateTimeOffset.UtcNow.AddYears(-10);
        var updatedAt = DateTimeOffset.UtcNow.AddYears(-15);
        var createDialogCommand = DialogGenerator.GenerateFakeCreateDialogCommand(updatedAt: updatedAt, createdAt: createdAt);

        // Act
        var response = await Application.Send(createDialogCommand);

        // Assert
        response.TryPickT2(out var validationError, out _).Should().BeTrue();
        validationError.Should().NotBeNull();
        validationError.Errors.Should().Contain(e => e.ErrorMessage.Contains(nameof(createDialogCommand.Dto.CreatedAt)));
    }

    [Fact]
    public async Task Cant_Create_Dialog_With_UpdatedAt_Or_CreatedAt_In_The_Future()
    {
        // Arrange
        var aYearFromNow = DateTimeOffset.UtcNow.AddYears(1);
        var createDialogCommand = DialogGenerator
            .GenerateFakeCreateDialogCommand(updatedAt: aYearFromNow, createdAt: aYearFromNow);

        // Act
        var response = await Application.Send(createDialogCommand);

        //
        response.TryPickT2(out var validationError, out _).Should().BeTrue();
        validationError.Should().NotBeNull();
        validationError.Errors.Should().Contain(e => e.ErrorMessage.Contains("in the past"));
    }

    [Fact]
    public async Task Can_Create_Dialog_With_UpdatedAt_And_CreatedAt_Being_Equal()
    {
        // Arrange
        var aYearAgo = DateTimeOffset.UtcNow.AddYears(-1);
        var createDialogCommand = DialogGenerator
            .GenerateFakeCreateDialogCommand(updatedAt: aYearAgo, createdAt: aYearAgo);

        // Act
        var response = await Application.Send(createDialogCommand);

        // Assert
        response.TryPickT0(out var success, out _).Should().BeTrue();
        success.Should().NotBeNull();
    }

    [Fact]
    public async Task Cannot_Create_Transmission_Without_Content()
    {
        // Arrange
        var transmission = DialogGenerator.GenerateFakeDialogTransmissions(1)[0];
        transmission.Content = null!;

        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        createDialogCommand.Dto.Transmissions = [transmission];

        // Act
        var response = await Application.Send(createDialogCommand);

        // Assert
        response.TryPickT2(out var validationError, out _).Should().BeTrue();
        validationError.Should().NotBeNull();
        validationError.Errors.Should().HaveCount(1);
        // The error message might be localized, so we just check for the property name
        validationError.Errors.First().ErrorMessage.Should().Contain("'Content'");
    }

    [Fact]
    public async Task Cannot_Create_Transmission_Without_Content_Value()
    {
        // Arrange
        var transmission = DialogGenerator.GenerateFakeDialogTransmissions(1)[0];
        transmission.Content.Summary.Value = [];
        transmission.Content.Title.Value = [];

        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        createDialogCommand.Dto.Transmissions = [transmission];

        // Act
        var response = await Application.Send(createDialogCommand);

        // Assert
        response.TryPickT2(out var validationError, out _).Should().BeTrue();
        validationError.Should().NotBeNull();
        validationError.Errors
            .Count(e => e.PropertyName.Contains(nameof(createDialogCommand.Dto.Content)))
            .Should()
            .Be(2);
    }

    [Fact]
    public async Task Cannot_Create_Transmission_With_Empty_Content_Localization_Values()
    {
        // Arrange
        var transmission = DialogGenerator.GenerateFakeDialogTransmissions(1)[0];
        transmission.Content.Summary.Value = [new LocalizationDto { LanguageCode = "nb", Value = "" }];
        transmission.Content.Title.Value = [new LocalizationDto { LanguageCode = "nb", Value = "" }];

        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        createDialogCommand.Dto.Transmissions = [transmission];

        // Act
        var response = await Application.Send(createDialogCommand);

        // Assert
        response.TryPickT2(out var validationError, out _).Should().BeTrue();
        validationError.Should().NotBeNull();
        validationError.Errors
            .Count(e => e.PropertyName.Contains(nameof(createDialogCommand.Dto.Content)))
            .Should()
            .Be(2);
    }

    private const string LegacyHtmlMediaType = MediaTypes.LegacyHtml;

    private static ContentValueDto CreateHtmlContentValueDto() => new()
    {
        MediaType = LegacyHtmlMediaType,
        Value = [new() { LanguageCode = "nb", Value = "<p>Some HTML content</p>" }]
    };

    [Fact]
    public async Task Cannot_Create_AdditionalInfo_Content_With_Html_MediaType_Without_Correct_Scope()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        createDialogCommand.Dto.Content.AdditionalInfo = CreateHtmlContentValueDto();

        // Act
        var response = await Application.Send(createDialogCommand);

        // Assert
        response.TryPickT2(out var validationError, out _).Should().BeTrue();
        validationError.Should().NotBeNull();
        validationError.Errors
            .Count(e => e.AttemptedValue.Equals(LegacyHtmlMediaType))
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task Can_Create_AdditionalInfo_Content_With_Html_MediaType_With_Correct_Scope()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        createDialogCommand.Dto.Content.AdditionalInfo = CreateHtmlContentValueDto();

        var userWithLegacyScope = new IntegrationTestUser([new("scope", AuthorizationScope.LegacyHtmlScope)]);
        Application.ConfigureServices(services =>
        {
            services.RemoveAll<IUser>();
            services.AddSingleton<IUser>(userWithLegacyScope);
        });

        // Act
        var response = await Application.Send(createDialogCommand);

        // Assert
        response.TryPickT0(out var success, out _).Should().BeTrue();
        success.Should().NotBeNull();
    }

    [Fact]
    public async Task Cannot_Create_Title_Content_With_Html_MediaType_With_Correct_Scope()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        createDialogCommand.Dto.Content.Title = CreateHtmlContentValueDto();

        var userWithLegacyScope = new IntegrationTestUser([new("scope", AuthorizationScope.LegacyHtmlScope)]);
        Application.ConfigureServices(services =>
        {
            services.RemoveAll<IUser>();
            services.AddSingleton<IUser>(userWithLegacyScope);
        });

        // Act
        var response = await Application.Send(createDialogCommand);

        // Assert
        response.TryPickT2(out var validationError, out _).Should().BeTrue();
        validationError.Should().NotBeNull();
        validationError.Errors
            .Count(e => e.AttemptedValue.Equals(LegacyHtmlMediaType))
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task Cannot_Create_Title_Content_With_Embeddable_Html_MediaType_With_Correct_Scope()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();
        createDialogCommand.Dto.Content.Title = new ContentValueDto
        {
            MediaType = MediaTypes.LegacyEmbeddableHtml,
            Value = [new LocalizationDto { LanguageCode = "en", Value = "https://external.html" }]
        };

        var userWithLegacyScope = new IntegrationTestUser([new("scope", AuthorizationScope.LegacyHtmlScope)]);
        Application.ConfigureServices(services =>
        {
            services.RemoveAll<IUser>();
            services.AddSingleton<IUser>(userWithLegacyScope);
        });

        // Act
        var response = await Application.Send(createDialogCommand);

        // Assert
        response.TryPickT2(out var validationError, out _).Should().BeTrue();
        validationError.Should().NotBeNull();
        validationError.Errors
            .Count(e => e.AttemptedValue.Equals(MediaTypes.LegacyEmbeddableHtml))
            .Should()
            .Be(1);
    }

    [Fact]
    public async Task Can_Create_MainContentRef_Content_With_Embeddable_Html_MediaType_With_Correct_Scope()
    {
        // Arrange
        var expectedDialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: expectedDialogId);
        createDialogCommand.Dto.Content.MainContentReference = new ContentValueDto
        {
            MediaType = MediaTypes.LegacyEmbeddableHtml,
            Value = [new LocalizationDto { LanguageCode = "en", Value = "https://external.html" }]
        };

        var userWithLegacyScope = new IntegrationTestUser([new("scope", AuthorizationScope.LegacyHtmlScope)]);
        Application.ConfigureServices(services =>
        {
            services.RemoveAll<IUser>();
            services.AddSingleton<IUser>(userWithLegacyScope);
        });

        // Act
        var response = await Application.Send(createDialogCommand);

        // Assert
        response.TryPickT0(out var success, out _).Should().BeTrue();
        success.Should().NotBeNull();
        success.DialogId.Should().Be(expectedDialogId);
    }

    [Fact]
    public async Task CreateDialogCommand_Should_Return_Revision()
    {
        // Arrange
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand();

        // Act
        var response = await Application.Send(createDialogCommand);

        // Assert
        response.TryPickT0(out var success, out _).Should().BeTrue();
        success.Revision.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Can_Create_Actors_With_Same_Name_Without_ActorId()
    {
        // Arrange
        var transmissions = DialogGenerator.GenerateFakeDialogTransmissions(2);
        transmissions[0].Sender = new ActorDto
        {
            ActorType = ActorType.Values.PartyRepresentative,
            ActorName = "Fredrik",
            ActorId = null
        };

        transmissions[1].Sender = new ActorDto
        {
            ActorType = ActorType.Values.PartyRepresentative,
            ActorName = "Fredrik",
            ActorId = null
        };
        var createDialogCommand = DialogGenerator.GenerateFakeCreateDialogCommand(transmissions: transmissions);

        // Act
        var response = await Application.Send(createDialogCommand);

        // Assert
        response.TryPickT0(out var success, out _).Should().BeTrue();
        success.Should().NotBeNull();
    }

    [Fact]
    public async Task Can_Create_Dialog_With_Default_DateTimeOffset_For_UpdatedAt_Without_Setting_CreatedAt()
    {
        // Arrange
        var dialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: dialogId);
        createDialogCommand.Dto.UpdatedAt = default(DateTimeOffset);

        // Act
        var createDialogResponse = await Application.Send(createDialogCommand);
        var getDialogResponse = await Application.Send(new GetDialogQuery
        {
            DialogId = dialogId
        });

        // Assert
        createDialogResponse.TryPickT0(out var success, out _).Should().BeTrue();
        success.Should().NotBeNull();

        getDialogResponse.TryPickT0(out var dialog, out _).Should().BeTrue();
        dialog.Should().NotBeNull();
        dialog.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, precision: TimeSpan.FromSeconds(1));
        dialog.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, precision: TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Can_Create_Dialog_With_Default_DateTimeOffset_For_UpdatedAt_And_CreatedAt_Set()
    {
        // Arrange
        var dialogId = IdentifiableExtensions.CreateVersion7();
        var createDialogCommand = DialogGenerator.GenerateSimpleFakeCreateDialogCommand(id: dialogId);
        createDialogCommand.Dto.UpdatedAt = default(DateTimeOffset);
        createDialogCommand.Dto.CreatedAt = DateTimeOffset.UtcNow;

        // Act
        var createDialogResponse = await Application.Send(createDialogCommand);
        var getDialogResponse = await Application.Send(new GetDialogQuery
        {
            DialogId = dialogId
        });

        // Assert
        createDialogResponse.TryPickT0(out var success, out _).Should().BeTrue();
        success.Should().NotBeNull();

        getDialogResponse.TryPickT0(out var dialog, out _).Should().BeTrue();
        dialog.Should().NotBeNull();
        dialog.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, precision: TimeSpan.FromSeconds(1));
        dialog.UpdatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, precision: TimeSpan.FromSeconds(1));
    }
}
