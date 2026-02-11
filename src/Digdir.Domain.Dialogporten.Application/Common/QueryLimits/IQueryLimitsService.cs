using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Application.Common.QueryLimits;

public interface IQueryLimitsService
{
    EndUserSearchQueryLimits GetEndUserSearchDialogLimits();
    ServiceOwnerSearchQueryLimits GetServiceOwnerSearchDialogLimits();
}

internal sealed class QueryLimitsService : IQueryLimitsService
{
    private readonly IOptionsSnapshot<ApplicationSettings> _applicationSettings;

    public QueryLimitsService(IOptionsSnapshot<ApplicationSettings> applicationSettings)
    {
        _applicationSettings = applicationSettings ?? throw new ArgumentNullException(nameof(applicationSettings));
    }

    public EndUserSearchQueryLimits GetEndUserSearchDialogLimits() =>
        _applicationSettings.Value.Limits.EndUserSearch;

    public ServiceOwnerSearchQueryLimits GetServiceOwnerSearchDialogLimits() =>
        _applicationSettings.Value.Limits.ServiceOwnerSearch;
}
