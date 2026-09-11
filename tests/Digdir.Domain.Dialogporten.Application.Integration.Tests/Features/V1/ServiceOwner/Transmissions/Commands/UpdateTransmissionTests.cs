using AwesomeAssertions;
using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes.Conflict;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.CreateTransmission;
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
using OneOf.Types;
using static Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.Common;
using Constants = Digdir.Domain.Dialogporten.Domain.Common.Constants;

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
                x.TransmissionId = NewUuidV7())
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
            .ExecuteAndAssert<Conflict>(x =>
            {
                x.ErrorMessage.Should().Contain(ExistingKey);
                x.AttemptedValues.Should().BeOfType<IdempotentKeyConflict>();
                var attempt = x.AttemptedValues.As<IdempotentKeyConflict>();
                attempt.ConflictingIdempotentKeys.Single().Should().Be(ExistingKey);
            });

    [Fact]
    public Task UpdateTransmission_Returns_DomainError_When_RelatedTransmissionId_Is_Cyclic() =>
        FlowBuilder.For(Application)
            .AsChangeTransmissionUser()
            .CreateSimpleDialog()
            .CreateTransmission((x, _) => x.Id = NewUuidV7())
            .CreateTransmission((x, ctx) =>
            {
                x.Id = NewUuidV7();
                x.RelatedTransmissionId = ctx.State.CreatedTransmissions.Single().Command.Transmissions.Single().Id;
            })
            .UpdateTransmission((x, ctx) =>
            {
                x.TransmissionId = ctx.State.CreatedTransmissions.First().Command.Transmissions.First().Id!.Value;
                x.Dto.RelatedTransmissionId = ctx.State.CreatedTransmissions.Last().Command.Transmissions.Single().Id;
            })
            .ExecuteAndAssert<DomainError>(x =>
                x.ShouldHaveErrorWithText("cyclic"));

    [Fact]
    public Task UpdateTransmission_Returns_Error_When_RelatedTransmissionId_Does_Not_Exist()
    {
        var dtoRelatedTransmissionId = NewUuidV7();
        return FlowBuilder.For(Application)
            .AsChangeTransmissionUser()
            .CreateSimpleDialog()
            .CreateTransmission()
            .UpdateTransmission((x, _) => x.Dto.RelatedTransmissionId = dtoRelatedTransmissionId)
            .ExecuteAndAssert<DomainError>((x, _) => x.ShouldHaveErrorWithText(dtoRelatedTransmissionId.ToString()));
    }

    private const string NewIdKey = "new-id";

    [Fact]
    public Task UpdateTransmission_Should_Not_Allow_Changing_Transmission_Id()
    {
        var newId = NewUuidV7();

        return FlowBuilder.For(Application)
            .AsChangeTransmissionUser()
            .CreateSimpleDialog()
            .CreateTransmission()
            .UpdateTransmission((t, _) =>
            {
                t.TransmissionId = newId;
            })
            .AssertResult<EntityNotFound<DialogTransmission>>()
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>((x, _) => x.Transmissions.Single().Id.Should().NotBe(newId));
    }

    [Fact]
    public Task UpdateTransmission_Should_Allow_Changing_Related_Transmission_Id() =>
        FlowBuilder.For(Application)
            .AsChangeTransmissionUser()
            .CreateSimpleDialog((x, _) => x.AddTransmission())
            .CreateTransmission((x, _) => x.RelatedTransmissionId = null)
            .UpdateTransmission((x, ctx) =>
                x.Dto.RelatedTransmissionId = ctx.State.CreatedDialogs.First().Command.Dto.Transmissions.Single().Id)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>((x, ctx) =>
                x.Transmissions.Should().ContainSingle(x => x.Id == ctx.GetTransmissionId())
                    .Which.RelatedTransmissionId.Should()
                    .Be(ctx.State.CreatedDialogs.First().Command.Dto.Transmissions.Single().Id));

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

    [Fact]
    public Task UpdateTransmission_Should_Be_Able_To_Update_Transmission_With_ExpiresAt_In_The_Past_On_Attachments()
        => FlowBuilder.For(Application)
            .OverrideUtc(DateTimeOffset.UtcNow.AddYears(-2))
            .CreateSimpleDialog()
            .CreateTransmission((x, _) => x.AddAttachment(y => y.ExpiresAt = DialogApplication.Clock.UtcNowOffset.AddDays(1)))
            .AssertResult<CreateTransmissionSuccess>()
            .OverrideUtc(DateTimeOffset.UtcNow)
            .AsChangeTransmissionUser()
            .UpdateTransmission((x, _) => x.Dto.IdempotentKey = "This is a key")
            .ExecuteAndAssert<UpdateTransmissionSuccess>();

    private const string InitialTransmissionCreatedAtKey = "initial-transmission-created-at";

    [Fact]
    public Task UpdateTransmission_Should_Preserve_CreatedAt_When_Not_Specified()
        => FlowBuilder.For(Application)
            .AsChangeTransmissionUser()
            .CreateSimpleDialog()
            .CreateTransmission()
            .GetServiceOwnerDialog()
            .AssertResult<DialogDto>((dialog, ctx) =>
                ctx.Bag[InitialTransmissionCreatedAtKey] = dialog.Transmissions.Single().CreatedAt)
            .UpdateTransmission((x, _) =>
            {
                x.Dto.ExternalReference = "preserve-created-at";
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>((dialog, ctx) =>
            {
                var expectedCreatedAt = (DateTimeOffset)ctx.Bag[InitialTransmissionCreatedAtKey]!;
                dialog.Transmissions.Should().ContainSingle()
                    .Which.CreatedAt.Should().Be(expectedCreatedAt);
            });

    private const string AttachmentUrlIdKey = "attachment-url-id";
    private const string UpdatedAttachmentUrlMediaType = "application/zip";

    [Fact]
    public Task UpdateTransmission_Should_Preserve_AttachmentUrl_Id_When_Provided()
        => FlowBuilder.For(Application)
            .AsChangeTransmissionUser()
            .CreateSimpleDialog()
            .CreateTransmission((x, _) =>
                x.AddAttachment())
            .GetServiceOwnerDialog()
            .AssertResult<DialogDto>((dialog, ctx) =>
                ctx.Bag[AttachmentUrlIdKey] = dialog.Transmissions.Single()
                    .Attachments.Single().Urls.Single().Id)
            .UpdateTransmission((x, ctx) =>
            {
                x.Dto.Attachments[0].Urls[0].Id = ctx.GetGuidByKey(AttachmentUrlIdKey);
                x.Dto.Attachments[0].Urls[0].MediaType = UpdatedAttachmentUrlMediaType;
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>((dialog, ctx) =>
            {
                var url = dialog.Transmissions.Single().Attachments.Single().Urls.Single();
                url.Id.Should().Be(ctx.GetGuidByKey(AttachmentUrlIdKey));
                url.MediaType.Should().Be(UpdatedAttachmentUrlMediaType);
            });

    private const string NavigationalActionIdKey = "navigational-action-id";
    private static readonly Uri UpdatedNavigationalActionUrl = new("https://digdir.no/updated-navigation");

    // Navigational action ids are emitted in the dialog token's authorized-entities claim, so a
    // service owner may reference them from its own endpoint. Echoing the id on PUT must therefore
    // update the existing row in place rather than replace it under a fresh id.
    [Fact]
    public Task UpdateTransmission_Should_Preserve_NavigationalAction_Id_When_Provided()
        => FlowBuilder.For(Application)
            .AsChangeTransmissionUser()
            .CreateSimpleDialog()
            .CreateTransmission((x, _) =>
                x.AddNavigationalAction())
            .GetServiceOwnerDialog()
            .AssertResult<DialogDto>((dialog, ctx) =>
                ctx.Bag[NavigationalActionIdKey] = dialog.Transmissions.Single()
                    .NavigationalActions.Single().Id)
            .UpdateTransmission((x, ctx) =>
            {
                x.Dto.NavigationalActions[0].Id = ctx.GetGuidByKey(NavigationalActionIdKey);
                x.Dto.NavigationalActions[0].Url = UpdatedNavigationalActionUrl;
            })
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>((dialog, ctx) =>
            {
                var navigationalAction = dialog.Transmissions.Single().NavigationalActions.Single();
                navigationalAction.Id.Should().Be(ctx.GetGuidByKey(NavigationalActionIdKey));
                navigationalAction.Url.Should().Be(UpdatedNavigationalActionUrl);
            });

    [Fact]
    public Task UpdateTransmission_Should_Replace_NavigationalAction_When_Id_Is_Omitted()
        => FlowBuilder.For(Application)
            .AsChangeTransmissionUser()
            .CreateSimpleDialog()
            .CreateTransmission((x, _) =>
                x.AddNavigationalAction())
            .GetServiceOwnerDialog()
            .AssertResult<DialogDto>((dialog, ctx) =>
                ctx.Bag[NavigationalActionIdKey] = dialog.Transmissions.Single()
                    .NavigationalActions.Single().Id)
            .UpdateTransmission((x, _) => x.Dto.NavigationalActions[0].Id = null)
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>((dialog, ctx) =>
                dialog.Transmissions.Single().NavigationalActions.Should().ContainSingle()
                    .Which.Id.Should().NotBe(ctx.GetGuidByKey(NavigationalActionIdKey)));

    [Theory]
    [ClassData(typeof(UpdateTransmissionBasicFieldTestData))]
    public Task UpdateTransmission_Persists_Changes_When_Silent_Update_And_Scope_Are_Present(
        UpdateTransmissionSuccessScenario successScenario) =>
        FlowBuilder.For(Application)
            .AsChangeTransmissionUser()
            .CreateSimpleDialog()
            .CreateTransmission((x, _) => x
                .AddNavigationalAction()
                .AddAttachment())
            .Do((_, ctx) => ctx.Application.PurgeEvents())
            .UpdateTransmission(successScenario.ModifyUpdateCommand)
            .AssertResult<UpdateTransmissionSuccess>()
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>((dialog, ctx) =>
            {
                ctx.Application.GetPublishedEvents().Should().ContainSingle()
                    .Which.Should().BeOfType<DialogUpdatedDomainEvent>()
                    .Which.Metadata[Constants.IsSilentUpdate]
                    .Should().Be(bool.TrueString);

                successScenario.Assert(dialog.Transmissions.Single());
            });

    private sealed class UpdateTransmissionBasicFieldTestData : TheoryData<UpdateTransmissionSuccessScenario>
    {
        public UpdateTransmissionBasicFieldTestData()
        {
            const string updatedIdempotentKey = "updated-idempotent-key";
            Add(new UpdateTransmissionSuccessScenario(
                Name: "Idempotent key",
                ModifyUpdateCommand: (command, _) => command.Dto.IdempotentKey = updatedIdempotentKey,
                Assert: transmission => transmission.IdempotentKey.Should().Be(updatedIdempotentKey)));

            const string updatedAuthorizationAttribute = "urn:altinn:subresource:updated-auth";
            Add(new UpdateTransmissionSuccessScenario(
                Name: "Authorization attribute",
                ModifyUpdateCommand: (command, _) => command.Dto.AuthorizationAttribute = updatedAuthorizationAttribute,
                Assert: transmission => transmission.AuthorizationAttribute.Should().Be(updatedAuthorizationAttribute)));

            var updatedExtendedType = new Uri("urn:dialogporten:test:updated-type");
            Add(new UpdateTransmissionSuccessScenario(
                Name: "Extended type",
                ModifyUpdateCommand: (command, _) => command.Dto.ExtendedType = updatedExtendedType,
                Assert: transmission => transmission.ExtendedType.Should().Be(updatedExtendedType)));

            const string updatedExternalReference = "updated-external-reference";
            Add(new UpdateTransmissionSuccessScenario(
                Name: "External reference",
                ModifyUpdateCommand: (command, _) => command.Dto.ExternalReference = updatedExternalReference,
                Assert: transmission => transmission.ExternalReference.Should().Be(updatedExternalReference)));

            const DialogTransmissionType.Values updatedType = DialogTransmissionType.Values.Acceptance;
            Add(new UpdateTransmissionSuccessScenario(
                Name: "Type",
                ModifyUpdateCommand: (command, _) => command.Dto.Type = updatedType,
                Assert: transmission => transmission.Type.Should().Be(updatedType)));

            const string updatedActorName = "vasher";
            Add(new UpdateTransmissionSuccessScenario(
                Name: "Sender actor name",
                ModifyUpdateCommand: (command, _) => command.Dto.Sender = new() { ActorType = ActorType.Values.PartyRepresentative, ActorName = updatedActorName },
                Assert: transmission => transmission.Sender.ActorName.Should().Be(updatedActorName)));

            const string updatedContentText = "updated-content-text";
            Add(new UpdateTransmissionSuccessScenario(
                Name: "Content title",
                ModifyUpdateCommand: (command, _) =>
                    command.Dto.Content?.Title.Value[0] = new() { Value = updatedContentText, LanguageCode = "nn" },
                Assert: transmission => transmission.Content.Title.Value.Should().ContainSingle()
                    .Which.Value.Should().Be(updatedContentText)));

            Add(new UpdateTransmissionSuccessScenario(
                Name: "Content summary",
                ModifyUpdateCommand: (command, _) =>
                    command.Dto.Content?.Summary = new() { Value = [new() { Value = updatedContentText, LanguageCode = "nn" }] },
                Assert: transmission => transmission.Content.Summary!.Value.Should().ContainSingle()
                    .Which.Value.Should().Be(updatedContentText)));

            const string updatedContentReference = "https://digdir.no/updated-content-reference";
            Add(new UpdateTransmissionSuccessScenario(
                Name: "Content reference",
                ModifyUpdateCommand: (command, _) =>
                    command.Dto.Content?.ContentReference = new()
                    {
                        MediaType = MediaTypes.EmbeddableMarkdown,
                        Value = [new() { Value = updatedContentReference, LanguageCode = "nn" }]
                    },
                Assert: transmission => transmission.Content.ContentReference!.Value.Should().ContainSingle()
                    .Which.Value.Should().Be(updatedContentReference)));

            var newCreatedAtDate = DateTimeOffset.UtcNow.AddDays(-10);
            Add(new UpdateTransmissionSuccessScenario(
                Name: "Created at",
                ModifyUpdateCommand: (command, _) => command.Dto.CreatedAt = newCreatedAtDate,
                Assert: transmission => transmission.CreatedAt.Should()
                    .BeCloseToWithinMicrosecond(newCreatedAtDate)));

            var updatedAttachmentId = NewUuidV7();
            Add(new UpdateTransmissionSuccessScenario(
                Name: "Attachment id",
                ModifyUpdateCommand: (command, _) => command.Dto.Attachments[0].Id = updatedAttachmentId,
                Assert: transmission => transmission.Attachments.Should().ContainSingle()
                    .Which.Id.Should().Be(updatedAttachmentId)));

            const string updatedAttachmentName = "updated-attachment-name";
            Add(new UpdateTransmissionSuccessScenario(
                Name: "Attachment name",
                ModifyUpdateCommand: (command, _) => command.Dto.Attachments[0].Name = updatedAttachmentName,
                Assert: transmission => transmission.Attachments.Should().ContainSingle()
                    .Which.Name.Should().Be(updatedAttachmentName)));

            const string updatedAttachmentDisplayName = "updated-attachment-name";
            Add(new UpdateTransmissionSuccessScenario(
                Name: "Attachment display name",
                ModifyUpdateCommand: (command, _) => command.Dto.Attachments[0].DisplayName =
                    [new() { LanguageCode = "nn", Value = updatedAttachmentDisplayName }],
                Assert: transmission => transmission.Attachments.Should().ContainSingle()
                    .Which.DisplayName.Should().ContainSingle()
                    .Which.Value.Should().Be(updatedAttachmentDisplayName)));

            var updatedAttachmentUrlId = NewUuidV7();
            Add(new UpdateTransmissionSuccessScenario(
                Name: "Attachment url id",
                ModifyUpdateCommand: (command, _) => command.Dto.Attachments[0].Urls[0].Id = updatedAttachmentUrlId,
                Assert: transmission => transmission.Attachments.Should().ContainSingle()
                    .Which.Urls.Should().ContainSingle()
                    .Which.Id.Should().Be(updatedAttachmentUrlId)));

            var updatedAttachmentUrl = new Uri("https://digdir.no/updated-attachment.pdf");
            Add(new UpdateTransmissionSuccessScenario(
                Name: "Attachment url",
                ModifyUpdateCommand: (command, _) => command.Dto.Attachments[0].Urls[0].Url = updatedAttachmentUrl,
                Assert: transmission => transmission.Attachments.Should().ContainSingle()
                    .Which.Urls.Should().ContainSingle()
                    .Which.Url.Should().Be(updatedAttachmentUrl)));

            const string updatedAttachmentMediaType = "application/zip";
            Add(new UpdateTransmissionSuccessScenario(
                Name: "Attachment media type",
                ModifyUpdateCommand: (command, _) => command.Dto.Attachments[0].Urls[0].MediaType = updatedAttachmentMediaType,
                Assert: transmission => transmission.Attachments.Should().ContainSingle()
                    .Which.Urls.Should().ContainSingle()
                    .Which.MediaType.Should().Be(updatedAttachmentMediaType)));

            const AttachmentUrlConsumerType.Values updatedAttachmentConsumerType = AttachmentUrlConsumerType.Values.Api;
            Add(new UpdateTransmissionSuccessScenario(
                Name: "Attachment consumer type",
                ModifyUpdateCommand: (command, _) => command.Dto.Attachments[0].Urls[0].ConsumerType = updatedAttachmentConsumerType,
                Assert: transmission => transmission.Attachments.Should().ContainSingle()
                    .Which.Urls.Should().ContainSingle()
                    .Which.ConsumerType.Should().Be(updatedAttachmentConsumerType)));

            var updatedAttachmentExpiresAt = DateTimeOffset.UtcNow.AddDays(7);
            Add(new UpdateTransmissionSuccessScenario(
                Name: "Attachment expires at",
                ModifyUpdateCommand: (command, _) => command.Dto.Attachments[0].ExpiresAt = updatedAttachmentExpiresAt,
                Assert: transmission => transmission.Attachments.Should().ContainSingle()
                    .Which.ExpiresAt.Should()
                    .BeCloseToWithinMicrosecond(updatedAttachmentExpiresAt)));

            const string updatedNavigationalActionTitle = "updated-navigation-title";
            Add(new UpdateTransmissionSuccessScenario(
                Name: "Navigational action title",
                ModifyUpdateCommand: (command, _) => command.Dto.NavigationalActions[0].Title =
                    [new() { LanguageCode = "nn", Value = updatedNavigationalActionTitle }],
                Assert: transmission => transmission.NavigationalActions.Should().ContainSingle()
                    .Which.Title.Should().ContainSingle()
                    .Which.Value.Should().Be(updatedNavigationalActionTitle)));

            var updatedNavigationalActionUrl = new Uri("https://digdir.no/updated-navigation");
            Add(new UpdateTransmissionSuccessScenario(
                Name: "Navigational action url",
                ModifyUpdateCommand: (command, _) => command.Dto.NavigationalActions[0].Url = updatedNavigationalActionUrl,
                Assert: transmission => transmission.NavigationalActions.Should().ContainSingle()
                    .Which.Url.Should().Be(updatedNavigationalActionUrl)));

            var updatedNavigationalActionExpiresAt = DateTimeOffset.UtcNow.AddDays(14);
            Add(new UpdateTransmissionSuccessScenario(
                Name: "Navigational action expires at",
                ModifyUpdateCommand: (command, _) => command.Dto.NavigationalActions[0].ExpiresAt = updatedNavigationalActionExpiresAt,
                Assert: transmission => transmission.NavigationalActions.Should().ContainSingle()
                    .Which.ExpiresAt.Should()
                    .BeCloseToWithinMicrosecond(updatedNavigationalActionExpiresAt)));

            Add(new UpdateTransmissionSuccessScenario(
                Name: "Clear attachments",
                ModifyUpdateCommand: (command, _) => command.Dto.Attachments = [],
                Assert: transmission => transmission.Attachments.Should().BeEmpty()));

            Add(new UpdateTransmissionSuccessScenario(
                Name: "Clear navigational actions",
                ModifyUpdateCommand: (command, _) => command.Dto.NavigationalActions = [],
                Assert: transmission => transmission.NavigationalActions.Should().BeEmpty()));
        }
    }

    private const string DeniedResource = "urn:altinn:resource:some-unauthorized-service";

    [Fact]
    public Task UpdateTransmission_Returns_Forbidden_When_Incoming_AuthorizationAttribute_Is_Not_Authorized() =>
        FlowBuilder.For(Application)
            .AsChangeTransmissionUser()
            .CreateSimpleDialog()
            .CreateTransmission()
            // Emulates a PDP that does not grant the service owner access to DeniedResource. The command
            // must therefore authorize service resources against the aggregate *after* the incoming DTO has
            // been mapped onto it — authorizing the pre-update state would let the denied reference through.
            .OverrideServiceResourceAuthorization(dialog =>
                dialog.Transmissions.Any(x => x.AuthorizationAttribute == DeniedResource)
                    ? new Forbidden($"Unauthorized service resource: {DeniedResource}")
                    : new Success())
            .UpdateTransmission((x, _) => x.Dto.AuthorizationAttribute = DeniedResource)
            .ExecuteAndAssert<Forbidden>(x =>
                x.Reasons.Should().ContainSingle(reason => reason.Contains(DeniedResource)));

    [Fact]
    public Task UpdateTransmission_Should_Preserve_An_AuthorizationAttribute_Equal_To_The_Exclusion_Sentinel() =>
        FlowBuilder.For(Application)
            .AsChangeTransmissionUser()
            .CreateSimpleDialog()
            // The sentinel value is a well-formed authorization attribute in its own right, so a service
            // owner may supply it on a transmission that has no authorization context. Suppressing it on
            // read would drop the restriction here: this flow round-trips the GET response straight back
            // through UpdateTransmission, which is how a suppressed attribute would be persisted as null.
            .CreateTransmission((x, _) => x.AuthorizationAttribute = Constants.ExcludedTransmissionAttribute)
            .GetServiceOwnerDialog()
            .AssertResult<DialogDto>((dialog, ctx) =>
                dialog.Transmissions.Should().ContainSingle(x => x.Id == ctx.GetTransmissionId())
                    .Which.AuthorizationAttribute.Should().Be(Constants.ExcludedTransmissionAttribute))
            .UpdateTransmission((_, _) => { })
            .AssertSuccess()
            .GetServiceOwnerDialog()
            .ExecuteAndAssert<DialogDto>((dialog, ctx) =>
                dialog.Transmissions.Should().ContainSingle(x => x.Id == ctx.GetTransmissionId())
                    .Which.AuthorizationAttribute.Should().Be(Constants.ExcludedTransmissionAttribute));
}


public record UpdateTransmissionSuccessScenario(
    string Name,
    Action<UpdateTransmissionCommand, FlowContext> ModifyUpdateCommand,
    Action<DialogTransmissionDto> Assert)
{
    public override string ToString() => Name;
}
