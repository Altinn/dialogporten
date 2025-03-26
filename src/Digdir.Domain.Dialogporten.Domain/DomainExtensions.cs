using Digdir.Domain.Dialogporten.Domain.Common.DomainEvents;
using Digdir.Domain.Dialogporten.Domain.Common.EventPublisher;
using Microsoft.Extensions.DependencyInjection;

namespace Digdir.Domain.Dialogporten.Domain;

public static class DomainExtensions
{
    public static IEnumerable<Type> GetDomainEventTypes()
        => DomainAssemblyMarker.Assembly.DefinedTypes
            .Where(x => !x.IsAbstract && !x.IsInterface && !x.IsGenericType)
            .Where(x => x.IsAssignableTo(typeof(IDomainEvent)));

    public static IServiceCollection AddDomain(this IServiceCollection services)
        => services.AddScoped<ISilentUpdateContext, SilentUpdateContext>();
}
