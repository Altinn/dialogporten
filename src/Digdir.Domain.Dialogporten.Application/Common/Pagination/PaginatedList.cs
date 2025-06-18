using Digdir.Domain.Dialogporten.Application.Common.Pagination.Extensions;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.OrderOption;

namespace Digdir.Domain.Dialogporten.Application.Common.Pagination;

public sealed class PaginatedList<T>
{
    /// <summary>
    /// The paginated list of items
    /// </summary>
    public List<T> Items { get; }

    /// <summary>
    /// Whether there are more items available that can be fetched by supplying the continuation token
    /// </summary>
    public bool HasNextPage { get; }

    /// <summary>
    /// The continuation token to be used to fetch the next page of items
    /// </summary>
    /// <example>createdat_2024-07-31T09:09:03.0257090Z,id_0c089101-b7cf-a476-955c-f00a78d74a4e</example>
    public string? ContinuationToken { get; }

    /// <summary>
    /// The current sorting order of the items
    /// </summary>
    /// <example>createdat_desc,id_desc</example>
    public string OrderBy { get; }

    public PaginatedList(IEnumerable<T> items, bool hasNextPage, string? @continue, string orderBy)
    {
        ContinuationToken = @continue;
        HasNextPage = hasNextPage;
        OrderBy = orderBy;
        Items = items?.ToList() ?? throw new ArgumentNullException(nameof(items));
    }

    /// <summary>
    /// Converts the items in the paginated list to a different type.
    /// </summary>
    /// <typeparam name="TDestination">The type to convert the items to.</typeparam>
    /// <param name="map">A function to convert each item to the new type.</param>
    /// <returns>A new <see cref="PaginatedList{TDestination}"/> with the converted items.</returns>
    public PaginatedList<TDestination> ConvertTo<TDestination>(Func<T, TDestination> map)
    {
        return new PaginatedList<TDestination>(
            Items.Select(map),
            HasNextPage,
            ContinuationToken,
            OrderBy);
    }

    /// <summary>
    /// Creates an empty paginated list based on the provided pagination parameters.
    /// </summary>
    /// <typeparam name="TOrderDefinition">The type of the order definition.</typeparam>
    /// <typeparam name="TTarget">The type of the target.</typeparam>
    /// <param name="parameter">The sortable pagination parameter.</param>
    /// <returns>An empty <see cref="PaginatedList{T}"/>.</returns>
    public static PaginatedList<T> CreateEmpty<TOrderDefinition, TTarget>(SortablePaginationParameter<TOrderDefinition, TTarget> parameter)
        where TOrderDefinition : IOrderDefinition<TTarget> =>
        new(
            items: [],
            hasNextPage: false,
            @continue: parameter.ContinuationToken?.Raw,
            orderBy: parameter.OrderBy.DefaultIfNull().GetOrderString());

    public bool Contains(T item)
    {
        return Items.Contains(item);
    }
}
