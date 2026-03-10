using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;

namespace Digdir.Domain.Dialogporten.Application.Integration.Tests.Common;

internal static class TestAltinnAuthorizationExtensions
{
    extension<TFlowStep>(TFlowStep flowStep) where TFlowStep : IFlowStep
    {
        public TFlowStep ConfigureAltinnAuthorization(Action<IAltinnAuthorization> configure)
        {
            ArgumentNullException.ThrowIfNull(configure);

            return flowStep.Do(ctx =>
                ctx.Application.ConfigureServices(services =>
                    services.ConfigureAltinnAuthorization(configure)));
        }
    }
}
