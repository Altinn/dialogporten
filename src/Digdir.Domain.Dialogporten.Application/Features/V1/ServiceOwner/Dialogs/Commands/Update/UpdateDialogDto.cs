﻿using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Actions;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.DialogElements;
using Digdir.Domain.Dialogporten.Domain.Http;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Commands.Update;

public sealed class UpdateDialogDto
{
    public string? ExtendedStatus { get; set; }
    public DateTimeOffset? VisibleFrom { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }

    public DialogStatus.Values Status { get; set; }

    public List<LocalizationDto> Body { get; set; } = new();
    public List<LocalizationDto> Title { get; set; } = new();
    public List<LocalizationDto> SenderName { get; set; } = new();
    public List<UpdateDialogSearchTagDto> SearchTags { get; set; } = new();

    public List<UpdateDialogDialogElementDto> Elements { get; set; } = new();
    public List<UpdateDialogDialogGuiActionDto> GuiActions { get; set; } = new();
    public List<UpdateDialogDialogApiActionDto> ApiActions { get; set; } = new();
    public List<UpdateDialogDialogActivityDto> Activities { get; set; } = new();
}

public sealed class UpdateDialogSearchTagDto
{
    public string Value { get; set; } = null!;
}

public class UpdateDialogDialogActivityDto
{
    public Guid? Id { get; set; }
    public DateTimeOffset? CreatedAt { get; set; }
    public Uri? ExtendedType { get; set; }

    public DialogActivityType.Values Type { get; set; }

    public Guid? RelatedActivityId { get; set; }
    public Guid? DialogElementId { get; set; }

    public List<LocalizationDto> PerformedBy { get; set; } = new();
    public List<LocalizationDto> Description { get; set; } = new();
}

public sealed class UpdateDialogDialogApiActionDto
{
    public Guid? Id { get; set; }
    public string Action { get; set; } = null!;
    public string? AuthorizationAttribute { get; set; }

    public Guid? DialogElementId { get; set; }

    public List<UpdateDialogDialogApiActionEndpointDto> Endpoints { get; set; } = new();
}

public sealed class UpdateDialogDialogApiActionEndpointDto
{
    public Guid? Id { get; set; }
    public string? Version { get; set; }
    public Uri Url { get; set; } = null!;
    public HttpVerb.Values HttpMethod { get; set; }
    public Uri? DocumentationUrl { get; set; }
    public Uri? RequestSchema { get; set; }
    public Uri? ResponseSchema { get; set; }
    public bool Deprecated { get; set; }
    public DateTimeOffset? SunsetAt { get; set; }
}

public sealed class UpdateDialogDialogGuiActionDto
{
    public Guid? Id { get; set; }
    public string Action { get; set; } = null!;
    public Uri Url { get; set; } = null!;
    public string? AuthorizationAttribute { get; set; }
    public bool IsBackChannel { get; set; }
    public bool IsDeleteAction { get; set; }

    public DialogGuiActionPriority.Values Priority { get; set; }

    public List<LocalizationDto> Title { get; set; } = new();
}

public class UpdateDialogDialogElementDto
{
    public Guid? Id { get; set; }
    public Uri? Type { get; set; }
    public string? AuthorizationAttribute { get; set; }

    public Guid? RelatedDialogElementId { get; set; }

    public List<LocalizationDto> DisplayName { get; set; } = new();
    public List<UpdateDialogDialogElementUrlDto> Urls { get; set; } = new();
}

public sealed class UpdateDialogDialogElementUrlDto
{
    public Guid? Id { get; set; }
    public Uri Url { get; set; } = null!;
    public string? MimeType { get; set; }

    public DialogElementUrlConsumerType.Values ConsumerType { get; set; }
}
