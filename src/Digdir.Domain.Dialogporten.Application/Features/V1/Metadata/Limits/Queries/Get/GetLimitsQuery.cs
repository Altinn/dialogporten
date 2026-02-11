using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using Digdir.Domain.Dialogporten.Application.Common.QueryLimits;
using MediatR;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.Limits.Queries.Get;

public sealed class GetLimitsQuery : IRequest<GetLimitsDto>, IFeatureMetricServiceResourceIgnoreRequest;

internal sealed class GetLimitsQueryHandler : IRequestHandler<GetLimitsQuery, GetLimitsDto>
{
    private readonly IQueryLimitsService _queryLimitsService;

    public GetLimitsQueryHandler(IQueryLimitsService queryLimitsService)
    {
        _queryLimitsService = queryLimitsService ?? throw new ArgumentNullException(nameof(queryLimitsService));
    }

    public Task<GetLimitsDto> Handle(GetLimitsQuery request, CancellationToken cancellationToken)
    {
        var endUserSearchLimits = _queryLimitsService.GetEndUserSearchDialogLimits();
        var serviceOwnerSearchLimits = _queryLimitsService.GetServiceOwnerSearchDialogLimits();

        return Task.FromResult(new GetLimitsDto
        {
            EndUserSearch = new EndUserSearchLimitsDto
            {
                MaxPartyFilterValues = endUserSearchLimits.MaxPartyFilterValues,
                MaxServiceResourceFilterValues = endUserSearchLimits.MaxServiceResourceFilterValues,
                MaxOrgFilterValues = endUserSearchLimits.MaxOrgFilterValues,
                MaxExtendedStatusFilterValues = endUserSearchLimits.MaxExtendedStatusFilterValues
            },
            ServiceOwnerSearch = new ServiceOwnerSearchLimitsDto
            {
                MaxPartyFilterValues = serviceOwnerSearchLimits.MaxPartyFilterValues,
                MaxServiceResourceFilterValues = serviceOwnerSearchLimits.MaxServiceResourceFilterValues,
                MaxExtendedStatusFilterValues = serviceOwnerSearchLimits.MaxExtendedStatusFilterValues
            }
        });
    }
}
