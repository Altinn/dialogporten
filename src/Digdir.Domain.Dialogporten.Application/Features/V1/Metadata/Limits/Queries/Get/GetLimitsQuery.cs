using Digdir.Domain.Dialogporten.Application.Common.Behaviours.FeatureMetric;
using MediatR;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Metadata.Limits.Queries.Get;

public sealed class GetLimitsQuery : IRequest<GetLimitsDto>, IFeatureMetricServiceResourceIgnoreRequest;

internal sealed class GetLimitsQueryHandler : IRequestHandler<GetLimitsQuery, GetLimitsDto>
{
    private readonly ApplicationSettings _applicationSettings;

    public GetLimitsQueryHandler(IOptionsSnapshot<ApplicationSettings> applicationSettings)
    {
        ArgumentNullException.ThrowIfNull(applicationSettings);
        _applicationSettings = applicationSettings.Value;
    }

    public Task<GetLimitsDto> Handle(GetLimitsQuery request, CancellationToken cancellationToken)
    {
        var endUserSearchLimits = _applicationSettings.Limits.EndUserSearch;
        var serviceOwnerSearchLimits = _applicationSettings.Limits.ServiceOwnerSearch;

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
