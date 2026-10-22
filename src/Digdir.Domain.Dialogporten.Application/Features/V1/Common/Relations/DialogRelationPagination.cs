using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using FluentValidation;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Relations;

/// <summary>
/// Relations are always ordered by the related dialog's id (creation order, as ids are UUIDv7), so the continuation
/// token is simply the last related dialog id of the previous page.
/// </summary>
public interface IDialogRelationPagination
{
    string? ContinuationToken { get; }
    int? Limit { get; }
}

internal static class DialogRelationPagination
{
    public const string OrderBy = "relateddialogid_asc";

    extension(IDialogRelationPagination pagination)
    {
        public int GetLimit() => pagination.Limit ?? PaginationConstants.DefaultLimit;

        public Guid? GetAfter() => Guid.TryParse(pagination.ContinuationToken, out var after) ? after : null;
    }

    /// <summary>
    /// Builds a page from up to limit + 1 items, where the extra item only tells whether there is a next page.
    /// </summary>
    public static PaginatedList<TResult> ToPage<TItem, TResult>(
        IReadOnlyList<TItem> items,
        int limit,
        Func<TItem, Guid> relatedDialogId,
        Func<TItem, TResult> map)
    {
        var hasNextPage = items.Count > limit;
        var page = items.Take(limit).ToList();
        return new PaginatedList<TResult>(
            page.Select(map),
            hasNextPage,
            page.Count > 0 ? relatedDialogId(page[^1]).ToString() : null,
            OrderBy);
    }
}

internal sealed class DialogRelationPaginationValidator : AbstractValidator<IDialogRelationPagination>
{
    public DialogRelationPaginationValidator()
    {
        RuleFor(x => x.Limit)
            .InclusiveBetween(PaginationConstants.MinLimit, PaginationConstants.MaxLimit);

        RuleFor(x => x.ContinuationToken)
            .Must(x => Guid.TryParse(x, out _))
            .When(x => x.ContinuationToken is not null)
            .WithMessage("Invalid continuation token.");
    }
}
