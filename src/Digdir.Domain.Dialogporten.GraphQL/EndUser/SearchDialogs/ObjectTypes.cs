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
    public Guid Id { get; set; }
    public string Org { get; set; } = null!;
    public string ServiceResource { get; set; } = null!;
    public string ServiceResourceType { get; set; } = null!;
    public string Party { get; set; } = null!;
    public int? Progress { get; set; }
    public string? Process { get; set; }
    public string? PrecedingProcess { get; set; }
    public int? GuiAttachmentCount { get; set; }
    public string? ExtendedStatus { get; set; }
    public string? ExternalReference { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset ContentUpdatedAt { get; set; }
    public DateTimeOffset? DueAt { get; set; }

    public DialogStatus Status { get; set; }
    public bool HasUnopenedContent { get; set; }

    [GraphQLDescription("Indicates if this dialog is intended for API consumption only and should not be shown in frontends aimed at humans")]
    public bool IsApiOnly { get; set; }

    [GraphQLDescription("The number of transmissions sent by the service owner")]
    public int FromServiceOwnerTransmissionsCount { get; set; }

    [GraphQLDescription("The number of transmissions sent by a party representative")]
    public int FromPartyTransmissionsCount { get; set; }

    public Activity? LatestActivity { get; set; }

    public SearchContent Content { get; set; } = null!;
    public List<SeenLog> SeenSinceLastUpdate { get; set; } = [];
    public List<SeenLog> SeenSinceLastContentUpdate { get; set; } = [];
    public EndUserContext EndUserContext { get; set; } = null!;
}

public sealed class SearchContent
{
    public ContentValue Title { get; set; } = null!;
    public ContentValue? Summary { get; set; }
    public ContentValue? SenderName { get; set; }
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

    [GraphQLDescription("Filter by system label")]
    public List<SystemLabel>? SystemLabel { get; init; }

    [GraphQLDescription("Whether to exclude API-only dialogs from the results. Defaults to false.")]
    public bool? ExcludeApiOnly { get; init; } = false;

    [GraphQLDescription("Only return dialogs created after this date")]
    public DateTimeOffset? CreatedAfter { get; init; }

    [GraphQLDescription("Only return dialogs created before this date")]
    public DateTimeOffset? CreatedBefore { get; init; }

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
