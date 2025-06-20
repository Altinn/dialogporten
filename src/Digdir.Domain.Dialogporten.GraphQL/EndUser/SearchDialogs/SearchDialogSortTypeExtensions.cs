using System.Globalization;
using System.Text;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Order;
using Digdir.Domain.Dialogporten.Application.Features.V1.EndUser.Dialogs.Queries.Search;

namespace Digdir.Domain.Dialogporten.GraphQL.EndUser.SearchDialogs;

internal static class SearchDialogSortTypeExtensions
{
    public static List<SearchDialogSortType> ToSearchDialogSortTypeList(this ReadOnlySpan<char> orderSet)
    {
        List<SearchDialogSortType> searchDialogSortTypes = [];
        var orderSetEnumerator = orderSet.Split(PaginationConstants.OrderSetDelimiter);
        while (orderSetEnumerator.MoveNext())
        {
            var order = orderSet[orderSetEnumerator.Current];
            var orderEnumerator = order.Split(PaginationConstants.OrderDelimiter);

            // ignore empty part
            if (!orderEnumerator.MoveNext())
            {
                continue;
            }

            var field = order[orderEnumerator.Current];
            // ignore order by id
            if (field.Equals(PaginationConstants.OrderIdKey, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var dir = PaginationConstants.DefaultOrderDirection;
            if (orderEnumerator.MoveNext() &&
                !Enum.TryParse(order[orderEnumerator.Current], ignoreCase: true, out dir))
            {
                throw new InvalidOperationException("Invalid sort direction");
            }

            var factory = GetSearchDialogSortTypeFactory(field);
            searchDialogSortTypes.Add(factory(dir));
        }

        return searchDialogSortTypes;
    }

    public static bool TryToOrderSet(this List<SearchDialogSortType>? searchDialogSortTypes,
        out OrderSet<SearchDialogQueryOrderDefinition, IntermediateDialogDto>? orderSet)
    {
        if (searchDialogSortTypes is null)
        {
            orderSet = null;
            return false;
        }

        var stringBuilder = new StringBuilder();
        foreach (var orderBy in searchDialogSortTypes)
        {
            if (orderBy.CreatedAt.HasValue)
            {
                stringBuilder.Append(CultureInfo.InvariantCulture, $"createdAt_{orderBy.CreatedAt},");
                continue;
            }

            if (orderBy.UpdatedAt.HasValue)
            {
                stringBuilder.Append(CultureInfo.InvariantCulture, $"updatedAt_{orderBy.UpdatedAt},");
                continue;
            }

            if (orderBy.DueAt.HasValue)
            {
                stringBuilder.Append(CultureInfo.InvariantCulture, $"dueAt_{orderBy.DueAt},");
            }

            if (orderBy.ContentUpdatedAt.HasValue)
            {
                stringBuilder.Append(CultureInfo.InvariantCulture, $"contentUpdatedAt_{orderBy.ContentUpdatedAt},");
            }
        }

        if (OrderSet<SearchDialogQueryOrderDefinition, IntermediateDialogDto>.TryParse(stringBuilder.ToString(),
                out var parsedOrderSet))
        {
            orderSet = parsedOrderSet;
            return true;
        }

        orderSet = null;
        return false;
    }

    private static Func<OrderDirection, SearchDialogSortType> GetSearchDialogSortTypeFactory(ReadOnlySpan<char> field)
    {
        if (field.Equals("createdat", StringComparison.OrdinalIgnoreCase))
        {
            return CreatedAtFactory;
        }

        if (field.Equals("updatedat", StringComparison.OrdinalIgnoreCase))
        {
            return UpdatedAtFactory;
        }

        if (field.Equals("dueat", StringComparison.OrdinalIgnoreCase))
        {
            return DuAtFactory;
        }

        if (field.Equals("contentupdatedat", StringComparison.OrdinalIgnoreCase))
        {
            return ContentUpdatedAtFactory;
        }

        throw new InvalidOperationException("Invalid sort field");
    }

    private static SearchDialogSortType DuAtFactory(OrderDirection dir) => new() { DueAt = dir };
    private static SearchDialogSortType UpdatedAtFactory(OrderDirection dir) => new() { UpdatedAt = dir };
    private static SearchDialogSortType CreatedAtFactory(OrderDirection dir) => new() { CreatedAt = dir };
    private static SearchDialogSortType ContentUpdatedAtFactory(OrderDirection dir) => new() { ContentUpdatedAt = dir };
}
