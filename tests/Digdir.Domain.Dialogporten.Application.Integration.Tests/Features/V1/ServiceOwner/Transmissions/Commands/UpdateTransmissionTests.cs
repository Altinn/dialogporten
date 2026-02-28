using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.UpdateTransmission;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Get;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.Common.Extensions;
using Digdir.Domain.Dialogporten.Domain;
using Digdir.Domain.Dialogporten.Domain.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Events;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
using TransmissionAttachmentDto =
    Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.UpdateTransmission.
    TransmissionAttachmentDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Features.V1.ServiceOwner.Transmissions.Commands;

[Collection(nameof(DialogCqrsCollectionFixture))]
public class UpdateTransmissionTests(DialogApplication application) : ApplicationCollectionFixture(application)
{
    [Fact]
    public Task UpdateTransmission_Returns_Forbidden_Without_ChangeTransmissions_Scope() =>
        FlowBuilder.For(Application)
            .CreateSimpleDialog()
            .CreateTransmission()
            .UpdateTransmission((x, _) =>
                x.IsSilentUpdate = true)
            .ExecuteAndAssert<Forbidden>(error =>
                error.Reasons.Should().ContainSingle(reason =>
                    reason.Contains(AuthorizationScope.ServiceProviderChangeTransmissions)));

    [Fact]
    public Task UpdateTransmission_Returns_NotFound_When_DialogId_Does_Not_Exist() =>
        FlowBuilder.For(Application)
            .AsChangeTransmissionUser()
            .CreateSimpleDialog()
            .CreateTransmission()
            .UpdateTransmission((x, _) => x.DialogId = NewUuidV7())
            .ExecuteAndAssert<EntityNotFound<DialogEntity>>();

    [Fact]
    public Task UpdateTransmission_Returns_NotFound_When_TransmissionId_Does_Not_Exist() =>
        FlowBuilder.For(Application)
            .AsChangeTransmissionUser()
            .CreateSimpleDialog()
            .CreateTransmission()
            .UpdateTransmission((x, _) =>
                x.TransmissionId = Guid.NewGuid())
            .ExecuteAndAssert<EntityNotFound<DialogTransmission>>();

    private const string ExistingKey = "existing-key";

    [Fact]
    public Task UpdateTransmission_Returns_Conflict_When_IdempotentKey_Is_Already_Used() =>
        FlowBuilder.For(Application)
            .AsChangeTransmissionUser()
            .CreateSimpleDialog((x, _) =>
                x.AddTransmission(x =>
                    x.IdempotentKey = ExistingKey))
            .CreateTransmission()
            .UpdateTransmission((x, _) =>
                x.Dto.IdempotentKey = ExistingKey)
            .ExecuteAndAssert<Conflict>();

    private const string FirstTransmissionIdKey = "first-transmission-id";
    private const string SecondTransmissionIdKey = "second-transmission-id";

    [Fact]
    public Task UpdateTransmission_Returns_DomainError_When_RelatedTransmissionId_Is_Cyclic() =>
        FlowBuilder.For(Application)
            .AsChangeTransmissionUser()
            .CreateSimpleDialog()
            .CreateTransmission((x, ctx) =>
            {
                x.Id = NewUuidV7();
                ctx.Bag[FirstTransmissionIdKey] = x.Id!.Value;
            })
            .CreateTransmission((x, ctx) =>
            {
                x.Id = NewUuidV7();
                ctx.Bag[SecondTransmissionIdKey] = x.Id!.Value;
                x.RelatedTransmissionId = ctx.GetGuidByKey(FirstTransmissionIdKey);
            })
            .UpdateTransmission((x, ctx) =>
            {
                x.TransmissionId = ctx.GetGuidByKey(FirstTransmissionIdKey);
                x.Dto.Id = ctx.GetGuidByKey(FirstTransmissionIdKey);
                x.Dto.RelatedTransmissionId = ctx.GetGuidByKey(SecondTransmissionIdKey);
            })
            .ExecuteAndAssert<DomainError>(x =>
                x.ShouldHaveErrorWithText("cyclic"));

    private const string InvalidRelatedKey = "invalid-related-id";

    [Fact]
    public Task UpdateTransmission_Returns_Error_When_RelatedTransmissionId_Does_Not_Exist() =>
        FlowBuilder.For(Application)
            .AsChangeTransmissionUser()
            .CreateSimpleDialog()
            .CreateTransmission()
            .UpdateTransmission((x, ctx) =>
            {
                x.Dto.RelatedTransmissionId = NewUuidV7();
                ctx.Bag[InvalidRelatedKey] = x.Dto.RelatedTransmissionId.Value;
            })
            .ExecuteAndAssert<DomainError>((x, ctx) =>
                x.ShouldHaveErrorWithText(
                    ctx.GetGuidByKey(InvalidRelatedKey).ToString()));

    private const string NewIdKey = "new-id";

    [Fact]
    public Task UpdateTransmission_Should_Not_Allow_Changing_Transmission_Id() =>
        FlowBuilder.For(Application)
            .AsChangeTransmissionUser()
            .CreateSimpleDialog()
            .CreateTransmission()
            .Do((_, ctx) => ctx.Application.PurgeEvents())
            .UpdateTransmission((x, ctx) =>
            {
                var newId = NewUuidV7();
                ctx.Bag[NewIdKey] = newId;
                x.Dto.Id = newId;
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>((x, ctx) =>
                x.Transmissions.Single().Id
                    .Should().NotBe(ctx.GetGuidByKey(NewIdKey)));

    private const string RelatedTransmissionIdKey = "related-transmission-id";

    [Fact]
    public Task UpdateTransmission_Should_Allow_Changing_Related_Transmission_Id() =>
        FlowBuilder.For(Application)
            .AsChangeTransmissionUser()
            .CreateSimpleDialog((x, ctx) =>
                x.AddTransmission(x =>
                    ctx.Bag[RelatedTransmissionIdKey] = x.Id))
            .CreateTransmission((x, _) => x.RelatedTransmissionId = null)
            .UpdateTransmission((x, ctx) =>
                x.Dto.RelatedTransmissionId =
                    ctx.GetGuidByKey(RelatedTransmissionIdKey))
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>((x, ctx) =>
                x.Transmissions.Should().ContainSingle(x => x.Id == ctx.GetTransmissionId())
                    .Which.RelatedTransmissionId.Should()
                    .Be(ctx.GetGuidByKey(RelatedTransmissionIdKey)));

    private const string InitialDialogRevisionKey = "initial-dialog-revision";

    [Fact]
    public Task UpdateTransmission_Should_Update_Dialog_Revision() =>
        FlowBuilder.For(Application)
            .AsChangeTransmissionUser()
            .CreateSimpleDialog()
            .CreateTransmission()
            .GetServiceOwnerDialog()
            .AssertResult<DialogDto>((dialog, ctx) =>
                ctx.Bag[InitialDialogRevisionKey] = dialog.Revision)
            .UpdateTransmission((x, _) =>
                x.Dto.ExternalReference = "updated-external-reference-for-revision-test")
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>((dialog, ctx) =>
                dialog.Revision.Should()
                    .NotBe(ctx.GetGuidByKey(InitialDialogRevisionKey)));

    [Theory]
    [ClassData(typeof(UpdateTransmissionBasicFieldTestData))]
    public Task UpdateTransmission_Persists_Changes_When_Silent_Update_And_Scope_Are_Present(
        Action<UpdateTransmissionCommand, FlowContext> modify,
        Action<DialogTransmissionDto> assert) =>
        FlowBuilder.For(Application)
            .AsChangeTransmissionUser()
            .CreateSimpleDialog()
            .CreateTransmission((x, _) => x
                .AddNavigationalAction()
                .AddAttachment())
            .Do((_, ctx) => ctx.Application.PurgeEvents())
            .UpdateTransmission(modify)
            .AssertResult<UpdateTransmissionSuccess>()
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>((dialog, ctx) =>
            {
                ctx.Application.GetPublishedEvents().Should().ContainSingle()
                    .Which.Should().BeOfType<DialogUpdatedDomainEvent>()
                    .Which.Metadata[Domain.Common.Constants.IsSilentUpdate]
                    .Should().Be(bool.TrueString);

                assert(dialog.Transmissions.Single());
            });

    private sealed class UpdateTransmissionBasicFieldTestData : TheoryData<
        Action<UpdateTransmissionCommand, FlowContext>,
        Action<DialogTransmissionDto>>
    {
        public UpdateTransmissionBasicFieldTestData()
        {
            const string updatedIdempotentKey = "updated-idempotent-key";
            Add(
                (command, _) => command.Dto.IdempotentKey = updatedIdempotentKey,
                transmission => transmission.IdempotentKey.Should().Be(updatedIdempotentKey));

            const string updatedAuthorizationAttribute = "urn:altinn:subresource:updated-auth";
            Add(
                (command, _) => command.Dto.AuthorizationAttribute = updatedAuthorizationAttribute,
                transmission => transmission.AuthorizationAttribute.Should().Be(updatedAuthorizationAttribute));

            var updatedExtendedType = new Uri("urn:dialogporten:test:updated-type");
            Add(
                (command, _) => command.Dto.ExtendedType = updatedExtendedType,
                transmission => transmission.ExtendedType.Should().Be(updatedExtendedType));

            const string updatedExternalReference = "updated-external-reference";
            Add(
                (command, _) => command.Dto.ExternalReference = updatedExternalReference,
                transmission => transmission.ExternalReference.Should().Be(updatedExternalReference));

            const DialogTransmissionType.Values updatedType = DialogTransmissionType.Values.Acceptance;
            Add(
                (command, _) => command.Dto.Type = updatedType,
                transmission => transmission.Type.Should().Be(updatedType));

            const string updatedActorName = "vasher";
            Add(
                (command, _) => command.Dto.Sender = new() { ActorType = ActorType.Values.PartyRepresentative, ActorName = updatedActorName },
                transmission => transmission.Sender.ActorName.Should().Be(updatedActorName));

            const string updatedContentText = "updated-content-text";
            Add((command, _) =>
                    command.Dto.Content?.Title.Value[0] = new() { Value = updatedContentText, LanguageCode = "nn" },
                transmission => transmission.Content.Title.Value.Should().ContainSingle()
                    .Which.Value.Should().Be(updatedContentText));

            Add((command, _) =>
                    command.Dto.Content?.Summary = new() { Value = [new() { Value = updatedContentText, LanguageCode = "nn" }] },
                transmission => transmission.Content.Summary!.Value.Should().ContainSingle()
                    .Which.Value.Should().Be(updatedContentText));

            const string updatedContentReference = "https://digdir.no/updated-content-reference";
            Add((command, _) =>
                    command.Dto.Content?.ContentReference = new()
                    {
                        MediaType = MediaTypes.EmbeddableMarkdown,
                        Value = [new() { Value = updatedContentReference, LanguageCode = "nn" }]
                    },
                transmission => transmission.Content.ContentReference!.Value.Should().ContainSingle()
                    .Which.Value.Should().Be(updatedContentReference));

            var newCreatedAtDate = DateTimeOffset.UtcNow.AddDays(-10);
            Add((command, _) => command.Dto.CreatedAt = newCreatedAtDate,
                transmission => transmission.CreatedAt.Should().BeCloseToDefault(newCreatedAtDate));

            var updatedAttachmentId = NewUuidV7();
            Add(
                (command, _) => command.Dto.Attachments[0].Id = updatedAttachmentId,
                transmission => transmission.Attachments.Should().ContainSingle()
                    .Which.Id.Should().Be(updatedAttachmentId));

            const string updatedAttachmentName = "updated-attachment-name";
            Add(
                (command, _) => command.Dto.Attachments[0].Name = updatedAttachmentName,
                transmission => transmission.Attachments.Should().ContainSingle()
                    .Which.Name.Should().Be(updatedAttachmentName));

            const string updatedAttachmentDisplayName = "updated-attachment-name";
            Add(
                (command, _) => command.Dto.Attachments[0].DisplayName =
                    [new() { LanguageCode = "nn", Value = updatedAttachmentDisplayName }],
                transmission => transmission.Attachments.Should().ContainSingle()
                    .Which.DisplayName.Should().ContainSingle()
                    .Which.Value.Should().Be(updatedAttachmentDisplayName));

            var updatedAttachmentUrl = new Uri("https://digdir.no/updated-attachment.pdf");
            Add(
                (command, _) => command.Dto.Attachments[0].Urls[0].Url = updatedAttachmentUrl,
                transmission => transmission.Attachments.Should().ContainSingle()
                    .Which.Urls.Should().ContainSingle()
                    .Which.Url.Should().Be(updatedAttachmentUrl));

            const string updatedAttachmentMediaType = "application/zip";
            Add(
                (command, _) => command.Dto.Attachments[0].Urls[0].MediaType = updatedAttachmentMediaType,
                transmission => transmission.Attachments.Should().ContainSingle()
                    .Which.Urls.Should().ContainSingle()
                    .Which.MediaType.Should().Be(updatedAttachmentMediaType));

            const AttachmentUrlConsumerType.Values updatedAttachmentConsumerType = AttachmentUrlConsumerType.Values.Api;
            Add(
                (command, _) => command.Dto.Attachments[0].Urls[0].ConsumerType = updatedAttachmentConsumerType,
                transmission => transmission.Attachments.Should().ContainSingle()
                    .Which.Urls.Should().ContainSingle()
                    .Which.ConsumerType.Should().Be(updatedAttachmentConsumerType));

            var updatedAttachmentExpiresAt = DateTimeOffset.UtcNow.AddDays(7);
            Add(
                (command, _) => command.Dto.Attachments[0].ExpiresAt = updatedAttachmentExpiresAt,
                transmission => transmission.Attachments.Should().ContainSingle()
                    .Which.ExpiresAt.Should().BeCloseToDefault(updatedAttachmentExpiresAt));

            const string updatedNavigationalActionTitle = "updated-navigation-title";
            Add(
                (command, _) => command.Dto.NavigationalActions[0].Title =
                    [new() { LanguageCode = "nn", Value = updatedNavigationalActionTitle }],
                transmission => transmission.NavigationalActions.Should().ContainSingle()
                    .Which.Title.Should().ContainSingle()
                    .Which.Value.Should().Be(updatedNavigationalActionTitle));

            var updatedNavigationalActionUrl = new Uri("https://digdir.no/updated-navigation");
            Add(
                (command, _) => command.Dto.NavigationalActions[0].Url = updatedNavigationalActionUrl,
                transmission => transmission.NavigationalActions.Should().ContainSingle()
                    .Which.Url.Should().Be(updatedNavigationalActionUrl));

            var updatedNavigationalActionExpiresAt = DateTimeOffset.UtcNow.AddDays(14);
            Add(
                (command, _) => command.Dto.NavigationalActions[0].ExpiresAt = updatedNavigationalActionExpiresAt,
                transmission => transmission.NavigationalActions.Should().ContainSingle()
                    .Which.ExpiresAt.Should().BeCloseToDefault(updatedNavigationalActionExpiresAt));

            Add(
                (command, _) => command.Dto.Attachments = [],
                transmission => transmission.Attachments.Should().BeEmpty());

            Add(
                (command, _) => command.Dto.NavigationalActions = [],
                transmission => transmission.NavigationalActions.Should().BeEmpty());
        }
    }

    [Theory]
    [ClassData(typeof(UpdateTransmissionValidationTestData))]
    public Task UpdateTransmission_Returns_ValidationError_When_Input_Is_Invalid(
        Action<UpdateTransmissionCommand, FlowContext> modify,
        Action<ValidationError, FlowContext> assert) =>
        FlowBuilder.For(Application)
            .AsChangeTransmissionUser()
            .CreateSimpleDialog()
            .CreateTransmission((x, _) => x
                .AddNavigationalAction()
                .AddAttachment())
            .UpdateTransmission(modify)
            .ExecuteAndAssert(assert);

    private sealed class UpdateTransmissionValidationTestData : TheoryData<
        Action<UpdateTransmissionCommand, FlowContext>,
        Action<ValidationError, FlowContext>>
    {
        private const string DuplicateAttachmentIdKey = "duplicate-attachment-id";

        public UpdateTransmissionValidationTestData()
        {
            Add(
                (command, _) => command.IsSilentUpdate = false,
                (error, _) => error.ShouldHaveErrorWithText(nameof(UpdateTransmissionCommand.IsSilentUpdate)));

            Add(
                (command, _) => command.DialogId = Guid.Empty,
                (error, _) => error.ShouldHaveErrorWithText(nameof(UpdateTransmissionCommand.DialogId)));

            Add(
                (command, _) => command.TransmissionId = Guid.Empty,
                (error, _) => error.ShouldHaveErrorWithText(nameof(UpdateTransmissionCommand.TransmissionId)));

            Add(
                (command, _) => command.Dto = null!,
                (error, _) => error.ShouldHaveErrorWithText(nameof(UpdateTransmissionCommand.Dto)));

            Add(
                (command, _) => command.Dto.IdempotentKey =
                    new string('a', Domain.Common.Constants.MinIdempotentKeyLength - 1),
                (error, _) => error.ShouldHaveErrorWithText(nameof(UpdateTransmissionDto.IdempotentKey)));

            Add(
                (command, _) => command.Dto.IdempotentKey =
                    new string('a', Domain.Common.Constants.MaxIdempotentKeyLength + 1),
                (error, _) => error.ShouldHaveErrorWithText(nameof(UpdateTransmissionDto.IdempotentKey)));

            Add(
                (command, _) => command.Dto.ExternalReference =
                    new string('a', Domain.Common.Constants.DefaultMaxStringLength + 1),
                (error, _) => error.ShouldHaveErrorWithText(nameof(UpdateTransmissionDto.ExternalReference)));

            Add(
                (command, _) => command.Dto.Sender = null!,
                (error, _) => error.ShouldHaveErrorWithText(nameof(UpdateTransmissionDto.Sender)));

            Add(
                (command, _) => command.Dto.AuthorizationAttribute = "invalid authorization attribute",
                (error, _) => error.ShouldHaveErrorWithText(nameof(UpdateTransmissionDto.AuthorizationAttribute)));

            Add(
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
                (error, ctx) => error.ShouldHaveErrorWithText(ctx.GetGuidByKey(DuplicateAttachmentIdKey).ToString()));

            Add(
                (command, _) => command.Dto.Content = null,
                (error, _) => error.ShouldHaveErrorWithText(nameof(UpdateTransmissionDto.Content)));

            Add(
                (command, _) => command.Dto.Attachments[0].Urls = [],
                (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionAttachmentDto.Urls)));

            Add(
                (command, _) => command.Dto.Attachments[0].Id = Guid.NewGuid(),
                (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionAttachmentDto.Id)));

            Add(
                (command, _) => command.Dto.Attachments[0].Id = NewUuidV7(DateTimeOffset.UtcNow.AddHours(1)),
                (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionAttachmentDto.Id)));

            Add(
                (command, _) => command.Dto.Attachments[0].DisplayName =
                    [new() { LanguageCode = "nb", Value = string.Empty }],
                (error, _) => error.ShouldHaveErrorWithText(nameof(LocalizationDto.Value)));

            Add(
                (command, _) => command.Dto.Attachments[0].Urls[0].Url = null!,
                (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionAttachmentUrlDto.Url)));

            Add(
                (command, _) => command.Dto.Attachments[0].Urls[0].MediaType = new string('a', 257),
                (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionAttachmentUrlDto.MediaType)));

            Add(
                (command, _) => command.Dto.Attachments[0].Urls[0].ConsumerType = (AttachmentUrlConsumerType.Values)999,
                (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionAttachmentUrlDto.ConsumerType)));

            Add(
                (command, _) => command.Dto.NavigationalActions[0].Title = [],
                (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionNavigationalActionDto.Title)));

            Add(
                (command, _) => command.Dto.NavigationalActions[0].Url = null!,
                (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionNavigationalActionDto.Url)));

            Add(
                (command, _) => command.Dto.NavigationalActions[0].Url = new Uri("http://digdir.no/action"),
                (error, _) => error.ShouldHaveErrorWithText("https"));

            Add(
                (command, _) => command.Dto.NavigationalActions[0].Url =
                    new Uri($"https://digdir.no/{new string('a', Domain.Common.Constants.DefaultMaxUriLength)}"),
                (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionNavigationalActionDto.Url)));

            Add(
                (command, _) => command.Dto.NavigationalActions[0].ExpiresAt = DateTimeOffset.UtcNow.AddDays(-1),
                (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionNavigationalActionDto.ExpiresAt)));

            Add(
                (command, _) => command.Dto.Content!.Title = null!,
                (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionContentDto.Title)));

            Add(
                (command, _) => command.Dto.Content!.Title.MediaType = MediaTypes.EmbeddableMarkdown,
                (error, _) => error.ShouldHaveErrorWithText(nameof(ContentValueDto.MediaType)));

            Add(
                (command, _) => command.Dto.Content!.Title.Value =
                    [new() { LanguageCode = "nb", Value = string.Empty }],
                (error, _) => error.ShouldHaveErrorWithText(nameof(ContentValueDto.Value)));

            Add(
                (command, _) => command.Dto.Content!.Summary = new()
                {
                    MediaType = "application/json",
                    Value = [new() { LanguageCode = "nb", Value = "summary" }]
                },
                (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionContentDto.Summary)));

            Add(
                (command, _) => command.Dto.Content!.Summary = new()
                {
                    MediaType = MediaTypes.PlainText,
                    Value = []
                },
                (error, _) => error.ShouldHaveErrorWithText(nameof(ContentValueDto.Value)));

            Add(
                (command, _) => command.Dto.Content!.ContentReference = new()
                {
                    MediaType = MediaTypes.PlainText,
                    Value = [new() { LanguageCode = "nb", Value = "https://digdir.no/content" }]
                },
                (error, _) => error.ShouldHaveErrorWithText(nameof(TransmissionContentDto.ContentReference)));

            Add(
                (command, _) => command.Dto.Content!.ContentReference = new()
                {
                    MediaType = MediaTypes.EmbeddableMarkdown,
                    Value = [new() { LanguageCode = "nb", Value = "not-an-url" }]
                },
                (error, _) => error.ShouldHaveErrorWithText("https"));

            Add(
                (command, _) => command.Dto.Content!.ContentReference = new()
                {
                    MediaType = MediaTypes.EmbeddableMarkdown,
                    Value = [new() { LanguageCode = "nb", Value = "http://digdir.no/not-https" }]
                },
                (error, _) => error.ShouldHaveErrorWithText("https"));
        }
    }
}
