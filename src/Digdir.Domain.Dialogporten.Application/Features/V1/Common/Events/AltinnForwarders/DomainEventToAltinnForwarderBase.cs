using Digdir.Domain.Dialogporten.Application.Externals;
using Microsoft.Extensions.Options;

namespace Digdir.Domain.Dialogporten.Application.Features.V1.Common.Events.AltinnForwarders;

internal class DomainEventToAltinnForwarderBase
{
    protected readonly ICloudEventBus CloudEventBus;
    private readonly DialogportenSettings _dialogportenSettings;

    protected DomainEventToAltinnForwarderBase(ICloudEventBus cloudEventBus, IOptions<ApplicationSettings> settings)
    {
        CloudEventBus = cloudEventBus ?? throw new ArgumentNullException(nameof(cloudEventBus));
        _dialogportenSettings = settings.Value.Dialogporten ?? throw new ArgumentNullException(nameof(settings));
    }

    internal string SourceBaseUrl() =>
        $"{_dialogportenSettings.BaseUri}/api/v1/enduser/dialogs/";
}
