using Digdir.Domain.Dialogporten.Application.Externals;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Events.AltinnForwarders;

internal class DomainEventToAltinnForwarderBase
{
    protected readonly ICloudEventBus CloudEventBus;
    private readonly DialogportenSettings _dialogportenSettings;

    protected DomainEventToAltinnForwarderBase(ICloudEventBus cloudEventBus, IOptions<ApplicationSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(cloudEventBus);
        ArgumentNullException.ThrowIfNull(settings);

        var dialogportenSettings = settings.Value.Dialogporten;
        ArgumentNullException.ThrowIfNull(dialogportenSettings, nameof(settings));

        CloudEventBus = cloudEventBus;
        _dialogportenSettings = dialogportenSettings;
    }

    internal string SourceBaseUrl() =>
        $"{_dialogportenSettings.BaseUri}/api/v1/enduser/dialogs/";
}
