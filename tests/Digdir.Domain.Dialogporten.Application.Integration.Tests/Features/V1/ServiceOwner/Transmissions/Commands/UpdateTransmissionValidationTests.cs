using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.UpdateTransmission;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Domain;
using Digdir.Domain.Dialogporten.Domain.Attachments;

using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
using TransmissionAttachmentDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.UpdateTransmission.TransmissionAttachmentDto;
using TransmissionAttachmentUrlDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.UpdateTransmission.TransmissionAttachmentUrlDto;
using TransmissionContentDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.UpdateTransmission.TransmissionContentDto;
using TransmissionNavigationalActionDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.UpdateTransmission.TransmissionNavigationalActionDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Transmissions.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class UpdateTransmissionValidationTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Theory]
    [ClassData(typeof(UpdateTransmissionValidationTestData))]
    public Task UpdateTransmission_Returns_ValidationError_When_Input_Is_Invalid(
        UpdateTransmissionValidationErrorScenario scenario) =>
        FlowBuilder.For(Application)
            .AsChangeTransmissionUser()
            .CreateSimpleDialog()
            .CreateTransmission((x, _) => x
                .AddNavigationalAction()
                .AddAttachment())
            .UpdateTransmission(scenario.ModifyUpdateCommand)
            .ExecuteAndAssert(scenario.Assert);

    private sealed class UpdateTransmissionValidationTestData : TheoryData<UpdateTransmissionValidationErrorScenario>
    {
        private const string DuplicateAttachmentIdKey = "duplicate-attachment-id";

        public UpdateTransmissionValidationTestData()
        {
            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Cannot create Url exceeding max length",
                ModifyUpdateCommand: (command, _) => command.Dto.Attachments.First().Urls.First().Url = new Uri($"https://www.altinn.no/{new string('a', Domain.Common.Constants.DefaultMaxUriLength)}"),
                Assert: (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionAttachmentUrlDto.Url))));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Cannot create unsecure link",
                ModifyUpdateCommand: (command, _) => command.Dto.Attachments.First().Urls.First().Url = new Uri("http://www.altinn.no"),
                Assert: (error, _) => error.ShouldHaveErrorWithText("HTTPS")));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Silent update false",
                ModifyUpdateCommand: (command, _) => command.IsSilentUpdate = false,
                Assert: (error, _) => error.ShouldHaveErrorWithText(nameof(UpdateTransmissionCommand.IsSilentUpdate))));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Empty dialog id",
                ModifyUpdateCommand: (command, _) => command.DialogId = Guid.Empty,
                Assert: (error, _) => error.ShouldHaveErrorWithText(nameof(UpdateTransmissionCommand.DialogId))));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Empty transmission id",
                ModifyUpdateCommand: (command, _) => command.TransmissionId = Guid.Empty,
                Assert: (error, _) => error.ShouldHaveErrorWithText(nameof(UpdateTransmissionCommand.TransmissionId))));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Missing dto",
                ModifyUpdateCommand: (command, _) => command.Dto = null!,
                Assert: (error, _) => error.ShouldHaveErrorWithText(nameof(UpdateTransmissionCommand.Dto))));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Idempotent key too short",
                ModifyUpdateCommand: (command, _) => command.Dto.IdempotentKey =
                    new string('a', Domain.Common.Constants.MinIdempotentKeyLength - 1),
                Assert: (error, _) => error.ShouldHaveErrorWithText(nameof(UpdateTransmissionDto.IdempotentKey))));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Idempotent key too long",
                ModifyUpdateCommand: (command, _) => command.Dto.IdempotentKey =
                    new string('a', Domain.Common.Constants.MaxIdempotentKeyLength + 1),
                Assert: (error, _) => error.ShouldHaveErrorWithText(nameof(UpdateTransmissionDto.IdempotentKey))));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "External reference too long",
                ModifyUpdateCommand: (command, _) => command.Dto.ExternalReference =
                    new string('a', Domain.Common.Constants.DefaultMaxStringLength + 1),
                Assert: (error, _) => error.ShouldHaveErrorWithText(nameof(UpdateTransmissionDto.ExternalReference))));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Missing sender",
                ModifyUpdateCommand: (command, _) => command.Dto.Sender = null!,
                Assert: (error, _) => error.ShouldHaveErrorWithText(nameof(UpdateTransmissionDto.Sender))));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Invalid authorization attribute",
                ModifyUpdateCommand: (command, _) => command.Dto.AuthorizationAttribute = "invalid authorization attribute",
                Assert: (error, _) => error.ShouldHaveErrorWithText(nameof(UpdateTransmissionDto.AuthorizationAttribute))));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Duplicate attachment id",
                ModifyUpdateCommand: (command, ctx) =>
                {
                    var existingAttachment = command.Dto.Attachments.Single();
                    ctx.Bag[DuplicateAttachmentIdKey] = existingAttachment.Id;
                    command.Dto.Attachments.Add(new TransmissionAttachmentDto
                    {
                        Id = existingAttachment.Id,
                        DisplayName = existingAttachment.DisplayName,
                        Urls = existingAttachment.Urls,
                        ExpiresAt = existingAttachment.ExpiresAt
                    });
                },
                Assert: (error, ctx) => error.ShouldHaveErrorWithText(ctx.GetGuidByKey(DuplicateAttachmentIdKey).ToString())));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Missing content",
                ModifyUpdateCommand: (command, _) => command.Dto.Content = null,
                Assert: (error, _) => error.ShouldHaveErrorWithText(nameof(UpdateTransmissionDto.Content))));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Attachment without urls",
                ModifyUpdateCommand: (command, _) => command.Dto.Attachments[0].Urls = [],
                Assert: (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionAttachmentDto.Urls))));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Attachment id not UUIDv7",
                ModifyUpdateCommand: (command, _) => command.Dto.Attachments[0].Id = Guid.NewGuid(),
                Assert: (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionAttachmentDto.Id))));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Attachment id from future",
                ModifyUpdateCommand: (command, _) => command.Dto.Attachments[0].Id = NewUuidV7(DateTimeOffset.UtcNow.AddHours(1)),
                Assert: (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionAttachmentDto.Id))));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Empty attachment display name",
                ModifyUpdateCommand: (command, _) => command.Dto.Attachments[0].DisplayName =
                    [new() { LanguageCode = "nb", Value = string.Empty }],
                Assert: (error, _) => error.ShouldHaveErrorWithText(nameof(LocalizationDto.Value))));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Missing attachment url",
                ModifyUpdateCommand: (command, _) => command.Dto.Attachments[0].Urls[0].Url = null!,
                Assert: (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionAttachmentUrlDto.Url))));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Attachment media type too long",
                ModifyUpdateCommand: (command, _) => command.Dto.Attachments[0].Urls[0].MediaType = new string('a', 257),
                Assert: (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionAttachmentUrlDto.MediaType))));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Invalid attachment consumer type",
                ModifyUpdateCommand: (command, _) => command.Dto.Attachments[0].Urls[0].ConsumerType = (AttachmentUrlConsumerType.Values)999,
                Assert: (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionAttachmentUrlDto.ConsumerType))));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Missing navigational action title",
                ModifyUpdateCommand: (command, _) => command.Dto.NavigationalActions[0].Title = [],
                Assert: (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionNavigationalActionDto.Title))));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Missing navigational action url",
                ModifyUpdateCommand: (command, _) => command.Dto.NavigationalActions[0].Url = null!,
                Assert: (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionNavigationalActionDto.Url))));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Navigational action url not https",
                ModifyUpdateCommand: (command, _) => command.Dto.NavigationalActions[0].Url = new Uri("http://digdir.no/action"),
                Assert: (error, _) => error.ShouldHaveErrorWithText("https")));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Navigational action url too long",
                ModifyUpdateCommand: (command, _) => command.Dto.NavigationalActions[0].Url =
                    new Uri($"https://digdir.no/{new string('a', Domain.Common.Constants.DefaultMaxUriLength)}"),
                Assert: (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionNavigationalActionDto.Url))));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Navigational action expired",
                ModifyUpdateCommand: (command, _) => command.Dto.NavigationalActions[0].ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
                Assert: (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionNavigationalActionDto.ExpiresAt))));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Missing content title",
                ModifyUpdateCommand: (command, _) => command.Dto.Content!.Title = null!,
                Assert: (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionContentDto.Title))));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Content title media type invalid",
                ModifyUpdateCommand: (command, _) => command.Dto.Content!.Title.MediaType = MediaTypes.EmbeddableMarkdown,
                Assert: (error, _) => error.ShouldHaveErrorWithText(nameof(ContentValueDto.MediaType))));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Empty content title value",
                ModifyUpdateCommand: (command, _) => command.Dto.Content!.Title.Value =
                    [new() { LanguageCode = "nb", Value = string.Empty }],
                Assert: (error, _) => error.ShouldHaveErrorWithText(nameof(ContentValueDto.Value))));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Summary media type invalid",
                ModifyUpdateCommand: (command, _) => command.Dto.Content!.Summary = new()
                {
                    MediaType = "application/json",
                    Value = [new() { LanguageCode = "nb", Value = "summary" }]
                },
                Assert: (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionContentDto.Summary))));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Summary value empty",
                ModifyUpdateCommand: (command, _) => command.Dto.Content!.Summary = new()
                {
                    MediaType = MediaTypes.PlainText,
                    Value = []
                },
                Assert: (error, _) => error.ShouldHaveErrorWithText(nameof(ContentValueDto.Value))));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Content reference media type invalid",
                ModifyUpdateCommand: (command, _) => command.Dto.Content!.ContentReference = new()
                {
                    MediaType = MediaTypes.PlainText,
                    Value = [new() { LanguageCode = "nb", Value = "https://digdir.no/content" }]
                },
                Assert: (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionContentDto.ContentReference))));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Content reference not url",
                ModifyUpdateCommand: (command, _) => command.Dto.Content!.ContentReference = new()
                {
                    MediaType = MediaTypes.EmbeddableMarkdown,
                    Value = [new() { LanguageCode = "nb", Value = "not-an-url" }]
                },
                Assert: (error, _) => error.ShouldHaveErrorWithText("https")));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Name cannot be too long",
                ModifyUpdateCommand: (command, _) => command.Dto.Attachments.First().Name = new string('a', Domain.Common.Constants.DefaultMaxUriLength + 10),
                Assert: (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionAttachmentDto.Name))));

            Add(new UpdateTransmissionValidationErrorScenario(
                Name: "Content reference not https",
                ModifyUpdateCommand: (command, _) => command.Dto.Content!.ContentReference = new()
                {
                    MediaType = MediaTypes.EmbeddableMarkdown,
                    Value = [new() { LanguageCode = "nb", Value = "http://digdir.no/not-https" }]
                },
                Assert: (error, _) => error.ShouldHaveErrorWithText("https")));
        }
    }
}

public record UpdateTransmissionValidationErrorScenario(
    string Name,
    Action<UpdateTransmissionCommand, FlowContext> ModifyUpdateCommand,
    Action<ValidationError, FlowContext> Assert)
{
    public override string ToString() => Name;
}
