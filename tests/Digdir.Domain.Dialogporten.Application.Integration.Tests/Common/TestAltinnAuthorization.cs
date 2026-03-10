using Digdir.Domain.Dialogporten.Application.Externals.AltinnAuthorization;
using Digdir.Domain.Dialogporten.Application.Integration.Tests.Common.ApplicationFlow;
using Microsoft.Extensions.DependencyInjection;

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

        public TFlowStep ConfigureAltinnAuthorization(
            Action<IAltinnAuthorization> configure,
            Action<IServiceCollection> configureServices)
        {
            ArgumentNullException.ThrowIfNull(configure);
            ArgumentNullException.ThrowIfNull(configureServices);

            return flowStep.Do(ctx =>
                ctx.Application.ConfigureServices(services =>
                {
                    configureServices(services);
                    services.ConfigureAltinnAuthorization(configure);
                }));
        }
    }
}
