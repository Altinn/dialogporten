using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Digdir.Library.Utils.AspNet;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAspNetCommon(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<WebHostCommonSettings>()
            .Bind(configuration);

        return services;
    }
}
