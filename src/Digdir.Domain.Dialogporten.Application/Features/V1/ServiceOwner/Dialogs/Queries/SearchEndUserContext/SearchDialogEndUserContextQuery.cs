using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.Pagination;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.Order;
using Digdir.Domain.Dialogporten.Application.Common.Pagination.OrderOption;
using Digdir.Domain.Dialogporten.Application.Common.ReturnTypes;
using Digdir.Domain.Dialogporten.Application.Common;
using Digdir.Domain.Dialogporten.Application.Externals;
using Digdir.Domain.Dialogporten.Domain.DialogEndUserContexts.Entities;
using MediatR;
using OneOf;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.ServiceOwner.Dialogs.Queries.SearchEndUserContext;

public sealed class SearchDialogEndUserContextQuery : PaginationParameter<SearchDialogEndUserContextOrderDefinition, DataDialogEndUserContextListItemDto>, IRequest<SearchDialogEndUserContextResult>, IFeatureMetricServiceResourceIgnoreRequest
{
    /// <summary>
    /// Filter by one or more owning parties
    /// </summary>
    public List<string> Party { get; set; } = [];

    /// <summary>
    /// Filter by content updated after this date
    /// </summary>
    public DateTimeOffset? CreatedAfter { get; set; }

    /// <summary>
    /// Filter by one or more system labels
    /// </summary>
    public List<SystemLabel.Values>? Label { get; set; }
}

public sealed class SearchDialogEndUserContextOrderDefinition : IOrderDefinition<DataDialogEndUserContextListItemDto>
{
    public static IOrderOptions<DataDialogEndUserContextListItemDto> Configure(IOrderOptionsBuilder<DataDialogEndUserContextListItemDto> options) =>
        options.AddId(x => x.Id)
            .AddDefault("contentUpdatedAt", x => x.ContentUpdatedAt)
            .Build();
}

[GenerateOneOf]
public sealed partial class SearchDialogEndUserContextResult : OneOfBase<PaginatedList<DialogEndUserContextItemDto>, ValidationError>;

public sealed class DialogEndUserContextItemDto
{
    public Guid DialogId { get; set; }

    public Guid EndUserContextRevision { get; set; }

    public List<SystemLabel.Values> SystemLabels { get; set; } = [];
}

internal sealed class SearchDialogEndUserContextQueryHandler : IRequestHandler<SearchDialogEndUserContextQuery, SearchDialogEndUserContextResult>
{
    private readonly IUserResourceRegistry _userResourceRegistry;
    private readonly IDialogSearchRepository _searchRepository;

    public SearchDialogEndUserContextQueryHandler(
        IUserResourceRegistry userResourceRegistry,
        IDialogSearchRepository searchRepository)
    {
        _userResourceRegistry = userResourceRegistry ?? throw new ArgumentNullException(nameof(userResourceRegistry));
        _searchRepository = searchRepository ?? throw new ArgumentNullException(nameof(searchRepository));
    }

    public async Task<SearchDialogEndUserContextResult> Handle(SearchDialogEndUserContextQuery request, CancellationToken cancellationToken)
    {
        var orgName = await _userResourceRegistry.GetCurrentUserOrgShortName(cancellationToken);
        var paginatedList = await _searchRepository.SearchDialogEndUserContextsAsServiceOwner(
            orgName,
            request.Party.Select(x => x.ToLowerInvariant()).ToList(),
            request.Label,
            request.CreatedAfter,
            request.ContinuationToken,
            request.Limit!.Value,
            cancellationToken);

        return paginatedList.ConvertTo(x => new DialogEndUserContextItemDto
        {
            DialogId = x.DialogId,
            EndUserContextRevision = x.EndUserContextRevision,
            SystemLabels = x.SystemLabels
        });
    }
}
