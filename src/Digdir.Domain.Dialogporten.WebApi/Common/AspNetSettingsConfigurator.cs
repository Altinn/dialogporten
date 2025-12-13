using Digdir.Domain.Dialogporten.Application;
using Digdir.Library.Utils.AspNet;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.WebApi.Common;

public sealed class AspNetSettingsConfigurator : IConfigureOptions<AspNetSettings>
{
    private readonly IOptionsMonitor<ApplicationSettings> _appSettings;

    public AspNetSettingsConfigurator(IOptionsMonitor<ApplicationSettings> appSettings)
    {
        _appSettings = appSettings;
    }

    public void Configure(AspNetSettings options)
    {
        var appSettings = _appSettings.CurrentValue;
        options.FeatureToggle.PresentationLayerMaintenanceMode = appSettings.FeatureToggle.PresentationLayerMaintenanceMode;
    }
}
