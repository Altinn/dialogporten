using Digdir.Domain.Dialogporten.Application.Common.Authorization;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogById;

[InterfaceType("DialogByIdError")]
public interface IDialogByIdError
{
    string Message { get; set; }
}

public sealed class DialogByIdNotFound : IDialogByIdError
{
    public string Message { get; set; } = null!;
}

public sealed class DialogByIdDeleted : IDialogByIdError
{
    public string Message { get; set; } = null!;
}

public sealed class DialogByIdForbidden : IDialogByIdError
{
    public string Message { get; set; } = "Forbidden";
}

public sealed class DialogByIdForbiddenAuthLevelTooLow : IDialogByIdError
{
    public string Message { get; set; } = Constants.AltinnAuthLevelTooLow;
}

public sealed class DialogByIdPayload
{
    public Dialog? Dialog { get; set; }
    public List<IDialogByIdError> Errors { get; set; } = [];
}

public sealed class Dialog
{
    public Guid Id { get; set; }
    public Guid Revision { get; set; }
    public string Org { get; set; } = null!;
    public string ServiceResource { get; set; } = null!;
    public string ServiceResourceType { get; set; } = null!;
    public string Party { get; set; } = null!;
    public int? Progress { get; set; }
    public string? Process { get; set; }
    public string? PrecedingProcess { get; set; }
    public string? ExtendedStatus { get; set; }
    public string? ExternalReference { get; set; }
    public DateTimeOffset? VisibleFrom { get; set; }
    public DateTimeOffset? DueAt { get; set; }
    public DateTimeOffset? ExpiresAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset ContentUpdatedAt { get; set; }

    public string? DialogToken { get; set; }

    public DialogStatus Status { get; set; }
    public bool HasUnopenedContent { get; set; }

    [GraphQLDescription("The number of transmissions sent by the service owner")]
    public int FromServiceOwnerTransmissionsCount { get; set; }

    [GraphQLDescription("The number of transmissions sent by a party representative")]
    public int FromPartyTransmissionsCount { get; set; }

    [GraphQLDescription("Indicates if this dialog is intended for API consumption only and should not be shown in frontends aimed at humans")]
    public bool IsApiOnly { get; set; }

    public Content Content { get; set; } = null!;
    public List<Attachment> Attachments { get; set; } = [];
    public List<GuiAction> GuiActions { get; set; } = [];
    public List<ApiAction> ApiActions { get; set; } = [];
    public List<Activity> Activities { get; set; } = [];
    public List<SeenLog> SeenSinceLastUpdate { get; set; } = [];
    public List<SeenLog> SeenSinceLastContentUpdate { get; set; } = [];
    public List<Transmission> Transmissions { get; set; } = [];
    public EndUserContext EndUserContext { get; set; } = null!;
}

public sealed class Transmission
{
    public Guid Id { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? AuthorizationAttribute { get; set; }
    public bool IsAuthorized { get; set; }
    public Uri? ExtendedType { get; set; }
    public Guid? RelatedTransmissionId { get; set; }

    public TransmissionType Type { get; set; }
    public Actor Sender { get; set; } = null!;
    public TransmissionContent Content { get; set; } = null!;
    public List<Attachment> Attachments { get; set; } = [];
}

public enum TransmissionType
{
    [GraphQLDescription("For general information, not related to any submissions")]
    Information = 1,

    [GraphQLDescription("Feedback/receipt accepting a previous submission")]
    Acceptance = 2,

    [GraphQLDescription("Feedback/error message rejecting a previous submission")]
    Rejection = 3,

    [GraphQLDescription("Question/request for more information")]
    Request = 4,

    [GraphQLDescription("Critical information about the process")]
    Alert = 5,

    [GraphQLDescription("Information about a formal decision ('resolution')")]
    Decision = 6,

    [GraphQLDescription("A normal submission of some information/form")]
    Submission = 7,

    [GraphQLDescription("A submission correcting/overriding some previously submitted information")]
    Correction = 8
}

public sealed class Content
{
    public ContentValue Title { get; set; } = null!;
    public ContentValue? Summary { get; set; }
    public ContentValue? SenderName { get; set; }
    public ContentValue? AdditionalInfo { get; set; }
    public ContentValue? ExtendedStatus { get; set; }
    public ContentValue? MainContentReference { get; set; }
}

public sealed class TransmissionContent
{
    public ContentValue Title { get; set; } = null!;
    public ContentValue? Summary { get; set; }
    public ContentValue? ContentReference { get; set; }
}

public sealed class ApiAction
{
    public Guid Id { get; set; }
    public string Action { get; set; } = null!;
    public string? AuthorizationAttribute { get; set; }
    public bool IsAuthorized { get; set; }
    public string? Name { get; set; }

    public List<ApiActionEndpoint> Endpoints { get; set; } = [];
}

// ReSharper disable InconsistentNaming
public enum HttpVerb
{
    GET = 1,
    POST = 2,
    PUT = 3,
    PATCH = 4,
    DELETE = 5,
    HEAD = 6,
    OPTIONS = 7,
    TRACE = 8,
    CONNECT = 9
}

public sealed class ApiActionEndpoint
{
    public Guid Id { get; set; }
    public string? Version { get; set; }
    public Uri Url { get; set; } = null!;
    public HttpVerb HttpMethod { get; set; }
    public Uri? DocumentationUrl { get; set; }
    public Uri? RequestSchema { get; set; }
    public Uri? ResponseSchema { get; set; }
    public bool Deprecated { get; set; }
    public DateTimeOffset? SunsetAt { get; set; }
}

public sealed class GuiAction
{
    public Guid Id { get; set; }
    public string Action { get; set; } = null!;
    public Uri Url { get; set; } = null!;
    public string? AuthorizationAttribute { get; set; }
    public bool IsAuthorized { get; set; }
    public bool IsDeleteDialogAction { get; set; }

    public GuiActionPriority Priority { get; set; }
    public HttpVerb HttpMethod { get; set; }

    public List<Localization> Title { get; set; } = [];
    public List<Localization>? Prompt { get; set; } = [];
}

public enum GuiActionPriority
{
    Primary = 1,
    Secondary = 2,
    Tertiary = 3
}

public sealed class Attachment
{
    public Guid Id { get; set; }

    public List<Localization> DisplayName { get; set; } = [];
    public List<AttachmentUrl> Urls { get; set; } = [];
}

public sealed class AttachmentUrl
{
    public Guid Id { get; set; }
    public Uri Url { get; set; } = null!;
    public string? MediaType { get; set; }

    public AttachmentUrlConsumer ConsumerType { get; set; }
}

public enum AttachmentUrlConsumer
{
    Gui = 1,
    Api = 2
}

public sealed class DialogEventPayload
{
    public Guid Id { get; set; }
    public DialogEventType Type { get; set; }
}

public enum DialogEventType
{
    DialogUpdated = 1,
    DialogDeleted = 2
}
