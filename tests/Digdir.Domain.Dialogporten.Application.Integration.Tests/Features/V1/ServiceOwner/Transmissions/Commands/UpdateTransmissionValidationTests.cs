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
            .UpdateTransmission(scenario.First)
            .ExecuteAndAssert(scenario.Second);

    private sealed class UpdateTransmissionValidationTestData : TheoryData<UpdateTransmissionValidationErrorScenario>
    {
        private const string DuplicateAttachmentIdKey = "duplicate-attachment-id";

        public UpdateTransmissionValidationTestData()
        {
            Add(new UpdateTransmissionValidationErrorScenario(
                "Cannot create Url exceeding max length",
                (command, _) => command.Dto.Attachments.First().Urls.First().Url = new Uri($"https://www.altinn.no/{new string('a', Domain.Common.Constants.DefaultMaxUriLength)}"),
                (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionAttachmentUrlDto.Url))));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Cannot create unsecure link",
                (command, _) => command.Dto.Attachments.First().Urls.First().Url = new Uri("http://www.altinn.no"),
                (error, _) => error.ShouldHaveErrorWithText("HTTPS")));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Silent update false",
                (command, _) => command.IsSilentUpdate = false,
                (error, _) => error.ShouldHaveErrorWithText(nameof(UpdateTransmissionCommand.IsSilentUpdate))));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Empty dialog id",
                (command, _) => command.DialogId = Guid.Empty,
                (error, _) => error.ShouldHaveErrorWithText(nameof(UpdateTransmissionCommand.DialogId))));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Empty transmission id",
                (command, _) => command.TransmissionId = Guid.Empty,
                (error, _) => error.ShouldHaveErrorWithText(nameof(UpdateTransmissionCommand.TransmissionId))));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Missing dto",
                (command, _) => command.Dto = null!,
                (error, _) => error.ShouldHaveErrorWithText(nameof(UpdateTransmissionCommand.Dto))));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Idempotent key too short",
                (command, _) => command.Dto.IdempotentKey =
                    new string('a', Domain.Common.Constants.MinIdempotentKeyLength - 1),
                (error, _) => error.ShouldHaveErrorWithText(nameof(UpdateTransmissionDto.IdempotentKey))));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Idempotent key too long",
                (command, _) => command.Dto.IdempotentKey =
                    new string('a', Domain.Common.Constants.MaxIdempotentKeyLength + 1),
                (error, _) => error.ShouldHaveErrorWithText(nameof(UpdateTransmissionDto.IdempotentKey))));

            Add(new UpdateTransmissionValidationErrorScenario(
                "External reference too long",
                (command, _) => command.Dto.ExternalReference =
                    new string('a', Domain.Common.Constants.DefaultMaxStringLength + 1),
                (error, _) => error.ShouldHaveErrorWithText(nameof(UpdateTransmissionDto.ExternalReference))));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Missing sender",
                (command, _) => command.Dto.Sender = null!,
                (error, _) => error.ShouldHaveErrorWithText(nameof(UpdateTransmissionDto.Sender))));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Invalid authorization attribute",
                (command, _) => command.Dto.AuthorizationAttribute = "invalid authorization attribute",
                (error, _) => error.ShouldHaveErrorWithText(nameof(UpdateTransmissionDto.AuthorizationAttribute))));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Duplicate attachment id",
                (command, ctx) =>
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
                (error, ctx) => error.ShouldHaveErrorWithText(ctx.GetGuidByKey(DuplicateAttachmentIdKey).ToString())));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Missing content",
                (command, _) => command.Dto.Content = null,
                (error, _) => error.ShouldHaveErrorWithText(nameof(UpdateTransmissionDto.Content))));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Attachment without urls",
                (command, _) => command.Dto.Attachments[0].Urls = [],
                (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionAttachmentDto.Urls))));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Attachment id not UUIDv7",
                (command, _) => command.Dto.Attachments[0].Id = Guid.NewGuid(),
                (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionAttachmentDto.Id))));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Attachment id from future",
                (command, _) => command.Dto.Attachments[0].Id = NewUuidV7(DateTimeOffset.UtcNow.AddHours(1)),
                (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionAttachmentDto.Id))));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Empty attachment display name",
                (command, _) => command.Dto.Attachments[0].DisplayName =
                    [new() { LanguageCode = "nb", Value = string.Empty }],
                (error, _) => error.ShouldHaveErrorWithText(nameof(LocalizationDto.Value))));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Missing attachment url",
                (command, _) => command.Dto.Attachments[0].Urls[0].Url = null!,
                (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionAttachmentUrlDto.Url))));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Attachment media type too long",
                (command, _) => command.Dto.Attachments[0].Urls[0].MediaType = new string('a', 257),
                (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionAttachmentUrlDto.MediaType))));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Invalid attachment consumer type",
                (command, _) => command.Dto.Attachments[0].Urls[0].ConsumerType = (AttachmentUrlConsumerType.Values)999,
                (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionAttachmentUrlDto.ConsumerType))));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Missing navigational action title",
                (command, _) => command.Dto.NavigationalActions[0].Title = [],
                (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionNavigationalActionDto.Title))));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Missing navigational action url",
                (command, _) => command.Dto.NavigationalActions[0].Url = null!,
                (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionNavigationalActionDto.Url))));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Navigational action url not https",
                (command, _) => command.Dto.NavigationalActions[0].Url = new Uri("http://digdir.no/action"),
                (error, _) => error.ShouldHaveErrorWithText("https")));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Navigational action url too long",
                (command, _) => command.Dto.NavigationalActions[0].Url =
                    new Uri($"https://digdir.no/{new string('a', Domain.Common.Constants.DefaultMaxUriLength)}"),
                (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionNavigationalActionDto.Url))));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Navigational action expired",
                (command, _) => command.Dto.NavigationalActions[0].ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
                (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionNavigationalActionDto.ExpiresAt))));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Missing content title",
                (command, _) => command.Dto.Content!.Title = null!,
                (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionContentDto.Title))));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Content title media type invalid",
                (command, _) => command.Dto.Content!.Title.MediaType = MediaTypes.EmbeddableMarkdown,
                (error, _) => error.ShouldHaveErrorWithText(nameof(ContentValueDto.MediaType))));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Empty content title value",
                (command, _) => command.Dto.Content!.Title.Value =
                    [new() { LanguageCode = "nb", Value = string.Empty }],
                (error, _) => error.ShouldHaveErrorWithText(nameof(ContentValueDto.Value))));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Summary media type invalid",
                (command, _) => command.Dto.Content!.Summary = new()
                {
                    MediaType = "application/json",
                    Value = [new() { LanguageCode = "nb", Value = "summary" }]
                },
                (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionContentDto.Summary))));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Summary value empty",
                (command, _) => command.Dto.Content!.Summary = new()
                {
                    MediaType = MediaTypes.PlainText,
                    Value = []
                },
                (error, _) => error.ShouldHaveErrorWithText(nameof(ContentValueDto.Value))));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Content reference media type invalid",
                (command, _) => command.Dto.Content!.ContentReference = new()
                {
                    MediaType = MediaTypes.PlainText,
                    Value = [new() { LanguageCode = "nb", Value = "https://digdir.no/content" }]
                },
                (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionContentDto.ContentReference))));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Content reference not url",
                (command, _) => command.Dto.Content!.ContentReference = new()
                {
                    MediaType = MediaTypes.EmbeddableMarkdown,
                    Value = [new() { LanguageCode = "nb", Value = "not-an-url" }]
                },
                (error, _) => error.ShouldHaveErrorWithText("https")));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Name cannot be too long",
                (command, _) => command.Dto.Attachments.First().Name = new string('a', Domain.Common.Constants.DefaultMaxUriLength + 10),
                (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionAttachmentDto.Name))));

            Add(new UpdateTransmissionValidationErrorScenario(
                "Content reference not https",
                (command, _) => command.Dto.Content!.ContentReference = new()
                {
                    MediaType = MediaTypes.EmbeddableMarkdown,
                    Value = [new() { LanguageCode = "nb", Value = "http://digdir.no/not-https" }]
                },
                (error, _) => error.ShouldHaveErrorWithText("https")));
        }
    }
}

public record UpdateTransmissionValidationErrorScenario(string Name, Action<UpdateTransmissionCommand, FlowContext> First, Action<ValidationError, FlowContext> Second)
{
    public override string ToString() => Name;
}
