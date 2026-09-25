#pragma warning disable CS0618 // Obsolete legacy authorization fields are mapped for backwards compatibility
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.DialogStatuses;
using Eu = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Get;
using Up = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;
using EuActorDto = Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Common.Actors.ActorDto;
using SoActorDto = Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors.ActorDto;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;

/// <summary>
/// Test-only projection of an end-user dialog view back into a service-owner update DTO. This replicates the
/// by-convention AutoMapper mapping previously used by <see cref="IFlowStepExtensions"/> (issue #967): only
/// members that exist on both DTOs are copied; service-owner-only members (search tags, authorization
/// contexts, idempotency keys, non-sensitive content) are left at their defaults, exactly as AutoMapper did.
/// </summary>
internal static class EndUserDialogMappingExtensions
{
    public static Up.UpdateDialogDto ToUpdateDialogDto(this Eu.DialogDto source) => new()
    {
        Progress = source.Progress,
        ExtendedStatus = source.ExtendedStatus,
        ExternalReference = source.ExternalReference,
        DueAt = source.DueAt,
        Process = source.Process,
        PrecedingProcess = source.PrecedingProcess,
        ExpiresAt = source.ExpiresAt,
        IsApiOnly = source.IsApiOnly,
        Status = (DialogStatusInput)source.Status,
        Content = ToContentDto(source.Content),
        Attachments = source.Attachments.Select(ToAttachmentDto).ToList(),
        Transmissions = source.Transmissions.Select(ToTransmissionDto).ToList(),
        GuiActions = source.GuiActions.Select(ToGuiActionDto).ToList(),
        ApiActions = source.ApiActions.Select(ToApiActionDto).ToList(),
        Activities = source.Activities.Select(ToActivityDto).ToList()
    };

    private static Up.ContentDto ToContentDto(Eu.ContentDto source) => new()
    {
        Title = source.Title,
        Summary = source.Summary,
        SenderName = source.SenderName,
        AdditionalInfo = source.AdditionalInfo,
        ExtendedStatus = source.ExtendedStatus,
        MainContentReference = source.MainContentReference
    };

    private static Up.AttachmentDto ToAttachmentDto(Eu.DialogAttachmentDto source) => new()
    {
        Id = source.Id,
        DisplayName = source.DisplayName,
        Name = source.Name,
        Urls = source.Urls.Select(ToAttachmentUrlDto).ToList(),
        ExpiresAt = source.ExpiresAt
    };

    private static Up.AttachmentUrlDto ToAttachmentUrlDto(Eu.DialogAttachmentUrlDto source) => new()
    {
        Id = source.Id,
        Url = source.Url,
        MediaType = source.MediaType,
        ConsumerType = source.ConsumerType
    };

    private static Up.GuiActionDto ToGuiActionDto(Eu.DialogGuiActionDto source) => new()
    {
        Id = source.Id,
        Action = source.Action,
        Url = source.Url,
        AuthorizationAttribute = source.AuthorizationAttribute,
        IsDeleteDialogAction = source.IsDeleteDialogAction,
        HttpMethod = source.HttpMethod,
        Priority = source.Priority,
        Title = source.Title,
        Prompt = source.Prompt
    };

    private static Up.ApiActionDto ToApiActionDto(Eu.DialogApiActionDto source) => new()
    {
        Id = source.Id,
        Action = source.Action,
        AuthorizationAttribute = source.AuthorizationAttribute,
        Name = source.Name,
        Endpoints = source.Endpoints.Select(ToApiActionEndpointDto).ToList()
    };

    private static Up.ApiActionEndpointDto ToApiActionEndpointDto(Eu.DialogApiActionEndpointDto source) => new()
    {
        Id = source.Id,
        Version = source.Version,
        Url = source.Url,
        HttpMethod = source.HttpMethod,
        DocumentationUrl = source.DocumentationUrl,
        RequestSchema = source.RequestSchema,
        ResponseSchema = source.ResponseSchema,
        Deprecated = source.Deprecated,
        SunsetAt = source.SunsetAt
    };

    private static Up.TransmissionDto ToTransmissionDto(Eu.DialogTransmissionDto source) => new()
    {
        Id = source.Id,
        CreatedAt = source.CreatedAt,
        AuthorizationAttribute = source.AuthorizationAttribute,
        ExtendedType = source.ExtendedType,
        ExternalReference = source.ExternalReference,
        RelatedTransmissionId = source.RelatedTransmissionId,
        Type = source.Type,
        Sender = ToActorDto(source.Sender),
        Content = ToTransmissionContentDto(source.Content),
        Attachments = source.Attachments.Select(ToTransmissionAttachmentDto).ToList(),
        NavigationalActions = source.NavigationalActions.Select(ToNavigationalActionDto).ToList()
    };

    private static Up.TransmissionContentDto ToTransmissionContentDto(Eu.DialogTransmissionContentDto source) => new()
    {
        Title = source.Title,
        Summary = source.Summary,
        ContentReference = source.ContentReference
    };

    private static Up.TransmissionAttachmentDto ToTransmissionAttachmentDto(Eu.DialogTransmissionAttachmentDto source) => new()
    {
        Id = source.Id,
        DisplayName = source.DisplayName,
        Name = source.Name,
        Urls = source.Urls.Select(ToTransmissionAttachmentUrlDto).ToList(),
        ExpiresAt = source.ExpiresAt
    };

    private static Up.TransmissionAttachmentUrlDto ToTransmissionAttachmentUrlDto(Eu.DialogTransmissionAttachmentUrlDto source) => new()
    {
        Url = source.Url,
        MediaType = source.MediaType,
        ConsumerType = source.ConsumerType
    };

    private static Up.TransmissionNavigationalActionDto ToNavigationalActionDto(Eu.DialogTransmissionNavigationalActionDto source) => new()
    {
        Id = source.Id,
        Title = source.Title,
        Url = source.Url,
        ExpiresAt = source.ExpiresAt
    };

    private static Up.ActivityDto ToActivityDto(Eu.DialogActivityDto source) => new()
    {
        Id = source.Id,
        CreatedAt = source.CreatedAt,
        ExtendedType = source.ExtendedType,
        Type = source.Type,
        TransmissionId = source.TransmissionId,
        PerformedBy = ToActorDto(source.PerformedBy),
        Description = source.Description
    };

    private static SoActorDto ToActorDto(EuActorDto source) => new()
    {
        ActorType = source.ActorType,
        ActorName = source.ActorName,
        ActorId = source.ActorId
    };
}
