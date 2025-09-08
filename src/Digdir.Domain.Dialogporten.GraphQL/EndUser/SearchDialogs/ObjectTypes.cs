using Digdir.Domain.Dialogporten.Application.Common.Pagination.Order;
using Digdir.Domain.Dialogporten.GraphQL.EndUser.Common;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.SearchDialogs;

[InterfaceType("SearchDialogError")]
public interface ISearchDialogError
{
    string Message { get; set; }
}

public sealed class SearchDialogContinuationTokenParsingError : ISearchDialogError
{
    public string Message { get; set; } = "An error occurred while parsing the ContinuationToken parameter";
}

public sealed class SearchDialogOrderByParsingError : ISearchDialogError
{
    public string Message { get; set; } = "An error occurred while parsing the OrderBy parameter";
}

public sealed class SearchDialogForbidden : ISearchDialogError
{
    public string Message { get; set; } = "Forbidden";
}

public sealed class SearchDialogValidationError : ISearchDialogError
{
    public string Message { get; set; } = null!;
}

public sealed class SearchDialogsPayload
{
    public List<SearchDialog>? Items { get; set; }
    public bool HasNextPage { get; set; }

    [GraphQLDescription("Use this token to fetch the next page of dialogs, must be used in combination with OrderBy from the previous response")]
    public string? ContinuationToken { get; set; }

    public List<SearchDialogSortType> OrderBy { get; set; } = [];

    public List<ISearchDialogError> Errors { get; set; } = [];
}

[GraphQLDescription("Set only one property per object.")]
public sealed class SearchDialogSortType
{
    public OrderDirection? CreatedAt { get; set; }
    public OrderDirection? UpdatedAt { get; set; }
    public OrderDirection? DueAt { get; set; }
    public OrderDirection? ContentUpdatedAt { get; set; }
}

public sealed class SearchDialog
{
    [GraphQLDescription("The unique identifier for the dialog in UUIDv7 format. Example: 01913cd5-784f-7d3b-abef-4c77b1f0972d")]
    public Guid Id { get; set; }

    [GraphQLDescription("The service owner code representing the organization (service owner) related to this dialog. Example: ske")]
    public string Org { get; set; } = null!;

    [GraphQLDescription("The service identifier for the service that the dialog is related to in URN-format. This corresponds to a service resource in the Altinn Resource Registry. Example: urn:altinn:resource:some-service-identifier")]
    public string ServiceResource { get; set; } = null!;

    [GraphQLDescription("The ServiceResource type, as defined in Altinn Resource Registry (see ResourceType).")]
    public string ServiceResourceType { get; set; } = null!;

    [GraphQLDescription("The party code representing the organization or person that the dialog belongs to in URN format. Example: urn:altinn:person:identifier-no:01125512345, urn:altinn:organization:identifier-no:912345678")]
    public string Party { get; set; } = null!;

    [GraphQLDescription("Advisory indicator of progress, represented as 1-100 percentage value. 100% representing a dialog that has come to a natural completion (successful or not).")]
    public int? Progress { get; set; }

    [GraphQLDescription("Optional process identifier used to indicate a business process this dialog belongs to.")]
    public string? Process { get; set; }

    [GraphQLDescription("Optional preceding process identifier to indicate the business process that preceded the process indicated in the 'Process' field. Cannot be set without also 'Process' being set.")]
    public string? PrecedingProcess { get; set; }

    [GraphQLDescription("The number of attachments in the dialog made available for browser-based frontends.")]
    public int? GuiAttachmentCount { get; set; }

    [GraphQLDescription("Arbitrary string with a service-specific indicator of status, typically used to indicate a fine-grained state of the dialog to further specify the 'status' enum. Refer to the service-specific documentation provided by the service owner for details on the possible values (if in use).")]
    public string? ExtendedStatus { get; set; }

    [GraphQLDescription("Arbitrary string with a service-specific reference to an external system or service. Refer to the service-specific documentation provided by the service owner for details (if in use).")]
    public string? ExternalReference { get; set; }

    [GraphQLDescription("The date and time when the dialog was created. Example: 2022-12-31T23:59:59Z")]
    public DateTimeOffset CreatedAt { get; set; }

    [GraphQLDescription("The date and time when the dialog was last updated. Example: 2022-12-31T23:59:59Z")]
    public DateTimeOffset UpdatedAt { get; set; }

    [GraphQLDescription("The date and time when the dialog content was last updated. Example: 2022-12-31T23:59:59Z")]
    public DateTimeOffset ContentUpdatedAt { get; set; }

    [GraphQLDescription("The due date for the dialog. This is the last date when the dialog is expected to be completed. Example: 2022-12-31T23:59:59Z")]
    public DateTimeOffset? DueAt { get; set; }

    [GraphQLDescription("The aggregated status of the dialog.")]
    public DialogStatus Status { get; set; }

    [GraphQLDescription("Indicates whether the dialog contains content that has not been viewed or opened by the user yet.")]
    public bool HasUnopenedContent { get; set; }

    [GraphQLDescription("Indicates if this dialog is intended for API consumption only and should not be shown in frontends aimed at humans")]
    public bool IsApiOnly { get; set; }

    [GraphQLDescription("The number of transmissions sent by the service owner")]
    public int FromServiceOwnerTransmissionsCount { get; set; }

    [GraphQLDescription("The number of transmissions sent by a party representative")]
    public int FromPartyTransmissionsCount { get; set; }

    [GraphQLDescription("The latest entry in the dialog's activity log.")]
    public Activity? LatestActivity { get; set; }

    [GraphQLDescription("The content of the dialog in search results.")]
    public SearchContent Content { get; set; } = null!;

    [GraphQLDescription("The list of seen log entries for the dialog newer than the dialog UpdatedAt date.")]
    public List<SeenLog> SeenSinceLastUpdate { get; set; } = [];

    [GraphQLDescription("The list of seen log entries for the dialog newer than the dialog ContentUpdatedAt date.")]
    public List<SeenLog> SeenSinceLastContentUpdate { get; set; } = [];

    [GraphQLDescription("Metadata about the dialog owned by end-users.")]
    public EndUserContext EndUserContext { get; set; } = null!;
}

public sealed class SearchContent
{
    [GraphQLDescription("The title of the dialog.")]
    public ContentValue Title { get; set; } = null!;

    [GraphQLDescription("A short summary of the dialog and its current state.")]
    public ContentValue? Summary { get; set; }

    [GraphQLDescription("Overridden sender name. If not supplied, assume 'org' as the sender name.")]
    public ContentValue? SenderName { get; set; }

    [GraphQLDescription("Used as the human-readable label used to describe the 'ExtendedStatus' field.")]
    public ContentValue? ExtendedStatus { get; set; }
}

public sealed class SearchDialogInput
{
    [GraphQLDescription("Filter by one or more service owner codes")]
    public List<string>? Org { get; init; }

    [GraphQLDescription("Filter by one or more service resources")]
    public List<string>? ServiceResource { get; init; }

    [GraphQLDescription("Filter by one or more owning parties")]
    public List<string>? Party { get; init; }

    [GraphQLDescription("Filter by one or more extended statuses")]
    public List<string>? ExtendedStatus { get; init; }

    [GraphQLDescription("Filter by external reference")]
    public string? ExternalReference { get; init; }

    [GraphQLDescription("Filter by status")]
    public List<DialogStatus>? Status { get; init; }

    [GraphQLDescription("Filter by process")]
    public string? Process { get; init; }

    [GraphQLDescription("Filter by system label")]
    public List<SystemLabel>? SystemLabel { get; init; }

    [GraphQLDescription("Whether to exclude API-only dialogs from the results. Defaults to false.")]
    public bool? ExcludeApiOnly { get; init; } = false;

    [GraphQLDescription("Only return dialogs created after this date")]
    public DateTimeOffset? CreatedAfter { get; init; }

    [GraphQLDescription("Only return dialogs created before this date")]
    public DateTimeOffset? CreatedBefore { get; init; }

    [GraphQLDescription("Only return dialogs with content updated after this date")]
    public DateTimeOffset? ContentUpdatedAfter { get; init; }

    [GraphQLDescription("Only return dialogs with content updated before this date")]
    public DateTimeOffset? ContentUpdatedBefore { get; init; }

    [GraphQLDescription("Only return dialogs updated after this date")]
    public DateTimeOffset? UpdatedAfter { get; init; }

    [GraphQLDescription("Only return dialogs updated before this date")]
    public DateTimeOffset? UpdatedBefore { get; init; }

    [GraphQLDescription("Only return dialogs with due date after this date")]
    public DateTimeOffset? DueAfter { get; init; }

    [GraphQLDescription("Only return dialogs with due date before this date")]
    public DateTimeOffset? DueBefore { get; init; }

    [GraphQLDescription("Search string for free text search. Will attempt to fuzzily match in all free text fields in the aggregate")]
    public string? Search { get; init; }

    [GraphQLDescription("Limit free text search to texts with this language code, e.g. 'nb', 'en'. Culture codes will be normalized to neutral language codes (ISO 639). Default: search all culture codes")]
    public string? SearchLanguageCode { get; init; }

    [GraphQLDescription("Limit the number of results returned, defaults to 100, max 1000")]
    public int? Limit { get; set; }

    [GraphQLDescription("Continuation token for pagination")]
    public string? ContinuationToken { get; init; }

    [GraphQLDescription("Sort the results by one or more fields")]
    public List<SearchDialogSortType>? OrderBy { get; set; }
}
