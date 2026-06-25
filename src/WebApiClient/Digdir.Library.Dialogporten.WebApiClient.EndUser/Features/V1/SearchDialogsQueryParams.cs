using Altinn.ApiClients.Dialogporten.EndUser.Features.V1.Contracts;
using Refit;

// TODO: Removing scope based namespace makes diff messy. Remove in different PR
#pragma warning disable IDE0161
namespace Altinn.ApiClients.Dialogporten.EndUser.Features.V1
#pragma warning restore IDE0161
{
    public class SearchDialogsQueryParams
    {

        /// <summary>
        /// Filter by one or more service owner codes
        /// </summary>
        [Query(CollectionFormat.Multi)]
        public IEnumerable<string>? Org { get; set; }

        /// <summary>
        /// Filter by one or more service resources
        /// </summary>
        [Query(CollectionFormat.Multi)]
        public IEnumerable<string>? ServiceResource { get; set; }

        /// <summary>
        /// Filter by one or more owning parties
        /// </summary>
        [Query(CollectionFormat.Multi)]
        public IEnumerable<string>? Party { get; set; }

        /// <summary>
        /// Filter by one or more extended statuses
        /// </summary>
        [Query(CollectionFormat.Multi)]
        public IEnumerable<string>? ExtendedStatus { get; set; }

        /// <summary>
        /// Filter by external reference
        /// </summary>
        [Query]
        public string? ExternalReference { get; set; }

        /// <summary>
        /// Filter by status
        /// </summary>
        [Query(CollectionFormat.Multi)]
        public IEnumerable<DialogStatus>? Status { get; set; }

        /// <summary>
        /// Only return dialogs created after this date
        /// </summary>
        [Query]
        public DateTimeOffset? CreatedAfter { get; set; }

        /// <summary>
        /// Only return dialogs created before this date
        /// </summary>
        [Query]
        public DateTimeOffset? CreatedBefore { get; set; }

        /// <summary>
        /// Only return dialogs updated after this date
        /// </summary>
        [Query]
        public DateTimeOffset? UpdatedAfter { get; set; }

        /// <summary>
        /// Only return dialogs updated before this date
        /// </summary>
        [Query]
        public DateTimeOffset? UpdatedBefore { get; set; }

        /// <summary>
        /// Only return dialogs with content updated after this date
        /// </summary>
        [Query]
        public DateTimeOffset? ContentUpdatedAfter { get; set; }

        /// <summary>
        /// Only return dialogs with content updated before this date
        /// </summary>
        [Query]
        public DateTimeOffset? ContentUpdatedBefore { get; set; }

        /// <summary>
        /// Only return dialogs that have content that has/hasn't been seen.
        /// If null, no filtering is applied
        /// If true, returns dialogs that have been seen
        /// If false, returns dialogs that have not been seen
        /// 
        /// A dialog's content is considered seen if:
        /// - It has been visited by the GET .../dialogs/{dialogId} endpoint since the last content update, and
        /// - It does not have a system label MarkedAsUnopened.
        /// </summary>
        [Query]
        public bool? IsContentSeen { get; set; }

        /// <summary>
        /// Only return dialogs with due date after this date
        /// </summary>
        [Query]
        public DateTimeOffset? DueAfter { get; set; }

        /// <summary>
        /// Only return dialogs with due date before this date
        /// </summary>
        [Query]
        public DateTimeOffset? DueBefore { get; set; }

        /// <summary>
        /// Filter by process
        /// </summary>
        [Query]
        public string? Process { get; set; }

        /// <summary>
        /// Filter by Display state
        /// </summary>
        [Query(CollectionFormat.Multi)]
        public IEnumerable<SystemLabel>? SystemLabel { get; set; }

        /// <summary>
        /// Whether to exclude API-only dialogs from the results. Defaults to false.
        /// </summary>
        [Query]
        public bool? ExcludeApiOnly { get; set; }

        /// <summary>
        /// Search string for free text search. Will attempt to fuzzily match in all free text fields in the aggregate
        /// </summary>
        [Query]
        public string? Search { get; set; }

        /// <summary>
        /// Limit free text search to texts with this language code, e.g. 'nb', 'en'. Culture codes will be normalized to neutral language codes (ISO 639). Default: search all culture codes
        /// </summary>
        [Query]
        public string? SearchLanguageCode { get; set; }

        [Query]
        public string? OrderBy { get; set; }

        /// <summary>
        /// Supply "continuationToken" for the response to get the next page of results, if hasNextPage is true
        /// </summary>
        [Query]
        public string? ContinuationToken { get; set; }

        /// <summary>
        /// Limit the number of results per page (1-1000, default: 100)
        /// </summary>
        [Query]
        public int? Limit { get; set; }

    }

}
