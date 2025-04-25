using Digdir.Domain.Dialogporten.Domain.Common.EventPublisher;

namespace Digdir.Domain.Dialogporten.Domain;

public static class DomainExtensions
{
    public static IEnumerable<Type> GetDomainEventTypes()
        => DomainAssemblyMarker.Assembly.DefinedTypes
            .Where(x => !x.IsAbstract && !x.IsInterface && !x.IsGenericType)
            .Where(x => x.IsAssignableTo(typeof(IDomainEvent)));
}
