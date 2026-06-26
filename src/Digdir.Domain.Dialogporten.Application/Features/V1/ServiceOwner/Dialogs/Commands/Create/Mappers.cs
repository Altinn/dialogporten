using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Content;
using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.Attachments;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Transmissions;
using Digdir.Domain.Dialogporten.Domain.DialogServiceOwnerContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Http;
using Digdir.Domain.Dialogporten.Domain.Localizations;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Create;

internal static class Mappers
{
    internal static DialogEntity ToDialogEntity(this CreateDialogDto source) =>
        new()
        {
            Id = source.Id ?? Guid.Empty,
            IdempotentKey = source.IdempotentKey,
            ServiceResource = source.ServiceResource,
            Party = source.Party,
            Progress = source.Progress,
            ExtendedStatus = source.ExtendedStatus,
            ExternalReference = source.ExternalReference,
            VisibleFrom = source.VisibleFrom,
            DueAt = source.DueAt,
            Process = source.Process,
            PrecedingProcess = source.PrecedingProcess,
            ExpiresAt = source.ExpiresAt,
            IsApiOnly = source.IsApiOnly,
            CreatedAt = source.CreatedAt ?? default,
            UpdatedAt = source.UpdatedAt ?? default,
            // Status is assigned in the handler, see CreateDialogCommandHandler.
            Content = source.Content.ToDialogContentList() ?? [],
            SearchTags = source.SearchTags.Select(x => x.ToDialogSearchTag()).ToList(),
            Attachments = source.Attachments.Select(x => x.ToDialogAttachment()).ToList(),
            Transmissions = source.Transmissions.Select(x => x.ToDialogTransmission()).ToList(),
            GuiActions = source.GuiActions.Select(x => x.ToDialogGuiAction()).ToList(),
            ApiActions = source.ApiActions.Select(x => x.ToDialogApiAction()).ToList(),
            Activities = source.Activities.Select(x => x.ToDialogActivity()).ToList()
        };

    internal static List<DialogServiceOwnerLabel> ToDialogServiceOwnerLabels(
        this IEnumerable<ServiceOwnerLabelDto> source) =>
        source.Select(x => new DialogServiceOwnerLabel { Value = x.Value }).ToList();

    private static DialogSearchTag ToDialogSearchTag(this SearchTagDto source) =>
        new() { Value = source.Value };

    private static DialogActivity ToDialogActivity(this ActivityDto source) =>
        new()
        {
            Id = source.Id ?? Guid.Empty,
            CreatedAt = source.CreatedAt ?? default,
            ExtendedType = source.ExtendedType,
            TypeId = source.Type,
            TransmissionId = source.TransmissionId,
            PerformedBy = source.PerformedBy.ToActor<DialogActivityPerformedByActor>(),
            Description = source.Description.ToLocalizationSet<DialogActivityDescription>()
        };

    private static DialogTransmission ToDialogTransmission(this TransmissionDto source) =>
        new()
        {
            Id = source.Id ?? Guid.Empty,
            IdempotentKey = source.IdempotentKey,
            CreatedAt = source.CreatedAt,
            AuthorizationAttribute = source.AuthorizationAttribute,
            ExtendedType = source.ExtendedType,
            ExternalReference = source.ExternalReference,
            RelatedTransmissionId = source.RelatedTransmissionId,
            TypeId = source.Type,
            Sender = source.Sender.ToActor<DialogTransmissionSenderActor>(),
            Content = source.Content.ToDialogTransmissionContentList() ?? [],
            Attachments = source.Attachments.Select(x => x.ToDialogTransmissionAttachment()).ToList(),
            NavigationalActions = source.NavigationalActions.Select(x => x.ToDialogTransmissionNavigationalAction()).ToList()
        };

    private static DialogAttachment ToDialogAttachment(this AttachmentDto source) =>
        new()
        {
            Id = source.Id ?? Guid.Empty,
            Name = source.Name,
            ExpiresAt = source.ExpiresAt,
            DisplayName = source.DisplayName.ToLocalizationSet<AttachmentDisplayName>(),
            Urls = source.Urls.Select(x => x.ToAttachmentUrl()).ToList()
        };

    private static AttachmentUrl ToAttachmentUrl(this AttachmentUrlDto source) =>
        new()
        {
            Id = source.Id ?? Guid.Empty,
            Url = source.Url,
            MediaType = source.MediaType,
            ConsumerTypeId = source.ConsumerType
        };

    private static DialogGuiAction ToDialogGuiAction(this GuiActionDto source) =>
        new()
        {
            Id = source.Id ?? Guid.Empty,
            Action = source.Action,
            Url = source.Url,
            AuthorizationAttribute = source.AuthorizationAttribute,
            IsDeleteDialogAction = source.IsDeleteDialogAction,
            PriorityId = source.Priority,
            HttpMethodId = source.HttpMethod ?? HttpVerb.Values.GET,
            Title = source.Title.ToLocalizationSet<DialogGuiActionTitle>(),
            Prompt = source.Prompt.ToLocalizationSet<DialogGuiActionPrompt>()
        };

    private static DialogApiAction ToDialogApiAction(this ApiActionDto source) =>
        new()
        {
            Id = source.Id ?? Guid.Empty,
            Action = source.Action,
            AuthorizationAttribute = source.AuthorizationAttribute,
            Name = source.Name,
            Endpoints = source.Endpoints.Select(x => x.ToDialogApiActionEndpoint()).ToList()
        };

    private static DialogApiActionEndpoint ToDialogApiActionEndpoint(this ApiActionEndpointDto source) =>
        new()
        {
            Id = source.Id ?? Guid.Empty,
            Version = source.Version,
            Url = source.Url,
            HttpMethodId = source.HttpMethod,
            DocumentationUrl = source.DocumentationUrl,
            RequestSchema = source.RequestSchema,
            ResponseSchema = source.ResponseSchema,
            Deprecated = source.Deprecated,
            SunsetAt = source.SunsetAt
        };

    private static DialogTransmissionAttachment ToDialogTransmissionAttachment(this TransmissionAttachmentDto source) =>
        new()
        {
            Id = source.Id ?? Guid.Empty,
            Name = source.Name,
            ExpiresAt = source.ExpiresAt,
            DisplayName = source.DisplayName.ToLocalizationSet<AttachmentDisplayName>(),
            Urls = source.Urls.Select(x => x.ToAttachmentUrl()).ToList()
        };

    private static AttachmentUrl ToAttachmentUrl(this TransmissionAttachmentUrlDto source) =>
        new()
        {
            Url = source.Url,
            MediaType = source.MediaType,
            ConsumerTypeId = source.ConsumerType
        };

    private static DialogTransmissionNavigationalAction ToDialogTransmissionNavigationalAction(
        this TransmissionNavigationalActionDto source) =>
        new()
        {
            Url = source.Url,
            ExpiresAt = source.ExpiresAt,
            Title = source.Title.ToLocalizationSet<DialogTransmissionNavigationalActionTitle>()!
        };

    private static TLocalizationSet? ToLocalizationSet<TLocalizationSet>(
        this IEnumerable<LocalizationDto>? source)
        where TLocalizationSet : LocalizationSet, new()
    {
        var localizations = source as ICollection<LocalizationDto> ?? source?.ToList();
        if (localizations is null || localizations.Count == 0)
        {
            return null;
        }

        var destination = new TLocalizationSet();
        destination.Localizations.MergeFrom(localizations);
        return destination;
    }
}
