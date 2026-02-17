using Dapper;
using Microsoft.Extensions.DependencyInjection;

namespace Digdir.Domain.Dialogporten.Infrastructure.Common.Configurations.Dapper;

internal static class ServiceCollectionExtensions
{
    public static IServiceCollection AddDapperTypeHandlers(this IServiceCollection services)
    {
        SqlMapper.AddTypeHandler(new GuidArrayTypeHandler());
        return services;
    }
}
