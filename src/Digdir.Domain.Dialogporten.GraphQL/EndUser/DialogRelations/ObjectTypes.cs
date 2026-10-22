using Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.SearchDialogs;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.DialogRelations;

[InterfaceType("DialogRelationsError")]
public interface IDialogRelationsError
{
    string Message { get; set; }
}

public sealed class DialogRelationsNotFound : IDialogRelationsError
{
    public required string Message { get; set; }
}

public sealed class DialogRelationsValidationError : IDialogRelationsError
{
    public required string Message { get; set; }
}

public enum DialogRelationType
{
    [GraphQLDescription("The related dialog is the parent of the dialog.")]
    Parent = 1,

    [GraphQLDescription("The related dialog is a child of the dialog.")]
    Child = 2,

    [GraphQLDescription("The dialogs are related, without a hierarchy.")]
    Related = 3
}

public sealed class DialogRelationsInput
{
    [GraphQLDescription("Only return relations of this type, as seen from the dialog.")]
    public DialogRelationType? Type { get; init; }

    [GraphQLDescription("Filter by one or more parties owning the related dialogs.")]
    public List<string>? Party { get; init; }

    [GraphQLDescription("Filter by one or more service resources of the related dialogs.")]
    public List<string>? ServiceResource { get; init; }

    [GraphQLDescription("Filter by one or more statuses of the related dialogs.")]
    public List<DialogStatus>? Status { get; init; }

    [GraphQLDescription("Whether to exclude API-only dialogs from the results. Defaults to false.")]
    public bool? ExcludeApiOnly { get; init; }

    [GraphQLDescription("Limit the number of results returned. Defaults to 100, maximum 1000.")]
    public int? Limit { get; init; }

    [GraphQLDescription("Continuation token for pagination.")]
    public string? ContinuationToken { get; init; }
}

public sealed class DialogRelationsPayload
{
    [GraphQLDescription("The related dialogs the user may see. Relations to dialogs the user cannot access are left out silently.")]
    public List<DialogRelation>? Items { get; set; }

    [GraphQLDescription("Whether there are more items available that can be fetched by supplying the continuation token.")]
    public bool HasNextPage { get; set; }

    [GraphQLDescription("The continuation token to be used to fetch the next page of items.")]
    public string? ContinuationToken { get; set; }

    public List<IDialogRelationsError> Errors { get; set; } = [];
}

public sealed class DialogRelation
{
    [GraphQLDescription("The type of the relation, as seen from the dialog the relations were fetched for.")]
    public DialogRelationType RelationType { get; set; }

    [GraphQLDescription("The related dialog.")]
    public RelatedDialog Dialog { get; set; } = null!;
}

[GraphQLDescription("A related dialog. A subset of the dialog search list item.")]
public sealed class RelatedDialog
{
    [GraphQLDescription("The unique identifier for the dialog in UUIDv7 format.")]
    public Guid Id { get; set; }

    [GraphQLDescription("The service owner code representing the organization (service owner) related to this dialog.")]
    public string Org { get; set; } = null!;

    [GraphQLDescription("The service identifier for the service that the dialog is related to in URN-format.")]
    public string ServiceResource { get; set; } = null!;

    [GraphQLDescription("The ServiceResource type, as defined in Altinn Resource Registry (see ResourceType).")]
    public string ServiceResourceType { get; set; } = null!;

    [GraphQLDescription("The party code representing the organization or person that the dialog belongs to in URN format.")]
    public string Party { get; set; } = null!;

    [GraphQLDescription("The aggregated status of the dialog.")]
    public DialogStatus Status { get; set; }

    [GraphQLDescription("Arbitrary string with a service-specific indicator of status.")]
    public string? ExtendedStatus { get; set; }

    [GraphQLDescription("The date and time when the dialog was created.")]
    public DateTimeOffset CreatedAt { get; set; }

    [GraphQLDescription("The date and time when the dialog was last updated.")]
    public DateTimeOffset UpdatedAt { get; set; }

    [GraphQLDescription("The date and time when the dialog content was last updated.")]
    public DateTimeOffset ContentUpdatedAt { get; set; }

    [GraphQLDescription("The due date for the dialog. This is the last date when the dialog is expected to be completed.")]
    public DateTimeOffset? DueAt { get; set; }

    [GraphQLDescription("Indicates if this dialog is intended for API consumption only and should not be shown in frontends aimed at humans.")]
    public bool IsApiOnly { get; set; }

    [GraphQLDescription("Indicates whether the related dialog has relations of its own.")]
    public bool HasRelations { get; set; }

    [GraphQLDescription("The content of the dialog, as in search results.")]
    public SearchContent Content { get; set; } = null!;
}
