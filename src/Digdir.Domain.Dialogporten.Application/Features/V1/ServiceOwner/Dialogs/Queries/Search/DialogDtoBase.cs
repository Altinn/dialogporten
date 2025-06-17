using Digdir.Domain.Dialogporten.Application.Features.V1.Common.Localizations;
using Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Common.Actors;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities;
using Digdir.Domain.Dialogporten.Domain.Dialogs.Entities.Activities;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.Search;

public class DialogDtoBase
{
    /// <summary>
    /// The unique identifier for the dialog in UUIDv7 format.
    /// </summary>
    /// <example>01913cd5-784f-7d3b-abef-4c77b1f0972d</example>
    public Guid Id { get; set; }

    /// <summary>
    /// The service owner code representing the organization (service owner) related to this dialog.
    /// </summary>
    /// <example>ske</example>
    public string Org { get; set; } = null!;

    /// <summary>
    /// The unique identifier for the revision in UUIDv4 format.
    /// </summary>
    /// <example>a312cb9c-7632-43c2-aa38-69b06aed56ca</example>
    public Guid Revision { get; set; }

    /// <summary>
    /// The service identifier for the service that the dialog is related to in URN-format.
    /// This corresponds to a service resource in the Altinn Resource Registry.
    /// </summary>
    /// <example>urn:altinn:resource:some-service-identifier</example>
    public string ServiceResource { get; set; } = null!;

    /// <summary>
    /// The ServiceResource type, as defined in Altinn Resource Registry (see ResourceType).
    /// </summary>
    public string ServiceResourceType { get; set; } = null!;

    /// <summary>
    /// The party code representing the organization or person that the dialog belongs to in URN format.
    /// </summary>
    /// <example>
    /// urn:altinn:person:identifier-no:01125512345
    /// urn:altinn:organization:identifier-no:912345678
    /// </example>
    public string Party { get; set; } = null!;

    /// <summary>
    /// Advisory indicator of progress, represented as 1-100 percentage value. 100% representing a dialog that has come
    /// to a natural completion (successful or not).
    /// </summary>
    public int? Progress { get; set; }

    /// <summary>
    /// Optional process identifier used to indicate a business process this dialog belongs to.
    /// </summary>
    public string? Process { get; set; }

    /// <summary>
    /// Optional preceding process identifier to indicate the business process that preceded the process indicated in the "Process" field. Cannot be set without also "Process" being set.
    /// </summary>
    public string? PrecedingProcess { get; set; }

    /// <summary>
    /// The number of attachments in the dialog made available for browser-based frontends.
    /// </summary>
    public int? GuiAttachmentCount { get; set; }

    /// <summary>
    /// Arbitrary string with a service-specific indicator of status, typically used to indicate a fine-grained state of
    /// the dialog to further specify the "status" enum.
    ///
    /// Refer to the service-specific documentation provided by the service owner for details on the possible values (if
    /// in use).
    /// </summary>
    public string? ExtendedStatus { get; set; }

    /// <summary>
    /// Arbitrary string with a service-specific reference to an external system or service.
    ///
    /// Refer to the service-specific documentation provided by the service owner for details (if in use).
    /// </summary>
    public string? ExternalReference { get; set; }

    /// <summary>
    /// The date and time when the dialog was created.
    /// </summary>
    /// <example>2022-12-31T23:59:59Z</example>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// The date and time when the dialog was last updated.
    /// </summary>
    /// <example>2022-12-31T23:59:59Z</example>
    public DateTimeOffset UpdatedAt { get; set; }

    /// <summary>
    /// The due date for the dialog. This is the last date when the dialog is expected to be completed.
    /// </summary>
    /// <example>2022-12-31T23:59:59Z</example>
    public DateTimeOffset? DueAt { get; set; }

    /// <summary>
    /// If deleted, the date and time when the deletion was performed.
    /// </summary>
    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>
    /// The timestamp when the dialog will be made visible for authorized end users.
    /// </summary>
    public DateTimeOffset? VisibleFrom { get; set; }

    /// <summary>
    /// The aggregated status of the dialog.
    /// </summary>
    public DialogStatus.Values Status { get; set; }

    /// <summary>
    /// Indicates if this dialog is intended for API consumption only and should not be shown in frontends aimed at humans.
    /// </summary>
    public bool IsApiOnly { get; set; }

    /// <summary>
    /// The latest entry in the dialog's activity log.
    /// </summary>
    public DialogActivityDto? LatestActivity { get; set; }

    /// <summary>
    /// The list of seen log entries for the dialog newer than the dialog ChangedAt date.
    /// </summary>
    public List<DialogSeenLogDto> SeenSinceLastUpdate { get; set; } = [];

    /// <summary>
    /// Metadata about the dialog owned by the service owner.
    /// </summary>
    public DialogServiceOwnerContextDto ServiceOwnerContext { get; set; } = null!;

    /// <summary>
    /// Metadata about the dialog owned by end-users.
    /// </summary>
    public DialogEndUserContextDto EndUserContext { get; set; } = null!;
}

public sealed class DialogEndUserContextDto
{
    /// <summary>
    /// The unique identifier for the end user context revision in UUIDv4 format.
    /// </summary>
    /// <example>0196fccd-bf48-7d27-bdfc-4ad3b0f3bee5</example>
    public Guid Revision { get; set; }

    /// <summary>
    /// System defined labels used to categorize dialogs.
    /// </summary>
    public List<SystemLabel.Values> SystemLabels { get; set; } = [];
}

public sealed class DialogServiceOwnerContextDto
{
    /// <summary>
    /// The unique identifier for the service owner context revision in UUIDv4 format.
    /// </summary>
    /// <example>0196fccd-bf48-7d27-bdfc-4ad3b0f3bee5</example>
    public Guid Revision { get; set; }

    /// <summary>
    /// A list of labels, not visible in end-user APIs.
    /// </summary>
    public List<ServiceOwnerLabelDto> ServiceOwnerLabels { get; set; } = [];
}

public sealed class ServiceOwnerLabelDto
{
    /// <summary>
    /// A label value.
    /// </summary>
    public string Value { get; set; } = null!;
}

public sealed class DialogSeenLogDto
{
    /// <summary>
    /// The unique identifier for the seen log entry in UUIDv7 format.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The timestamp when the dialog revision was seen.
    /// </summary>
    public DateTimeOffset SeenAt { get; set; }

    /// <summary>
    /// The actor that saw the dialog revision.
    /// </summary>
    public ActorDto SeenBy { get; set; } = null!;

    /// <summary>
    /// Flag indicating whether the seen log entry was created via the service owner.
    ///
    /// This is used when the service owner uses the service owner API to implement its own frontend.
    /// </summary>
    public bool? IsViaServiceOwner { get; set; }

    /// <summary>
    /// Flag indicating whether the seen log entry was created by the end user supplied in the query.
    /// </summary>
    public bool IsCurrentEndUser { get; set; }
}

public sealed class DialogActivityDto
{
    /// <summary>
    /// The unique identifier for the activity in UUIDv7 format.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// The date and time when the activity was created.
    /// </summary>
    public DateTimeOffset? CreatedAt { get; set; }

    /// <summary>
    /// An arbitrary string with a service-specific activity type.
    ///
    /// Consult the service-specific documentation provided by the service owner for details (if in use).
    /// </summary>
    public Uri? ExtendedType { get; set; }

    /// <summary>
    /// The type of activity.
    /// </summary>
    public DialogActivityType.Values Type { get; set; }

    /// <summary>
    /// If the activity is related to a particular transmission, this field will contain the transmission identifier.
    /// </summary>
    public Guid? TransmissionId { get; set; }

    /// <summary>
    /// The actor that performed the activity.
    /// </summary>
    public ActorDto PerformedBy { get; set; } = null!;

    /// <summary>
    /// Unstructured text describing the activity. Only set if the activity type is "Information".
    /// </summary>
    public List<LocalizationDto> Description { get; set; } = [];
}
